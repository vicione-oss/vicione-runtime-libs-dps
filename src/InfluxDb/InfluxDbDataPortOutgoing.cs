using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public class InfluxDbDataPortOutgoing : IExternalOutgoingCommunication<InfluxDbDataPortCommunication>, IDisposable
{
    private const int QueueSize = 100_000;

    private readonly Dictionary<string, Guid> _channelNodes;
    private Dictionary<string, RecordInfo> _channelRecordInfos = [];
    private readonly Dictionary<string, INode[]> _routes;
    private InfluxDBClient? _client;
    private WriteApiAsync? _writeApi;
    private readonly InfluxDbDataPortCommunication _communication;
    private readonly string _organization;
    private readonly int _bulkSize;
    private readonly TimeSpan _minimumPublishInterval;
    private readonly ILogger _logger;
    private ManagedQueuePipeline<PointToSend>? _queue;

    public InfluxDbDataPortOutgoing(InfluxDbDataPortCommunication communication, ILogger<InfluxDbDataPortOutgoing> logger)
    {
        var nodeParentsCache = communication.Nodes.CreateNodeParentsCache();
        var routes = communication.Nodes
            .Where(n => n.DesignId is InfluxDbNodeDesignId.FieldNode or InfluxDbNodeDesignId.TagNode)
            .SelectMany(GetRoutes)
            .ToArray();

        _communication = communication;
        InfluxDbDataPortProperties properties = new(_communication);
        _organization = properties.Organization;
        _bulkSize = properties.BulkSize;
        _minimumPublishInterval = properties.MinimumPublishInterval;
        _logger = logger;
        _channelNodes = routes.ToDictionary(r => r.AffectedChannel, r => r.Node.Id);
        _routes = routes.ToDictionary(r => r.AffectedChannel, r => r.Route);
        _queue ??= new ManagedQueuePipeline<PointToSend>(
            QueueSize,
            string.Empty,
            _bulkSize,
            _logger,
            _logger.PublishBatchFailed,
            _logger.PublishWithCompletedQueueFailed,
            minimumPublishInterval: _minimumPublishInterval);

        IEnumerable<(string AffectedChannel, INode Node, INode[] Route)> GetRoutes(INode node)
        {
            var routes = node.GetSuccessorRoutes(nodeParentsCache);

            if (routes.Count == 0)
            {
                foreach (var channel in node.AffectedChannels)
                    yield return (channel, node, []);
                yield break;
            }

            foreach (var route in routes)
            {
                foreach (var channel in node.AffectedChannels)
                    yield return (channel, node, route);
            }
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        InfluxDbDataPortProperties properties = new(_communication);
        _client ??= new(properties.Url, properties.Token);
        _writeApi ??= _client.GetWriteApiAsync();

        var nodeRecordInfo = InfluxDbExtensions.CreateRecordInfo(_communication.Nodes);
        _channelRecordInfos = _channelNodes.ToDictionary(nf => nf.Key, nf => nodeRecordInfo[nf.Value]);

        var expected = GetExpectedStructure(_communication.Nodes);
        var current = await GetExistingStructureAsync().ConfigureAwait(false);

        await EnsureStructureAsync(expected, current, cancellationToken).ConfigureAwait(false);

        _queue?.Start(PublishAsync, cancellationToken);
    }

    private async Task EnsureStructureAsync(IReadOnlyCollection<Bucket> expected, IReadOnlyCollection<Bucket> current, CancellationToken cancellationToken)
    {
        if (InfluxDbComparer.TryGetMissingStructure(current, expected, out var missingBuckets))
            await CreateMissingStructureAsync(missingBuckets, cancellationToken).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_queue is not null)
            await _queue.Complete();

        _client?.Dispose();
        _client = null;
        _writeApi = null;
    }

    public Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values, CancellationToken cancellationToken)
    {
        var records = PrepareDataRecords(values, _channelRecordInfos, _routes).GroupBy(r => r.MeasurementInfo.Bucket);
        var pointsToSend = records.SelectMany(r => CreatePointToSend(r.Key, r));
        _queue?.Append(pointsToSend);
        return Task.CompletedTask;
    }

    internal async ValueTask<bool> PublishAsync(IEnumerable<PointToSend> data, CancellationToken cancellationToken)
    {
        try
        {
            if (_writeApi is null)
                throw new InvalidOperationException("Call ConnectAsync befor trying to send Data");

            foreach (var pointsByBucket in data.GroupBy(p => p.Bucket))
            {
                var bucket = pointsByBucket.Key;
                var points = pointsByBucket.Select(p => p.PointData).ToArray();

                await PublishToInfluxDbAsync(_writeApi, points, bucket, cancellationToken);
                _logger.Published(points.Length, bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.PublishingFailed(ex);
            return false;
        }

        return true;
    }

    private async Task PublishToInfluxDbAsync(WriteApiAsync write, IEnumerable<PointData> points, string bucket, CancellationToken cancellationToken)
        => await write.WritePointsAsync(points.ToArray(), bucket, _organization, cancellationToken);

    internal static IReadOnlyCollection<DataRecord> PrepareDataRecords(IReadOnlyCollection<ExternalValue> values, Dictionary<string, RecordInfo> channelFields, Dictionary<string, INode[]> routes)
    {
        return [.. values
            .GroupBy(v => channelFields[v.Channel])
            .GroupBy(e => e.Key.MeasurementInfo)
            .Select(mg => new DataRecord(mg.Key, mg.ToDictionary(mg => mg.Key, mg => CreateValue(mg)), values.Max(v => v.Timestamp)))];

        object? CreateValue(IGrouping<RecordInfo, ExternalValue> grouping)
        {
            if (grouping.Key.IsJsonStructure)
            {
                return NodeValueFactory.CreateJsonStructure(routes, [.. grouping]).IncludeMeta().ToString();
            }
            else
            {
                var value = grouping.First().Value;

                if (value is null)
                    return DBNull.Value;
                else if (grouping.Key.IsUnknownType)
                    return JsonSerializer.Serialize(value);
                return value;
            }
        }
    }

    internal static IReadOnlyCollection<PointToSend> CreatePointToSend(string bucket, IEnumerable<DataRecord> dataRecords)
    {
        List<PointToSend> points = [];

        foreach (var dataRecord in dataRecords)
        {
            var point = PointData.Measurement(dataRecord.MeasurementInfo.Measurement);

            foreach (var record in dataRecord.Records)
            {
                if (record.Key.RecordType == RecordType.Field)
                    point = point.Field(record.Key.Name, record.Value);
                else if (record.Key.RecordType == RecordType.Tag)
                    point = point.Tag(record.Key.Name, (record.Value ?? string.Empty).ToString());
            }

            point = point.Timestamp(dataRecord.TimeStamp, InfluxDB.Client.Api.Domain.WritePrecision.Ns);

            points.Add(new PointToSend(point, bucket));
        }

        return points;
    }

    private async Task CreateMissingStructureAsync(List<Bucket> missingBuckets, CancellationToken cancellationToken)
    {
        if (missingBuckets.Count > 0)
        {
            var org = await _client!.GetOrganizationsApi().FindOrganizationsAsync(org: _organization, cancellationToken: cancellationToken);

            foreach (var bucket in missingBuckets)
            {
                await _client.GetBucketsApi().CreateBucketAsync(bucket.Name, org.First().Id, cancellationToken);
            }
        }
    }

    internal async Task<IReadOnlyCollection<Bucket>> GetExistingStructureAsync()
    {
        var orgs = await _client!.GetOrganizationsApi().FindOrganizationsAsync(org: _organization);
        var buckets = await _client.GetBucketsApi().FindBucketsAsync(orgID: orgs.First().Id);
        var existingStructure = buckets.Select(b => new Bucket(b.Name)).ToList();

        return existingStructure;
    }

    internal static IReadOnlyCollection<Bucket> GetExpectedStructure(IReadOnlyCollection<INode> nodes)
    {
        var bucketNodes = nodes.Where(n => n.DesignId == InfluxDbNodeDesignId.BucketNode);
        var buckets = new List<Bucket>();

        foreach (var bucket in bucketNodes)
        {
            buckets.Add(new(bucket.Name));
        }

        return buckets;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
            _queue?.Dispose();
        }
    }
}
