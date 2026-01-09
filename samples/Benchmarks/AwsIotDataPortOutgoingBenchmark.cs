using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

public class AwsIotDataPortOutgoingBenchmark : IDisposable
{
    private AwsIotDataPortOutgoing? _communication;
    private bool _disposedValue;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<List<ExternalValue>> _data = [];

    public AwsIotDataPortOutgoingBenchmark()
        => _loggerFactory = LoggerFactory
            .Create(logging => logging.AddConsole());

    [GlobalSetup]
    public void Setup()
    {
        Node broker = new()
        {
            Id = Guid.NewGuid(),
            DesignId = AwsIotNodeDesignId.Broker,
            Name = "Broker",
        };
        Node things = new()
        {
            Id = Guid.NewGuid(),
            DesignId = AwsIotNodeDesignId.Things,
            Name = "Things",
        };
        List<Node> nodes = [broker, things,];
        List<Node> valueNodes = [];
        Random random = new(100);

        for (var i = 0; i < CountPerBatch; i++)
        {
            var nodeId = Guid.NewGuid();
            Node node = new()
            {
                Id = nodeId,
                ParentId = broker.Id,
                Name = $"value{i}",
                DesignId = AwsIotNodeDesignId.Topic,
                TransferredChannels = { nodeId.ToString(), },
                AffectedChannels = { nodeId.ToString(), },
                ValueType = typeof(int),
            };
            nodes.Add(node);
            valueNodes.Add(node);
        }

        for (var i = 0; i < CountPerBatch; i++)
        {
            var thingNodeId = Guid.NewGuid();
            Node thingNode = new()
            {
                Id = thingNodeId,
                ParentId = things.Id,
                Name = $"thing{i}",
                DesignId = AwsIotNodeDesignId.Thing,
            };
            nodes.Add(thingNode);

            var reportedValueNodeId = Guid.NewGuid();
            var classicShadowNodeId = Guid.NewGuid();
            Node classicShadownode = new()
            {
                Id = classicShadowNodeId,
                ParentId = thingNodeId,
                Name = "ClassicShadow",
                DesignId = AwsIotNodeDesignId.ClassicDeviceShadow,
                TransferredChannels = { reportedValueNodeId.ToString(), },
            };
            nodes.Add(classicShadownode);

            var reportedNodeId = Guid.NewGuid();
            Node reportedNode = new()
            {
                Id = reportedNodeId,
                ParentId = classicShadowNodeId,
                DesignId = AwsIotNodeDesignId.Reported,
                Name = "reported",
            };
            nodes.Add(reportedNode);

            var groupNodeId = Guid.NewGuid();
            Node groupNode = new()
            {
                Id = groupNodeId,
                ParentId = reportedNodeId,
                DesignId = AwsIotNodeDesignId.JsonStructure,
                Name = "group",
            };
            nodes.Add(groupNode);

            Node reportedValueNode = new()
            {
                Id = reportedValueNodeId,
                ParentId = groupNodeId,
                DesignId = AwsIotNodeDesignId.JsonStructure,
                Name = "value",
                AffectedChannels = { reportedValueNodeId.ToString(), },
                ValueType = typeof(int),
            };
            nodes.Add(reportedValueNode);
            valueNodes.Add(reportedValueNode);
        }

        for (var i = 0; i < Batches; i++)
        {
#pragma warning disable CA5394 // Keine unsichere Zufälligkeitsstufe verwenden
            _data.Add([.. valueNodes.Select(n => new ExternalValue
            {
                Channel = n.Id.ToString(),
                Value = random.Next(),
                Timestamp = DateTime.UtcNow,
            })]);
#pragma warning restore CA5394 // Keine unsichere Zufälligkeitsstufe verwenden
        }

        AwsIotDataPortCommunication communication = new()
        {
            Nodes = nodes,
            Region = RegionEndpoint.EUCentral1.SystemName,
            AccessKey = "",
            SecretAccessKey = "",
            CertificateFile = @"",
            PrivateKeyFile = @"",
            CertificateId = "",
        };
        _communication = new(communication, _loggerFactory);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _communication?.Dispose();
        _data.Clear();
    }

    [Params(2)]
    public int CountPerBatch { get; set; }
    [Params(2)]
    public int Batches { get; set; }

    [Benchmark]
    [SuppressMessage("Naming", "CA1707:Bezeichner dürfen keine Unterstriche enthalten")]
    public async Task AwsIot_External_Outgoing_Async()
    {
        await _communication!.ConnectAsync(CancellationToken.None);
        foreach (var partition in _data)
            await _communication.SendAsync(0, partition, CancellationToken.None);
        await _communication.DisconnectAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _communication?.Dispose();
                _loggerFactory.Dispose();
            }
            _disposedValue = true;
        }
    }
}
