using System;
using System.Collections.Generic;
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
    where TDataPointValue : IDataPointValue
    where TWriteClient : IWriteClient<TDataPointValue>
    where TRootNode : IRootConfigurationNode<TCommunication>
    where TCommunication : DataPortCommunication
    where TClientInformation : IClientInformation
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly ChannelResolver<TDataPoint> _channelResolver;
    private readonly QueueConfiguration _queueConfiguration;
    private readonly IDelayProvider _delayProvider;
    private readonly DataPointValueValidator<TDataPoint, TDataPointValue> _validator;

    private QueueProcessor<TDataPointValue>? _queueProcessor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance.
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
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(GetType());
        _queueConfiguration = queueConfiguration;
        _delayProvider = delayProvider;
        DeviceNode = typedNodeMapper.MapToTypedNodes(communication);
        DataPoints = mapper.ToDataPoints(DeviceNode);
        _channelResolver = new ChannelResolver<TDataPoint>(DataPoints);
        _validator = new DataPointValueValidator<TDataPoint, TDataPointValue>(
            loggerFactory.CreateLogger<DataPointValueValidator<TDataPoint, TDataPointValue>>());
    }

    /// <summary>
    /// The information required to uniquely identify and create a client.
    /// </summary>
    public abstract TClientInformation ClientInformation { get; }

    /// <summary>Gets the root configuration node derived from the communication object.</summary>
    public TRootNode DeviceNode { get; }

    /// <summary>Gets the data points mapped from the root configuration node.</summary>
    public IReadOnlyList<TDataPoint> DataPoints { get; }

    /// <summary>
    /// Manager for acquiring and releasing client connections.
    /// </summary>
    protected abstract IClientLifecycleManager<TWriteClient, TClientInformation> ClientLifecycleManager { get; }

    /// <summary>
    /// Gets the retry policy for write operations (implementation-specific).
    /// </summary>
    protected virtual IRetryPolicy RetryPolicy => new InfiniteRetryPolicy();

    /// <summary>
    /// The client used for writing datapoints. Will be set after initial connect.
    /// </summary>
    protected TWriteClient? Client { get; private set; }

    /// <summary>
    /// Template method: Connect to external system and start queue processing.
    /// Orchestrates connection, verification, and background task creation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (Client is not null) return;
        Client = await ClientLifecycleManager.AcquireConnectedAsync(ClientInformation, cancellationToken)
            .ConfigureAwait(false);

        var misconfiguredDataPoints = await VerifyDataPoints(Client, cancellationToken).ConfigureAwait(false);
        if (misconfiguredDataPoints.Count > 0)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidConfigurationException("Failed to connect as there are misconfigured dataPoints.");
        }

        StartWritingDataPoints(Client);
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
        var conversionResult = ConvertValues(resolvedChannels);
        // TODO: Discuss inconsistent behaviour
        // Unmatched channels will just be logged and we write the rest, when there are some invalid-values we drop the whole batch
        if (!conversionResult.AllConverted)
        {
            _logger.DataPointValuesInvalid(conversionResult.Failures.Count, resolvedChannels.Count);
            return Task.CompletedTask;
        }

        var validationResult = _validator.ValidateAll(conversionResult.ConvertedValues);
        if (validationResult.IsValid) return EnqueueAsync(validationResult.ValidValues);
        _logger.DataPointValuesInvalid(validationResult.Failures.Count, conversionResult.ConvertedValues.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Template method: Disconnect from external system and stop queue processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (Client is null) return;
        await StopWritingDataPointsAsync().ConfigureAwait(false);
        await ReleaseClientAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the data port and releases all resources.
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
    protected virtual IDataPointConfigurationVerifier<TDataPoint>? CreateConfigurationVerifier(TWriteClient client) =>
        null;

    /// <summary>
    /// Converts an external value, matched to a specific data point, into a strongly-typed data point value.
    /// </summary>
    /// <param name="externalValue">The external value to convert.</param>
    /// <param name="dataPoint">The data point the value was matched to.</param>
    /// <returns>
    /// A <see cref="DataPointValueConversion{TDataPointValue}"/> carrying either the converted value
    /// (<see cref="ConvertedDataPointValue{TDataPointValue}"/>) or a <see cref="DataPointValueValidationFailure"/>
    /// describing why the value could not be represented as the data point's configured .NET type
    /// (<see cref="NotConvertedDataPointValue{TDataPointValue}"/>).
    /// </returns>
    protected abstract DataPointValueConversion<TDataPointValue> ConvertToDataPointValue(
        ExternalValue externalValue, TDataPoint dataPoint);

    /// <summary>
    /// Override to dispose resources owned by the subclass. Called before the base class releases its own resources.
    /// No need to call <c>base.DisposeAdditionalResourcesAsync()</c>.
    /// </summary>
    protected virtual ValueTask DisposeAdditionalResourcesAsync() => ValueTask.CompletedTask;

    private void StartWritingDataPoints(TWriteClient client)
    {
        _queueProcessor = new QueueProcessor<TDataPointValue>(
            _queueConfiguration,
            _delayProvider,
            _loggerFactory.CreateLogger<QueueProcessor<TDataPointValue>>(),
            $"{GetType().Name}Queue",
            client,
            RetryPolicy);
        _queueProcessor.Start();
    }

    private async ValueTask<IReadOnlyList<MisconfiguredDataPoint<TDataPoint>>> VerifyDataPoints(
        TWriteClient client, CancellationToken cancellationToken)
    {
        var verifier = CreateConfigurationVerifier(client);
        if (verifier == null) return [];
        var misconfiguredDataPoints = await verifier
            .Verify(DataPoints, cancellationToken)
            .ConfigureAwait(false);
        foreach (var misconfigured in misconfiguredDataPoints)
        {
            var misconfigurations =
                string.Join(", ", misconfigured.MismatchingConfigurations.Select(static m => m.Value));
            _logger.MisConfiguredDataPoint(misconfigured.DataPoint.Identifier.Value, misconfigurations);
        }

        return misconfiguredDataPoints;
    }

    private async Task StopWritingDataPointsAsync()
    {
        if (_queueProcessor is null) return;
        await _queueProcessor.DisposeAsync().ConfigureAwait(false);
        _queueProcessor = null;
    }

    private async ValueTask ReleaseClientAsync(CancellationToken cancellationToken)
    {
        if (Client is null) return;
        await ClientLifecycleManager.ReleaseAsync(ClientInformation, cancellationToken)
            .ConfigureAwait(false);
        Client = default;
    }

    private IReadOnlyList<ResolvedChannel<TDataPoint>> ResolveChannels(IReadOnlyCollection<ExternalValue> values)
    {
        var resolution = _channelResolver.Resolve(values);
        if (resolution.UnresolvedChannels.Count <= 0) return resolution.ResolvedChannels;
        var unresolvedChannelNames =
            resolution.UnresolvedChannels.Select(static channel => channel.ExternalValue.Channel);
        _logger.UnresolvedChannels(resolution.UnresolvedChannels.Count, string.Join(", ", unresolvedChannelNames));
        return resolution.ResolvedChannels;
    }

    private DataPointValuesConversionResult<TDataPointValue> ConvertValues(
        IReadOnlyList<ResolvedChannel<TDataPoint>> resolvedChannels)
    {
        var converted = new List<TDataPointValue>(resolvedChannels.Count);
        var failures = new List<DataPointValueValidationFailure>();
        foreach (var channel in resolvedChannels)
        {
            switch (ConvertToDataPointValue(channel.ExternalValue, channel.DataPoint))
            {
                case ConvertedDataPointValue<TDataPointValue> c:
                    converted.Add(c.Value);
                    break;
                case NotConvertedDataPointValue<TDataPointValue> nc:
                    _logger.ConversionFailed(channel.ExternalValue.Channel, nc.Failure.Details);
                    failures.Add(nc.Failure);
                    break;
            }
        }

        return new DataPointValuesConversionResult<TDataPointValue>(
            ConvertedValues: converted,
            Failures: failures);
    }

    private Task EnqueueAsync(IReadOnlyList<TDataPointValue> dataPointValues)
    {
        if (dataPointValues.Count == 0) return Task.CompletedTask;
        _queueProcessor?.Enqueue(dataPointValues);
        return Task.CompletedTask;
    }
}
