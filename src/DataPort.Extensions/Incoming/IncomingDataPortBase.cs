using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Client;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Incoming.Client;
using ViciOne.Suite.DataPort.Extensions.Incoming.Polling;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.Mapping;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Verification;
using IClientInformation = ViciOne.Suite.DataPort.Extensions.Client.IClientInformation;

namespace ViciOne.Suite.DataPort.Extensions.Incoming;

/// <summary>
/// Base class for IncomingDataPort implementations.
/// Implements Template Method pattern with strategy hooks for connection management.
/// </summary>
/// <typeparam name="TCommunication">Communication configuration type</typeparam>
/// <typeparam name="TDeviceNode">Device node configuration type</typeparam>
/// <typeparam name="TDataPoint">Polling data point type</typeparam>
/// <typeparam name="TDataPointGroup">Data point group type</typeparam>
/// <typeparam name="TDataPointValue">Read result type with value</typeparam>
/// <typeparam name="TReadClient">Client type for read operations</typeparam>
/// <typeparam name="TClientInformation">The information required to uniquely identify and create a client.</typeparam>
/// <remarks>
/// <para><b>Design Patterns:</b></para>
/// <list type="bullet">
/// <item><b>Template Method:</b> ConnectAsync, DisconnectAsync orchestrate common flow</item>
/// <item><b>Strategy:</b> IClientLifecycleManager abstracts connection patterns</item>
/// <item><b>Hook Method:</b> ValidateAsync is virtual (no-op default)</item>
/// </list>
/// <para><b>Lifecycle:</b></para>
/// <list type="number">
/// <item>Create instance via constructor</item>
/// <item>Call <see cref="ConnectAsync"/> to acquire connection and start polling</item>
/// <item>Subscribe to <see cref="Received"/> event for data updates</item>
/// <item>Call <see cref="DisconnectAsync"/> to stop polling and release connection (supports reconnection)</item>
/// <item>Call <see cref="DisposeAsync"/> for final cleanup (cannot reconnect after disposal)</item>
/// </list>
/// <para><b>Thread Safety:</b> Lifecycle methods are not thread-safe and should be called from a single thread.</para>
/// </remarks>
public abstract class IncomingDataPortBase<TCommunication, TDeviceNode, TDataPoint, TDataPointGroup, TDataPointValue,
    TReadClient, TClientInformation>
    : IExternalIncomingCommunication<TCommunication>, IAsyncDisposable
    where TCommunication : DataPortCommunication
    where TDeviceNode : IRootConfigurationNode<TCommunication>
    where TDataPoint : IPollingDataPoint
    where TDataPointGroup : IDataPointPollingGroup<TDataPoint>
    where TDataPointValue : IDataPointValue<TDataPoint>
    where TReadClient : IReadClient<TDataPointGroup, TDataPointValue>
    where TClientInformation : IClientInformation
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    private PollingJobOrchestrator<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>? _pollingOrchestrator;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the IncomingDataPortBase class.
    /// </summary>
    /// <param name="communication">The communication configuration to build the device node from.</param>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    /// <param name="typedNodeMapper">The mapper that converts the communication configuration to a typed device node.</param>
    /// <param name="dataPointGroupsMapper">The mapper that converts the device node to grouped data points.</param>
    /// <param name="timeProvider">Optional time provider for testing purposes.</param>
    protected IncomingDataPortBase(
        TCommunication communication,
        ILoggerFactory loggerFactory,
        TypedNodeMapper<TCommunication, TDeviceNode> typedNodeMapper,
        IDataPointGroupsMapper<TDataPoint, TDataPointGroup, TDeviceNode, TCommunication> dataPointGroupsMapper,
        TimeProvider? timeProvider = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(GetType());
        _timeProvider = timeProvider ?? TimeProvider.System;
        DeviceNode = typedNodeMapper.MapToTypedNodes(communication);
        DataPoints = dataPointGroupsMapper.ToDataPoints(DeviceNode);
        DataPointGroups = dataPointGroupsMapper.Group(DataPoints);
    }

    /// <summary>
    /// The information required to uniquely identify and create a client.
    /// </summary>
    public abstract TClientInformation ClientInformation { get; }

    /// <summary>The typed device node built from the communication configuration.</summary>
    public TDeviceNode DeviceNode { get; }

    /// <summary>The flat list of data points extracted from the device node.</summary>
    public IReadOnlyList<TDataPoint> DataPoints { get; }

    /// <summary>The data points grouped for polling, derived from <see cref="DataPoints"/>.</summary>
    public IReadOnlyList<TDataPointGroup> DataPointGroups { get; }

    /// <summary>
    /// Event raised when new data values are received from the device and converted to external values.
    /// </summary>
    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    /// <summary>
    /// Manager for acquiring and releasing client connections.
    /// </summary>
    protected abstract IClientLifecycleManager<TReadClient, TClientInformation> ClientLifecycleManager { get; }

    /// <summary>
    /// The client used for retrieving dataPoints. Will be set after initial connect.
    /// </summary>
    protected TReadClient? Client { get; private set; }

    /// <summary>
    /// Template method: Connect to external system and start polling.
    /// Orchestrates connection, validation, and polling job creation.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (Client is not null)
        {
            _logger.AlreadyConnectedToDevice(DeviceNode.DeviceIdentifier);
            return;
        }

        _logger.ConnectingToDevice(DeviceNode.DeviceIdentifier);
        Client = await ClientLifecycleManager
            .AcquireConnectedAsync(ClientInformation, cancellationToken)
            .ConfigureAwait(false);

        var misConfiguredDataPoints = await VerifyDataPoints(Client, cancellationToken).ConfigureAwait(false);
        if (misConfiguredDataPoints.Count > 0)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidConfigurationException("Failed to connect as there are misconfigured dataPoints.");
        }

        StartPollingDataPoints(Client);
    }

    /// <summary>
    /// Template method: Disconnect from external system and stop polling.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (Client is null)
        {
            _logger.AlreadyDisconnectedFromDevice(DeviceNode.DeviceIdentifier);
            return;
        }

        _logger.DisconnectingFromDevice(DeviceNode.DeviceIdentifier);

        await StopPollingDataPointsAsync().ConfigureAwait(false);
        await ReleaseClientAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources asynchronously.
    /// Stops all polling jobs and releases the client connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisposeAdditionalResourcesAsync().ConfigureAwait(false);
        await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Optional verifier that checks data points for configuration mismatches before queue processing starts
    /// </summary>
    protected virtual IDataPointConfigurationVerifier<TDataPoint>? CreateConfigurationVerifier(TReadClient client) =>
        null;

    /// <summary>
    /// Converts a batch of data point values to external values.
    /// Override to provide custom conversion logic.
    /// </summary>
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    [SuppressMessage("ReSharper", "ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator")]
    protected virtual IReadOnlyList<ExternalValue> ToExternalValues(
        IReadOnlyList<TDataPointValue> values,
        DateTime timestamp)
    {
        var result = new List<ExternalValue>(values.Sum(dpv => dpv.DataPoint.Channels.TransferredChannels.Count));
        foreach (var dataPointValue in values)
        {
            foreach (var channel in dataPointValue.DataPoint.Channels.TransferredChannels)
            {
                result.Add(ToExternalValue(timestamp, channel, dataPointValue));
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a data point value to an external value.
    /// Override to provide custom conversion logic.
    /// </summary>
    protected virtual ExternalValue ToExternalValue(DateTime timestamp, TransferredChannel channel,
        TDataPointValue dataPointValue) =>
        new()
        {
            Channel = channel.Value,
            Value = dataPointValue.Value,
            Timestamp = timestamp,
            Validity = 1
        };

    /// <summary>
    /// Override to dispose resources owned by the subclass. Called before the base class releases its own resources.
    /// No need to call <c>base.DisposeAdditionalResourcesAsync()</c>.
    /// </summary>
    protected virtual ValueTask DisposeAdditionalResourcesAsync() => ValueTask.CompletedTask;

    private void StartPollingDataPoints(TReadClient client)
    {
        _pollingOrchestrator = new PollingJobOrchestrator<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>(
            _loggerFactory, _timeProvider, client, DataPointGroups, PublishDataPointValues);
        _pollingOrchestrator.Start();
    }

    private async ValueTask<IReadOnlyList<MisconfiguredDataPoint<TDataPoint>>> VerifyDataPoints(TReadClient client,
        CancellationToken cancellationToken)
    {
        var verifier = CreateConfigurationVerifier(client);
        if (verifier == null) return [];

        var misconfiguredDataPoints = await verifier
            .Verify(DataPoints, cancellationToken)
            .ConfigureAwait(false);
        foreach (var misconfigured in misconfiguredDataPoints)
        {
            var misconfigurations = string.Join(", ", misconfigured.MismatchingConfigurations.Select(m => m.Value));
            _logger.MisConfiguredDataPoint(misconfigured.DataPoint.Identifier.Value, misconfigurations);
        }

        return misconfiguredDataPoints;
    }

    private void PublishDataPointValues(IReadOnlyList<TDataPointValue> dataPointValues)
    {
        var publicationTime = _timeProvider.GetUtcNow().DateTime;
        var externalValues = ToExternalValues(dataPointValues, publicationTime);
        _logger.PublishingExternalValues(externalValues.Count, new FormattedExternalValues(externalValues));
        Received?.Invoke(externalValues);
    }

    private async ValueTask StopPollingDataPointsAsync()
    {
        if (_pollingOrchestrator is null) return;
        await _pollingOrchestrator.DisposeAsync().ConfigureAwait(false);
        _pollingOrchestrator = null;
    }

    private async ValueTask ReleaseClientAsync(CancellationToken cancellationToken)
    {
        await ClientLifecycleManager.ReleaseAsync(ClientInformation, cancellationToken).ConfigureAwait(false);
        Client = default;
    }
}
