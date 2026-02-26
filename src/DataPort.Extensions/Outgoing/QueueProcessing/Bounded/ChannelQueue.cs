using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Bounded;

/// <summary>
/// Simple bounded channel-based queue with configurable overflow strategy (DropOldest or DropNewest).
/// Supports Peek/Dequeue pattern for retry-safe processing with backpressure detection.
/// </summary>
#pragma warning disable CA1711
public sealed class ChannelQueue<T> : ChannelQueueBase<T>
    where T : notnull
{
    private readonly Channel<T> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Maximum queue capacity.</param>
    /// <param name="boundedChannelFullMode">Strategy when queue is full.</param>
    /// <param name="logger">Logger for queue operations.</param>
    /// <param name="name">Name for logging purposes.</param>
    public ChannelQueue(
        int capacity,
        QueueStrategy boundedChannelFullMode,
        ILogger logger,
        string name)
        : base(capacity, boundedChannelFullMode, logger, name)
    {
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = boundedChannelFullMode switch
            {
                QueueStrategy.DropOldest => BoundedChannelFullMode.DropOldest,
                QueueStrategy.DropNewest => BoundedChannelFullMode.DropNewest,
                _ => throw new ArgumentOutOfRangeException(nameof(boundedChannelFullMode), boundedChannelFullMode, null)
            },
            SingleReader = true,
            // SingleWriter is intentionally not set (defaults to false).
            // Append may be called from multiple threads (e.g. via OutgoingDataPortBase.EnqueueAsync
            // which is reachable from SendAsync, an external entry point with no thread-affinity guarantee).
            // SingleWriter = true removes write-side locking and would cause silent data corruption.
        });
    }

    /// <summary>
    /// Gets whether adding has been completed via <see cref="CompleteAdding"/>.
    /// </summary>
    public override bool IsAddingCompleted { get; protected set; }

    /// <summary>
    /// Gets the current number of items in the queue, including any peeked item.
    /// </summary>
    public override int Count => _channel.Reader.Count;

    /// <summary>
    /// Appends an item to the queue. If the queue is full, behavior depends on the configured strategy:
    /// DropOldest removes the oldest item, DropNewest removes the newest (current) item.
    /// </summary>
    /// <param name="item">The item to append to the queue.</param>
    /// <remarks>
    /// This method is thread-safe for single-writer scenarios.
    /// When the queue is at capacity, the configured <see cref="QueueStrategy"/> determines which item is dropped.
    /// </remarks>
    public override void Append(T item)
    {
        var wasAtCapacity = Count >= _capacity;
        var wasWritten = _channel.Writer.TryWrite(item);

        if (!wasWritten)
        {
            _logger.LogFailedToWriteItemToChannel(_name);
            return;
        }

        if (!wasAtCapacity) return;
        switch (_overflowStrategy)
        {
            case QueueStrategy.DropNewest:
                _logger.LogQueueDroppedNewestItem(_name, _capacity);
                return;
            case QueueStrategy.DropOldest:
                _logger.LogQueueDroppedOldestItem(_name, _capacity);
                return;
        }
    }

    /// <summary>
    /// Appends multiple items to the queue from a span. Zero-allocation batch operation.
    /// </summary>
    /// <param name="items">The items to append to the queue.</param>
    public override void Append(ReadOnlySpan<T> items)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < items.Length; i++)
        {
            Append(items[i]);
        }
    }

    /// <summary>
    /// Signals that no more items will be added to the queue.
    /// Subsequent calls to <see cref="PeekAsync"/> will complete when the queue is empty.
    /// </summary>
    public override void CompleteAdding()
    {
        IsAddingCompleted = true;
        try
        {
            _channel.Writer.Complete();
        }
        catch (ChannelClosedException)
        {
            // Already closed, ignore
        }
    }

    /// <summary>
    /// Attempts to remove and return an item from the queue.
    /// </summary>
    /// <param name="item">When this method returns, contains the removed item if successful; otherwise, the default value.</param>
    /// <returns>True if an item was successfully removed; otherwise, false.</returns>
    public override bool TryTake([NotNullWhen(true)] out T? item)
        => _channel.Reader.TryRead(out item);

    /// <summary>
    /// Asynchronously waits for work to become available in the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the wait operation.</param>
    /// <returns>True if work is available; false if adding has completed and the queue is empty.</returns>
    public override async Task<bool> WaitForWorkAsync(CancellationToken cancellationToken = default)
    {
        if (_channel.Reader.TryPeek(out _))
            return true;
        if (IsAddingCompleted)
            return false;
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }

    /// <summary>
    /// Removes all items from the queue.
    /// </summary>
    public override void Clear()
    {
        while (_channel.Reader.TryRead(out _)) { }
    }

    /// <summary>
    /// Peeks at the next item in the queue without removing it.
    /// Waits asynchronously until an item is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the wait operation.</param>
    /// <returns>The peeked item, or default if the channel is completed.</returns>
    public override async Task<T?> PeekAsync(CancellationToken cancellationToken = default)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_channel.Reader.TryPeek(out var peekedItem))
            {
                return peekedItem;
            }
        }

        return default;
    }

    /// <summary>
    /// Removes the specified item from the queue if it is still at the head, advancing to the next item.
    /// This provides retry-safe processing by verifying the item hasn't been superseded.
    /// </summary>
    /// <param name="item">The item to mark as read (must match the current head).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that completes when the item is removed (if it was still the head).</returns>
    /// <remarks>
    /// Must be called after <see cref="PeekAsync"/> to remove the peeked item.
    /// If the item is no longer at the head (e.g., superseded by DropNewest strategy), it is not removed.
    /// </remarks>
    public override async Task MarkItemAsReadAsync(T item, CancellationToken cancellationToken)
    {
        if (IsItemStillHead(item))
        {
            await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks if the specified item is still at the head of the queue.
    /// Used to verify that a previously peeked item hasn't been superseded by newer items
    /// before marking it as read (particularly important during error/retry scenarios).
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item is still at the head of the queue; otherwise, false.</returns>
    public override bool IsItemStillHead(T item) =>
        _channel.Reader.TryPeek(out var peekedItem) && peekedItem.Equals(item);

    /// <summary>
    /// Releases the resources used by the queue.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    /// <remarks>
    /// This method signals that no more items will be added and releases associated resources.
    /// It is safe to call multiple times.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _channel.Writer.Complete();
            }
            catch (ChannelClosedException)
            {
                // Already closed, ignore
            }
        }
    }

    /// <summary>
    /// Returns an async enumerator that iterates through the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the enumeration.</param>
    /// <returns>An async enumerator for the queue.</returns>
    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new ChannelQueueEnumerator<T>(_channel.Reader, cancellationToken);
}
