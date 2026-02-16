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
    private readonly ILogger _logger;

    private readonly PollingJobOrchestrator<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>
        _pollingOrchestrator;

    private readonly TimeProvider _timeProvider;

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
        _logger = loggerFactory.CreateLogger(GetType());
        _timeProvider = timeProvider ?? TimeProvider.System;
        DeviceNode = typedNodeMapper.MapToTypedNodes(communication);
        DataPoints = dataPointGroupsMapper.ToDataPoints(DeviceNode);
        DataPointGroups = dataPointGroupsMapper.Group(DataPoints);
        _pollingOrchestrator = new PollingJobOrchestrator<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>(
            loggerFactory, _timeProvider);
    }

    /// <summary>The typed device node built from the communication configuration.</summary>
    public TDeviceNode DeviceNode { get; }

    /// <summary>The flat list of data points extracted from the device node.</summary>
    public List<TDataPoint> DataPoints { get; }

    /// <summary>The data points grouped for polling, derived from <see cref="DataPoints"/>.</summary>
    public List<TDataPointGroup> DataPointGroups { get; }

    /// <summary>
    /// The information required to uniquely identify and create a client.
    /// </summary>
    public abstract TClientInformation ClientInformation { get; }

    /// <summary>
    /// The client used for retrieving dataPoints. Will be set after initial connect.
    /// </summary>
    protected TReadClient? Client { get; private set; }

    /// <summary>
    /// Optional verifier that checks data points for configuration mismatches before queue processing starts
    /// </summary>
    protected abstract IDataPointConfigurationVerifier<TDataPoint>? ConfigurationVerifier { get; }

    /// <summary>
    /// Manager for acquiring and releasing client connections.
    /// </summary>
    protected abstract IClientLifecycleManager<TReadClient, TClientInformation> ClientLifecycleManager { get; }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources asynchronously.
    /// Stops all polling jobs and releases the client connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisposeAdditionalResourcesAsync().ConfigureAwait(false);
        await _pollingOrchestrator.DisposeAsync().ConfigureAwait(false);
        await ReleaseClientAsync(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override to dispose resources owned by the subclass. Called before the base class releases its own resources.
    /// No need to call <c>base.DisposeAdditionalResourcesAsync()</c>.
    /// </summary>
    protected virtual ValueTask DisposeAdditionalResourcesAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Event raised when new data values are received from the device and converted to external values.
    /// </summary>
    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    /// <summary>
    /// Template method: Connect to external system and start polling.
    /// Orchestrates connection, validation, and polling job creation.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _logger.ConnectingToDevice(DeviceNode.DeviceIdentifier);
        Client = await ClientLifecycleManager.AcquireConnectedAsync(ClientInformation, cancellationToken)
            .ConfigureAwait(false);

        var misConfiguredDataPoints = await VerifyDataPoints(cancellationToken).ConfigureAwait(false);
        if (misConfiguredDataPoints.Count > 0)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidConfigurationException(
                $"Failed to connect as {misConfiguredDataPoints.Count} data point(s) are misconfigured.");
        }

        _pollingOrchestrator.StartAll(DataPointGroups, Client, PublishDataPointValues);
    }

    /// <summary>
    /// Template method: Disconnect from external system and stop polling.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger.DisconnectingFromDevice(DeviceNode.DeviceIdentifier);
        await _pollingOrchestrator.StopAllAsync().ConfigureAwait(false);
        await ReleaseClientAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<List<MisConfiguredDataPoint<TDataPoint>>> VerifyDataPoints(
        CancellationToken cancellationToken)
    {
        if (ConfigurationVerifier == null) return [];
        var misconfiguredDataPoints = await ConfigurationVerifier
            .Verify(DataPoints, cancellationToken)
            .ConfigureAwait(false);
        foreach (var misconfigured in misconfiguredDataPoints)
        {
            var misconfigurations = string.Join(", ", misconfigured.MisMatchingConfigurations.Select(m => m.Value));
            _logger.MisConfiguredDataPoint(misconfigured.DataPoint.Identifier.Value, misconfigurations);
        }

        return misconfiguredDataPoints;
    }


    /// <summary>
    /// Publishes data point values as external values via Received event.
    /// </summary>
    private void PublishDataPointValues(IReadOnlyList<TDataPointValue> dataPointValues)
    {
        var publicationTime = _timeProvider.GetUtcNow().DateTime;
        var externalValues = ToExternalValues(dataPointValues, publicationTime);
        _logger.PublishingExternalValues(externalValues.Count, new FormattedExternalValues(externalValues));
        Received?.Invoke(externalValues);
    }

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
        var totalCount = 0;
        foreach (var dpv in values)
            totalCount += dpv.DataPoint.Channels.TransferredChannels.Count;

        var result = new List<ExternalValue>(totalCount);
        foreach (var dataPointValue in values)
        {
            foreach (var channel in dataPointValue.DataPoint.Channels.TransferredChannels)
            {
                result.Add(new ExternalValue
                {
                    Channel = channel.Value,
                    Value = dataPointValue.Value,
                    Timestamp = timestamp,
                    Validity = 1
                });
            }
        }

        return result;
    }

    private async ValueTask ReleaseClientAsync(CancellationToken cancellationToken)
    {
        if (Client is null) return;
        await ClientLifecycleManager.ReleaseAsync(ClientInformation, cancellationToken).ConfigureAwait(false);
        Client = default;
    }
}
