using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Outgoing.Client;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Bounded;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;

/// <summary>
/// Manages the lifecycle of queue-based write operations for outgoing data ports.
/// Encapsulates queue creation, background processing, retry logic, and disposal.
/// </summary>
/// <typeparam name="TValue">Write value type</typeparam>
/// <remarks>
/// <para><b>Lifecycle:</b></para>
/// <list type="number">
/// <item>Call <see cref="Start"/> after connection is established to begin background processing</item>
/// <item>Call <see cref="Enqueue"/> to add batches of values to the queue</item>
/// <item>Call <see cref="StopAsync"/> to drain and stop background processing</item>
/// <item>Call <see cref="DisposeAsync"/> for final cleanup</item>
/// </list>
/// </remarks>
internal sealed class QueueProcessor<TValue> : IAsyncDisposable
{
    private readonly ChannelQueue<IReadOnlyList<TValue>> _queue;
    private readonly IDelayProvider _delayProvider;
    private readonly ILogger _logger;

    // Lifecycle state — null until Start() is called
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _queueTask;

    // Set by Start(), used in ProcessQueueAsync/PublishAsync
    private IWriteClient<TValue>? _writeClient;
    private IRetryPolicy? _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueProcessor{TDataPointValue}"/> class.
    /// </summary>
    /// <param name="queueConfiguration">Queue capacity and overflow strategy configuration</param>
    /// <param name="delayProvider">Delay provider for retry backoff</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="queueName">Name used for queue logging</param>
    internal QueueProcessor(
        QueueConfiguration queueConfiguration,
        IDelayProvider delayProvider,
        ILogger logger,
        string queueName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(queueConfiguration.QueueSize.Value);
        _delayProvider = delayProvider;
        _logger = logger;
        _queue = new ChannelQueue<IReadOnlyList<TValue>>(
            capacity: queueConfiguration.QueueSize.Value,
            boundedChannelFullMode: queueConfiguration.Strategy,
            logger: logger,
            name: queueName);
    }

    /// <summary>
    /// Appends a batch of data point values to the processing queue.
    /// </summary>
    internal void Enqueue(IReadOnlyList<TValue> values) => _queue.Append(values);

    /// <summary>
    /// Starts background queue processing. Supports reconnection via repeated calls.
    /// </summary>
    /// <param name="writeClient">Write client used to publish values to the external system</param>
    /// <param name="retryPolicy">Retry policy governing retry behaviour on write failures</param>
    internal void Start(IWriteClient<TValue> writeClient, IRetryPolicy retryPolicy)
    {
        _writeClient = writeClient;
        _retryPolicy = retryPolicy;

        _cancellationTokenSource = new CancellationTokenSource();
        _queueTask ??= ProcessQueueAsync();
    }

    /// <summary>
    /// Signals queue completion, cancels background processing, and waits for the task to stop.
    /// </summary>
    internal async Task StopAsync()
    {
        if (_cancellationTokenSource is not null)
        {
            // Signal that no more items will be added to the queue
            _queue.CompleteAdding();

            // Capture reference and clear field immediately to prevent re-entry
            var cts = _cancellationTokenSource;
            _cancellationTokenSource = null;

            await cts.CancelAsync().ConfigureAwait(false);
            cts.Dispose();
        }

        if (_queueTask is not null)
        {
            try
            {
                await _queueTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }

            _queueTask = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource is not null)
        {
            await StopAsync().ConfigureAwait(false);
        }

        _queue.Dispose();
    }

    private async Task ProcessQueueAsync()
    {
        // Capture token at start to avoid race condition with StopAsync setting _cancellationTokenSource to null
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

    private async ValueTask PublishAsync(IReadOnlyList<TValue> dataPointValues,
        CancellationToken cancellationToken)
    {
        long retryCount = 0;

        while (true)
        {
            try
            {
                // Always use CancellationToken.None for retries (don't cancel mid-write)
                await _writeClient!.WriteAsync(dataPointValues, CancellationToken.None).ConfigureAwait(false);
                return; // Success
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                retryCount++;

                // Check if we should continue retrying
                if (!_retryPolicy!.ShouldRetry(retryCount))
                {
                    _retryPolicy.OnMaxRetriesExceeded(retryCount, ex);
                    throw new MaxRetriesExceededException(retryCount,
                        $"Write operation failed after maximum retry attempts ({dataPointValues.Count} data points)",
                        ex);
                }

                // Calculate exponential backoff
                var delay = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs: 200, maxDelayMs: 1000);

                // Progressive logging
                LogRetryAttempt(retryCount, delay);

                await _delayProvider.DelayAsync(delay, cancellationToken).ConfigureAwait(false);

                // Backpressure detection: Check if this item is still at queue head
                if (_queue.IsItemStillHead(dataPointValues)) continue;
                _logger.AbandoningRetryDueToBackpressure(retryCount);
                throw new QueueBackpressureException();
            }
        }
    }

    /// <summary>
    /// Logs retry attempts with progressive verbosity:
    /// - Debug level: Attempts 1-4
    /// - Warning level: Attempt 5, then every 10th attempt (15, 20, 30, ...)
    /// </summary>
    /// <param name="retryCount">Current retry attempt number</param>
    /// <param name="delay">Backoff delay duration</param>
    internal void LogRetryAttempt(long retryCount, TimeSpan delay)
    {
        var delayMs = delay.TotalMilliseconds;

        switch (retryCount)
        {
            // Extended retry (5+): Log at 5, then every 10th
            case >= 5 when retryCount == 5 || retryCount % 10 == 0:
                _logger.ExtendedRetryInProgress(retryCount, delayMs);
                break;

            // Initial retries (1-4): Debug level
            case < 5:
                _logger.RetryingWriteOperation(retryCount, delayMs);
                break;
        }
    }
}
