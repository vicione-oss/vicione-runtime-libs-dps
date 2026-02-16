using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Outgoing.Utilities;
using ViciOne.Suite.DataPort.Extensions.Queue;
using ViciOne.Suite.DataPort.Extensions.Resilience;
using ViciOne.Suite.DataPort.Extensions.ValueValidation;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing;

/// <summary>
/// Base class for OutgoingDataPort implementations.
/// Implements Template Method pattern with strategy hooks for retry policies, write clients, and value factories.
/// </summary>
/// <typeparam name="TDataPoint">Configuration type for data points</typeparam>
/// <typeparam name="TDataPointValue">Write value type</typeparam>
/// <typeparam name="TClient">Connection client type for lifecycle management</typeparam>
/// <remarks>
/// <para><b>Design Patterns:</b></para>
/// <list type="bullet">
/// <item><b>Template Method:</b> ConnectAsync, DisconnectAsync, ProcessQueueAsync orchestrate common flow</item>
/// <item><b>Strategy:</b> IRetryPolicy, IWriteClient, IDataPointValueFactory abstract implementation-specific behavior</item>
/// <item><b>Factory:</b> IDataPointValueFactory creates validated data point values</item>
/// </list>
/// <para><b>Performance Optimizations:</b></para>
/// <list type="bullet">
/// <item>Pre-allocated List capacity in GetDataPointValues (avoids resizing)</item>
/// <item>Indexed for loops (no LINQ) in hot paths</item>
/// <item>O(1) channel lookup via Dictionary</item>
/// <item>All-or-nothing validation semantics (fails fast)</item>
/// </list>
/// </remarks>
public abstract class OutgoingDataPortBase<TDataPoint, TDataPointValue, TClient> : IAsyncDisposable
    where TDataPoint : class, IDataPoint
    where TClient : class
    where TDataPointValue : IDataPointValue<TDataPoint>
{
    // Queue and dependencies
    private readonly ChannelQueue<IReadOnlyList<TDataPointValue>> _queue;
    private readonly ILogger _logger;
    private readonly IDelayProvider _delayProvider;
    private readonly IReadOnlyDictionary<string, TDataPoint> _channelToDataPointMap;
    private readonly DataPointValueValidator<TDataPoint, TDataPointValue> _dataPointValuesValidator;

    // Lifecycle state
    private Task? _queueTask;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Gets the retry policy for write operations (implementation-specific).
    /// </summary>
    protected abstract IRetryPolicy RetryPolicy { get; }

    /// <summary>
    /// Gets the write client for external system write operations (implementation-specific).
    /// </summary>
    protected abstract IWriteClient<TDataPointValue> WriteClient { get; }

    /// <summary>
    /// Gets the value factory for creating validated data point values (implementation-specific).
    /// </summary>
    protected abstract IDataPointValueFactory<TDataPoint, TDataPointValue> ValueFactory { get; }

    /// <summary>
    /// Acquires a connected client instance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connected client instance</returns>
    protected abstract Task<TClient> AcquireConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Releases the connection and performs cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    protected abstract Task ReleaseConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Validates the connection after acquisition.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    protected abstract Task ValidateConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Initializes a new instance of the <see cref="OutgoingDataPortBase{TDataPoint, TDataPointValue, TClient}"/> class.
    /// </summary>
    /// <param name="dataPoints">Collection of data point configurations</param>
    /// <param name="channelSelector">Function to extract channels from a data point</param>
    /// <param name="queueCapacity">Maximum queue capacity</param>
    /// <param name="overflowStrategy">Strategy for handling queue overflow (DropOldest or DropNewest)</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="delayProvider">Delay provider for retry backoff (defaults to Task.Delay)</param>
    /// <param name="timeProvider">Time provider for testing (not used by base class, available for derived classes)</param>
#pragma warning disable IDE0060 // Remove unused parameter - kept for derived class usage
    protected OutgoingDataPortBase(
        IReadOnlyList<TDataPoint> dataPoints,
        Func<TDataPoint, IReadOnlyList<string>> channelSelector,
        int queueCapacity,
        QueueStrategy overflowStrategy,
        ILogger logger,
        IDelayProvider? delayProvider = null,
        TimeProvider? timeProvider = null)
#pragma warning restore IDE0060
    {
        ArgumentNullException.ThrowIfNull(dataPoints);
        ArgumentNullException.ThrowIfNull(channelSelector);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(queueCapacity);

        _logger = logger;
        _delayProvider = delayProvider ?? new DefaultDelayProvider();

        // Build O(1) channel lookup
        _channelToDataPointMap = ChannelLookupBuilder.BuildLookup(dataPoints, channelSelector);

        // Initialize queue
        _queue = new ChannelQueue<IReadOnlyList<TDataPointValue>>(
            capacity: queueCapacity,
            boundedChannelFullMode: overflowStrategy,
            logger: logger,
            name: $"{GetType().Name}Queue");

        _dataPointValuesValidator = new DataPointValueValidator<TDataPoint, TDataPointValue>(logger);
    }

    /// <summary>
    /// Template method: Connect to external system and start queue processing.
    /// Orchestrates connection, validation, and background task creation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await AcquireConnectionAsync(cancellationToken).ConfigureAwait(false);
        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        _cancellationTokenSource = new CancellationTokenSource();
        _queueTask ??= ProcessQueueAsync();
    }

    /// <summary>
    /// Template method: Disconnect from external system and stop queue processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async task</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is not null)
        {
            // Signal that no more items will be added to the queue
            _queue.CompleteAdding();

            // Capture reference and clear field immediately to prevent re-entry from DisposeAsync
            var cts = _cancellationTokenSource;
            _cancellationTokenSource = null;

            // Cancel background processing
            await cts.CancelAsync();
            cts.Dispose();
        }

        // Wait for queue processing to stop
        if (_queueTask is not null)
        {
            try
            {
                await _queueTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            _queueTask = null;
        }

        // Release connection (implementation-specific)
        await ReleaseConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the data port and releases all resources.
    /// </summary>
    /// <returns>Async task</returns>
    public async ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource is not null)
        {
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }

        _queue.Dispose();
        DisposeClient();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Provides an optional hook for derived classes to dispose of client-specific resources.
    /// </summary>
    /// <remarks>
    /// This method is called during disposal but is currently not invoked by the base class.
    /// Derived classes can override this method to perform cleanup of their client instances
    /// if needed. Consider whether this logic should be moved to <see cref="ReleaseConnectionAsync"/>
    /// or called from <see cref="DisposeAsync"/>.
    /// </remarks>
    protected virtual void DisposeClient() { }

    /// <summary>
    /// Sends external values to the external system via the queue.
    /// </summary>
    /// <param name="values">External values to send</param>
    /// <returns>Async task</returns>
    public Task SendAsync(IReadOnlyCollection<ExternalValue> values)
    {
        if (values.Count == 0) return Task.CompletedTask;

        var dataPointValues = GetDataPointValues(values);
        if (dataPointValues.Count == 0)
            return Task.CompletedTask;

        _queue.Append(dataPointValues);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Transforms external values to data point values with batch validation.
    /// Uses two-phase approach: mapping then validation for complete error diagnostics.
    /// </summary>
    /// <param name="values">External values from DataPort</param>
    /// <returns>List of validated data point values, or empty list on validation failure</returns>
    /// <remarks>
    /// Uses all-or-nothing semantics: If any value fails validation, the entire batch is rejected.
    /// This prevents partial writes that could leave the target system in an inconsistent state.
    /// Phase 1: Map channels to data points, collecting unmapped channels
    /// Phase 2: Create and validate all values, collecting all validation failures
    /// </remarks>
    protected IReadOnlyList<TDataPointValue> GetDataPointValues(IReadOnlyCollection<ExternalValue> values)
    {
        if (values.Count == 0) return [];

        var mappingResult = MapExternalValuesToDataPointValues(values);
        if (mappingResult.UnmappedChannels.Count > 0)
        {
            _logger.UnmappedChannelsBatch(mappingResult.UnmappedChannels.Count,
                string.Join(", ", mappingResult.UnmappedChannels));
            return [];
        }

        var validationResult = _dataPointValuesValidator.ValidateAll(mappingResult.MappedValues);
        if (!validationResult.IsValid)
        {
            _logger.BatchValidationFailed(validationResult.Failures.Count, values.Count);
            return [];
        }

        return validationResult.ValidValues;
    }

    /// <summary>
    /// Maps external values to data point values, collecting any unmapped channels.
    /// </summary>
    /// <param name="externalValues">The external values to map.</param>
    /// <returns>A result containing mapped DataPointValues and any unmapped channel names.</returns>
    private ExternalValuesMappingResult<TDataPoint, TDataPointValue> MapExternalValuesToDataPointValues(
        IReadOnlyCollection<ExternalValue> externalValues)
    {
        var dataPointValues = new List<TDataPointValue>(externalValues.Count);
        var unmappedChannels = new List<string>();

        foreach (var externalValue in externalValues)
        {
            if (_channelToDataPointMap.TryGetValue(externalValue.Channel, out var dataPoint))
            {
                dataPointValues.Add(ValueFactory.Create(dataPoint, externalValue.Value));
            }
            else
            {
                unmappedChannels.Add(externalValue.Channel);
            }
        }

        return new ExternalValuesMappingResult<TDataPoint, TDataPointValue>(dataPointValues, unmappedChannels);
    }

    /// <summary>
    /// Background task that processes queued write operations with retry logic.
    /// </summary>
    /// <returns>Async task</returns>
    private async Task ProcessQueueAsync()
    {
        // Capture token at start to avoid race condition with DisconnectAsync setting _cancellationTokenSource to null
        var token = _cancellationTokenSource!.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var item = await _queue.PeekAsync(token).ConfigureAwait(false);
                if (item is null)
                {
                    await _delayProvider.DelayAsync(TimeSpan.FromMilliseconds(10), token)
                        .ConfigureAwait(false);
                    continue;
                }

                try
                {
                    await PublishAsync(item, token).ConfigureAwait(false);
                    await _queue.MarkItemAsReadAsync(item, token).ConfigureAwait(false);
                }
                catch (QueueBackpressureException)
                {
                    // Item dropped due to backpressure during retry
                    _logger.ItemDroppedDueToBackpressure();
                    await _queue.MarkItemAsReadAsync(item, token).ConfigureAwait(false);
                }
                catch (MaxRetriesExceededException ex)
                {
                    _logger.MaxRetriesExceeded(ex);
                    await _queue.MarkItemAsReadAsync(item, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation during shutdown
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during disconnect
        }
    }

    /// <summary>
    /// Publishes data point values to the external system with exponential backoff retry logic.
    /// </summary>
    /// <param name="dataPointValues">Values to write</param>
    /// <param name="cancellationToken">Cancellation token for shutdown (retries always use CancellationToken.None)</param>
    /// <returns>Async task</returns>
    /// <exception cref="MaxRetriesExceededException">Thrown when retry policy exhausted</exception>
    /// <exception cref="QueueBackpressureException">Thrown when item superseded by queue overflow</exception>
    private async ValueTask PublishAsync(IReadOnlyList<TDataPointValue> dataPointValues,
        CancellationToken cancellationToken)
    {
        long retryCount = 0;

        while (true)
        {
            try
            {
                // Always use CancellationToken.None for retries (don't cancel mid-write)
                await WriteClient.WriteAsync(dataPointValues, CancellationToken.None).ConfigureAwait(false);
                return; // Success
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                retryCount++;

                // Check if we should continue retrying
                if (!RetryPolicy.ShouldRetry(retryCount))
                {
                    RetryPolicy.OnMaxRetriesExceeded(retryCount, ex);
                    throw new MaxRetriesExceededException(retryCount,
                        $"Write operation failed after maximum retry attempts ({dataPointValues.Count} data points)",
                        ex);
                }

                // Calculate exponential backoff
                var delay = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs: 200, maxDelayMs: 1000);

                // Progressive logging
                RetryLogger.LogRetryAttempt(retryCount, delay, _logger);

                await _delayProvider.DelayAsync(delay, cancellationToken).ConfigureAwait(false);

                // Backpressure detection: Check if this item is still at queue head
                if (!_queue.IsItemStillHead(dataPointValues))
                {
                    _logger.AbandoningRetryDueToBackpressure(retryCount);
                    throw new QueueBackpressureException();
                }
            }
        }
    }
}
