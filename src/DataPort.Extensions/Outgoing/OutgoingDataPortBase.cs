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
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.Mapping;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Outgoing.ChannelResolution;
using ViciOne.Suite.DataPort.Extensions.Outgoing.Client;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;
using ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;
using ViciOne.Suite.DataPort.Extensions.Verification;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing;

/// <summary>
/// Base class for OutgoingDataPort implementations.
/// Implements Template Method pattern with strategy hooks for retry policies and value conversion.
/// </summary>
/// <typeparam name="TDataPoint">Configuration type for data points</typeparam>
/// <typeparam name="TDataPointValue">Write value type</typeparam>
/// <typeparam name="TWriteClient">Connection client type; must also implement <see cref="IWriteClient{TDataPointValue}"/></typeparam>
/// <typeparam name="TRootNode">Root configuration node type</typeparam>
/// <typeparam name="TCommunication">Communication configuration type</typeparam>
/// <typeparam name="TClientInformation">The information required to uniquely identify and create a client.</typeparam>
/// <remarks>
/// <para><b>Design Patterns:</b></para>
/// <list type="bullet">
/// <item><b>Template Method:</b> ConnectAsync, DisconnectAsync orchestrate common flow</item>
/// <item><b>Strategy:</b> IRetryPolicy, IClientLifecycleManager, ValueConverter abstract implementation-specific behavior</item>
/// </list>
/// <para><b>Performance Optimizations:</b></para>
/// <list type="bullet">
/// <item>Pre-allocated List capacity in ValueConverter (avoids resizing)</item>
/// <item>Indexed for loops (no LINQ) in hot paths</item>
/// <item>O(1) channel lookup via Dictionary</item>
/// <item>All-or-nothing validation semantics (fails fast)</item>
/// </list>
/// </remarks>
public abstract class OutgoingDataPortBase<TDataPoint, TDataPointValue, TWriteClient, TRootNode, TCommunication,
    TClientInformation>
    : IExternalOutgoingCommunication<TCommunication>, IAsyncDisposable
    where TDataPoint : IDataPoint
    where TDataPointValue : IDataPointValue<TDataPoint>
    where TWriteClient : IWriteClient<TDataPointValue>
    where TRootNode : IRootConfigurationNode<TCommunication>
    where TCommunication : DataPortCommunication
    where TClientInformation : IClientInformation
{
    private readonly ChannelResolver<TDataPoint> _channelResolver;
    private readonly ILogger _logger;

    private readonly QueueProcessor<TDataPointValue> _queueProcessor;
    private readonly DataPointValueValidator<TDataPoint, TDataPointValue> _validator;

    private bool _isConnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutgoingDataPortBase{TDataPoint, TDataPointValue, TWriteClient, TRootNode, TCommunication, TClientInformation}"/> class.
    /// </summary>
    /// <param name="communication">The communication configuration used to establish the connection</param>
    /// <param name="loggerFactory">Logger factory for creating typed loggers</param>
    /// <param name="typedNodeMapper">The mapper that converts the communication object to a typed root node</param>
    /// <param name="mapper">The mapper that converts the root node to data points</param>
    /// <param name="queueConfiguration">Queue capacity and overflow strategy configuration</param>
    /// <param name="delayProvider">Delay provider for retry backoff (defaults to Task.Delay)</param>
    protected internal OutgoingDataPortBase(
        TCommunication communication,
        ILoggerFactory loggerFactory,
        TypedNodeMapper<TCommunication, TRootNode> typedNodeMapper,
        IDataPointsMapper<TRootNode, TCommunication, TDataPoint> mapper,
        QueueConfiguration queueConfiguration,
        IDelayProvider delayProvider)
    {
        _logger = loggerFactory.CreateLogger(GetType());
        DevicNode = typedNodeMapper.MapToTypedNodes(communication);
        DataPoints = mapper.ToDataPoints(DevicNode);
        _channelResolver = new ChannelResolver<TDataPoint>(DataPoints);
        _validator = new DataPointValueValidator<TDataPoint, TDataPointValue>(
            loggerFactory.CreateLogger<DataPointValueValidator<TDataPoint, TDataPointValue>>());

        _queueProcessor = new QueueProcessor<TDataPointValue>(
            queueConfiguration,
            delayProvider,
            loggerFactory.CreateLogger<QueueProcessor<TDataPointValue>>(),
            $"{GetType().Name}Queue");
    }

    /// <summary>
    /// Optional verifier that checks data points for configuration mismatches before queue processing starts
    /// </summary>
    protected abstract IDataPointConfigurationVerifier<TDataPoint>? ConfigurationVerifier { get; }

    /// <summary>
    /// Manager for acquiring and releasing client connections.
    /// </summary>
    protected abstract IClientLifecycleManager<TWriteClient, TClientInformation> ClientLifecycleManager { get; }

    /// <summary>Gets the root configuration node derived from the communication object.</summary>
    public TRootNode DevicNode { get; }

    /// <summary>Gets the data points mapped from the root configuration node.</summary>
    public List<TDataPoint> DataPoints { get; }

    /// <summary>
    /// The information required to uniquely identify and create a client.
    /// </summary>
    public abstract TClientInformation ClientInformation { get; }

    /// <summary>
    /// The client used for writing datapoints. Will be set after intial connect.
    /// </summary>
    protected TWriteClient? Client { get; private set; }

    /// <summary>
    /// Gets the retry policy for write operations (implementation-specific).
    /// </summary>
    protected abstract IRetryPolicy RetryPolicy { get; }

    /// <summary>
    /// Disposes the data port and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisposeAdditionalResourcesAsync().ConfigureAwait(false);
        if (_isConnected)
        {
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }

        await _queueProcessor.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override to dispose resources owned by the subclass. Called before the base class releases its own resources.
    /// No need to call <c>base.DisposeAdditionalResourcesAsync()</c>.
    /// </summary>
    protected virtual ValueTask DisposeAdditionalResourcesAsync() => ValueTask.CompletedTask;


    /// <summary>
    /// Template method: Connect to external system and start queue processing.
    /// Orchestrates connection, verification, and background task creation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        Client = await ClientLifecycleManager.AcquireConnectedAsync(ClientInformation, cancellationToken)
            .ConfigureAwait(false);

        await VerifyDataPoints(cancellationToken).ConfigureAwait(false);
        _queueProcessor.Start(Client, RetryPolicy);
        _isConnected = true;
    }


    /// <summary>
    /// Matches, converts, validates, and enqueues a batch of external values for writing.
    /// Drops the entire batch if any value fails validation.
    /// </summary>
    /// <param name="engineCycle">The engine cycle number for this batch</param>
    /// <param name="values">External values to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    public virtual Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values,
        CancellationToken cancellationToken)
    {
        var resolvedChannels = ResolveChannels(values);
        var dataPointValues = ToDataPointValues(resolvedChannels);
        var validationResult = _validator.ValidateAll(dataPointValues);
        if (validationResult.IsValid) return EnqueueAsync(validationResult.ValidValues);
        // TODO: Discuss inconsistent behaviour
        // Unmatched channels will just be logged and we write the rest, when there are some invalid-values we drop the whole batch
        _logger.DataPointValuesInvalid(validationResult.Failures.Count, dataPointValues.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Template method: Disconnect from external system and stop queue processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _isConnected = false;
        await _queueProcessor.StopAsync().ConfigureAwait(false);
        await ReleaseClientAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts an external value matched to a specific data point into a typed data point value.
    /// </summary>
    protected abstract TDataPointValue ConvertValue(ExternalValue externalValue, TDataPoint dataPoint);

    private async ValueTask<List<MisConfiguredDataPoint<TDataPoint>>> VerifyDataPoints(
        CancellationToken cancellationToken)
    {
        if (ConfigurationVerifier == null) return [];
        var misconfiguredDataPoints = await ConfigurationVerifier
            .Verify(DataPoints, cancellationToken)
            .ConfigureAwait(false);
        foreach (var misconfigured in misconfiguredDataPoints)
        {
            var misconfigurations =
                string.Join(", ", misconfigured.MisMatchingConfigurations.Select(static m => m.Value));
            _logger.MisConfiguredDataPoint(misconfigured.DataPoint.Identifier.Value, misconfigurations);
        }

        if (misconfiguredDataPoints.Count <= 0) return misconfiguredDataPoints;

        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        var exceptionMessage = $"Failed to connect as {misconfiguredDataPoints.Count} data point(s) are misconfigured.";
        throw new InvalidConfigurationException(exceptionMessage);
    }

    /// <summary>
    /// Enqueues external values for writing to the external system.
    /// </summary>
    /// <param name="dataPointValues">DataPoint-values to enqueue</param>
    /// <returns>Async task</returns>
    private Task EnqueueAsync(IReadOnlyList<TDataPointValue> dataPointValues)
    {
        if (dataPointValues.Count == 0) return Task.CompletedTask;
        _queueProcessor.Enqueue(dataPointValues);
        return Task.CompletedTask;
    }

    private async ValueTask ReleaseClientAsync(CancellationToken cancellationToken)
    {
        if (Client is null) return;
        await ClientLifecycleManager.ReleaseAsync(ClientInformation, cancellationToken)
            .ConfigureAwait(false);
        Client = default;
    }


    private List<ResolvedChannel<TDataPoint>> ResolveChannels(IReadOnlyCollection<ExternalValue> values)
    {
        var resolution = _channelResolver.Resolve(values);
        if (resolution.UnresolvedChannels.Count <= 0) return resolution.ResolvedChannels;
        var unresolvedChannelNames =
            resolution.UnresolvedChannels.Select(static channel => channel.ExternalValue.Channel);
        _logger.UnresolvedChannels(resolution.UnresolvedChannels.Count, string.Join(", ", unresolvedChannelNames));
        return resolution.ResolvedChannels;
    }

    [SuppressMessage("ReSharper", "ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator")]
    private List<TDataPointValue> ToDataPointValues(List<ResolvedChannel<TDataPoint>> resolvedChannels)
    {
        var dataPointValues = new List<TDataPointValue>(resolvedChannels.Count);
        foreach (var channel in resolvedChannels)
        {
            var dataPointValue = ConvertValue(channel.ExternalValue, channel.DataPoint);
            dataPointValues.Add(dataPointValue);
        }

        return dataPointValues;
    }
}
