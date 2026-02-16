using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Queue;

/// <summary>
/// Lock-free bounded queue with both Append and Prepend operations using dual-channel architecture.
/// Prepended items are always read before appended items. Supports configurable overflow strategy.
/// </summary>
/// <remarks>
/// Uses two separate unbounded channels internally with coordinated capacity management:
/// - Priority channel: receives prepended items (read first)
/// - Normal channel: receives appended items (read second)
/// This design enables lock-free operations for both Append and Prepend.
/// </remarks>
/// <typeparam name="T">The type of items in the queue.</typeparam>
#pragma warning disable CA1711
public sealed class PrependableChannelQueue<T> : ChannelQueueBase<T>
#pragma warning restore CA1711
    where T : notnull
{
    private readonly Channel<T> _priorityChannel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    private readonly Channel<T> _normalChannel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    // Lock only used for cached head state (requires atomic read/write of multiple fields)
    private readonly Lock _headLock = new();
    private T? _cachedHead;
    private HeadSource _headSource = HeadSource.None;

    // Count uses Interlocked for simple increment/decrement operations
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrependableChannelQueue{T}"/> class.
    /// </summary>
    /// <param name="capacity">Maximum queue capacity.</param>
    /// <param name="overflowStrategy">Strategy when queue is full.</param>
    /// <param name="logger">Logger for queue operations.</param>
    /// <param name="name">Name for logging purposes.</param>
    public PrependableChannelQueue(
        int capacity,
        QueueStrategy overflowStrategy,
        ILogger logger,
        string name)
        : base(capacity, overflowStrategy, logger, name)
    {
    }

    /// <summary>
    /// Gets whether adding has been completed via <see cref="CompleteAdding"/>.
    /// </summary>
    public override bool IsAddingCompleted { get; protected set; }

    /// <summary>
    /// Gets the current number of items in the queue.
    /// </summary>
    public override int Count => Volatile.Read(ref _count);

    /// <summary>
    /// Appends an item to the end of the queue (after all existing items).
    /// </summary>
    /// <param name="item">The item to append.</param>
    public override void Append(T item)
    {
        if (!TryMakeRoom())
        {
            _logger.LogFailedToWriteItemToChannel(_name);
            return;
        }

        if (_normalChannel.Writer.TryWrite(item))
        {
            Interlocked.Increment(ref _count);
        }
        else
        {
            _logger.LogFailedToWriteItemToChannel(_name);
        }
    }

    /// <summary>
    /// Appends multiple items from a span with optimized batch overflow handling.
    /// Zero-allocation batch operation with single count update.
    /// </summary>
    /// <param name="items">The items to append.</param>
    public override void Append(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty) return;

        var currentCount = Count;
        var available = _capacity - currentCount;
        var toAdd = items.Length;

        if (toAdd > available)
        {
            if (_overflowStrategy == QueueStrategy.DropNewest)
            {
                // Only add what fits
                toAdd = available;
            }
            else
            {
                // DropOldest: make room by dropping existing items
                // We can only keep 'capacity' items total, taking from the end of the input
                if (toAdd >= _capacity)
                {
                    // Clear the queue entirely and only add the last 'capacity' items
                    DropOldestItems(currentCount);
                    toAdd = _capacity;
                    items = items[^_capacity..];
                }
                else
                {
                    // Drop enough to fit all new items
                    var toDrop = toAdd - available;
                    DropOldestItems(toDrop);
                }
            }
        }

        var written = 0;
        for (var i = 0; i < toAdd; i++)
        {
            if (_normalChannel.Writer.TryWrite(items[i]))
                written++;
        }

        if (written > 0)
            Interlocked.Add(ref _count, written);
    }

    /// <summary>
    /// Prepends an item to the front of the queue (before all existing items).
    /// Multiple prepends maintain FIFO order within priority channel.
    /// </summary>
    /// <param name="item">The item to prepend.</param>
    public void Prepend(T item)
    {
        if (!TryMakeRoom())
        {
            _logger.LogFailedToWriteItemToChannel(_name);
            return;
        }

        if (_priorityChannel.Writer.TryWrite(item))
        {
            Interlocked.Increment(ref _count);
        }
        else
        {
            _logger.LogFailedToWriteItemToChannel(_name);
        }
    }

    /// <summary>
    /// Prepends multiple items from a span with optimized batch overflow handling.
    /// Zero-allocation batch operation with single count update.
    /// Items are prepended in order, so the first item in the span becomes the new head.
    /// </summary>
    /// <param name="items">The items to prepend.</param>
    public void Prepend(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty) return;

        var currentCount = Count;
        var available = _capacity - currentCount;
        var toAdd = items.Length;

        if (toAdd > available)
        {
            if (_overflowStrategy == QueueStrategy.DropNewest)
            {
                // Only add what fits (take from the beginning for prepend)
                toAdd = available;
            }
            else
            {
                // DropOldest: make room by dropping existing items
                // We can only keep 'capacity' items total
                if (toAdd >= _capacity)
                {
                    // Clear the queue entirely and only add the first 'capacity' items
                    DropOldestItems(currentCount);
                    toAdd = _capacity;
                }
                else
                {
                    // Drop enough to fit all new items
                    var toDrop = toAdd - available;
                    DropOldestItems(toDrop);
                }
            }
        }

        var written = 0;
        for (var i = 0; i < toAdd; i++)
        {
            if (_priorityChannel.Writer.TryWrite(items[i]))
                written++;
        }

        if (written > 0)
            Interlocked.Add(ref _count, written);
    }

    /// <summary>
    /// Prepends multiple items to the queue. Items are prepended in order, so the first
    /// item in the enumerable becomes the new head of the queue.
    /// Optimized for arrays and lists (uses span internally).
    /// </summary>
    /// <param name="items">The items to prepend.</param>
    public void Prepend(IEnumerable<T> items)
    {
        switch (items)
        {
            case T[] array:
                Prepend(array.AsSpan());
                break;
            case List<T> list:
                Prepend(CollectionsMarshal.AsSpan(list));
                break;
            default:
                foreach (var item in items)
                    Prepend(item);
                break;
        }
    }

    /// <summary>
    /// Signals that no more items will be added to the queue.
    /// </summary>
    public override void CompleteAdding()
    {
        IsAddingCompleted = true;
        try
        {
            _priorityChannel.Writer.Complete();
            _normalChannel.Writer.Complete();
        }
        catch (ChannelClosedException)
        {
            // Already closed, ignore
        }
    }

    /// <summary>
    /// Attempts to remove and return an item from the queue.
    /// Priority items (prepended) are returned before normal items (appended).
    /// </summary>
    /// <param name="item">When this method returns, contains the removed item if successful.</param>
    /// <returns>True if an item was successfully removed; otherwise, false.</returns>
    public override bool TryTake([NotNullWhen(true)] out T? item)
    {
        // First consume any cached head (requires lock for atomic state manipulation)
        lock (_headLock)
        {
            if (_headSource != HeadSource.None)
            {
                item = _cachedHead!;

                // Consume from the actual channel
                var sourceChannel = _headSource == HeadSource.Priority
                    ? _priorityChannel.Reader
                    : _normalChannel.Reader;
                sourceChannel.TryRead(out _);

                _cachedHead = default;
                _headSource = HeadSource.None;
                Interlocked.Decrement(ref _count);
                return true;
            }
        }

        // Try priority channel first, then normal (lock-free path)
        if (_priorityChannel.Reader.TryRead(out item))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }

        if (_normalChannel.Reader.TryRead(out item))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Asynchronously waits for work to become available in either channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the wait.</param>
    /// <returns>True if work is available; false if adding has completed and queue is empty.</returns>
    public override async Task<bool> WaitForWorkAsync(CancellationToken cancellationToken = default)
    {
        // Check if we have cached head or items in either channel
        lock (_headLock)
        {
            if (_headSource != HeadSource.None)
                return true;
        }

        if (_priorityChannel.Reader.TryPeek(out _))
            return true;
        if (_normalChannel.Reader.TryPeek(out _))
            return true;

        if (IsAddingCompleted)
            return false;

        // Wait on either channel to have data
        var priorityWait = _priorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
        var normalWait = _normalChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();

        var completedTask = await Task.WhenAny(priorityWait, normalWait);
        return await completedTask;
    }

    /// <summary>
    /// Removes all items from the queue.
    /// </summary>
    public override void Clear()
    {
        lock (_headLock)
        {
            _cachedHead = default;
            _headSource = HeadSource.None;
        }

        // Drain channels and reset count
        while (_priorityChannel.Reader.TryRead(out _)) { }
        while (_normalChannel.Reader.TryRead(out _)) { }
        Volatile.Write(ref _count, 0);
    }

    /// <summary>
    /// Peeks at the next item in the queue without removing it.
    /// Priority items (prepended) are returned before normal items (appended).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the wait.</param>
    /// <returns>The peeked item, or default if both channels are completed.</returns>
    public override async Task<T?> PeekAsync(CancellationToken cancellationToken = default)
    {
        // Check cached head first
        lock (_headLock)
        {
            if (_headSource != HeadSource.None)
                return _cachedHead;
        }

        // Try to get from priority channel first
        if (_priorityChannel.Reader.TryPeek(out var priorityItem))
        {
            CacheHead(priorityItem, HeadSource.Priority);
            return priorityItem;
        }

        // Try to get from normal channel
        if (_normalChannel.Reader.TryPeek(out var normalItem))
        {
            CacheHead(normalItem, HeadSource.Normal);
            return normalItem;
        }

        // Wait for either channel to have data
        while (true)
        {
            var priorityWait = _priorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var normalWait = _normalChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();

            var completedTask = await Task.WhenAny(priorityWait, normalWait);
            var hasData = await completedTask;

            if (!hasData)
            {
                // One channel completed, check if the other has data
                if (_priorityChannel.Reader.TryPeek(out var pItem))
                {
                    CacheHead(pItem, HeadSource.Priority);
                    return pItem;
                }

                if (_normalChannel.Reader.TryPeek(out var nItem))
                {
                    CacheHead(nItem, HeadSource.Normal);
                    return nItem;
                }

                // Check if both channels are completed
                if (_priorityChannel.Reader.Completion.IsCompleted && _normalChannel.Reader.Completion.IsCompleted)
                    return default;

                continue;
            }

            // Check priority first (even if normal completed first, priority takes precedence)
            if (_priorityChannel.Reader.TryPeek(out var peekPriority))
            {
                CacheHead(peekPriority, HeadSource.Priority);
                return peekPriority;
            }

            if (_normalChannel.Reader.TryPeek(out var peekNormal))
            {
                CacheHead(peekNormal, HeadSource.Normal);
                return peekNormal;
            }
        }
    }

    /// <summary>
    /// Removes the specified item from the queue if it is still at the head.
    /// This provides retry-safe processing by verifying the item hasn't been superseded.
    /// </summary>
    /// <param name="item">The item to mark as read.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    public override Task MarkItemAsReadAsync(T item, CancellationToken cancellationToken)
    {
        _ = cancellationToken; // Reserved for future use

        if (!IsItemStillHead(item))
            return Task.CompletedTask;

        lock (_headLock)
        {
            if (_headSource == HeadSource.None)
                return Task.CompletedTask;

            var sourceChannel = _headSource == HeadSource.Priority
                ? _priorityChannel.Reader
                : _normalChannel.Reader;

            // Clear the cached head
            _cachedHead = default;
            _headSource = HeadSource.None;

            // Actually consume the item from the channel and decrement count
            if (sourceChannel.TryRead(out _))
            {
                Interlocked.Decrement(ref _count);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the specified item is still at the head of the queue.
    /// Priority items supersede normal items even if the normal item was peeked first.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item is still at the head; otherwise, false.</returns>
    public override bool IsItemStillHead(T item)
    {
        // Always check priority channel first - a new prepended item supersedes
        // any previously peeked normal item
        if (_priorityChannel.Reader.TryPeek(out var priorityItem))
            return priorityItem.Equals(item);

        lock (_headLock)
        {
            // If we have a cached head and no priority items, check cached head
            if (_headSource != HeadSource.None)
                return _cachedHead!.Equals(item);
        }

        // No cached head and no priority items - check normal channel
        if (_normalChannel.Reader.TryPeek(out var normalItem))
            return normalItem.Equals(item);

        return false;
    }

    /// <summary>
    /// Releases the resources used by the queue.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _priorityChannel.Writer.Complete();
            }
            catch (ChannelClosedException) { }

            try
            {
                _normalChannel.Writer.Complete();
            }
            catch (ChannelClosedException) { }
        }
    }

    /// <summary>
    /// Returns an async enumerator that iterates through the queue.
    /// Priority items are yielded before normal items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the enumeration.</param>
    /// <returns>An async enumerator for the queue.</returns>
    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new PrependableChannelQueueEnumerator<T>(this, cancellationToken);

    /// <summary>
    /// Tries to make room for a new item by potentially dropping items according to overflow strategy.
    /// </summary>
    /// <returns>True if room is available; false if the new item should be dropped (DropNewest).</returns>
    private bool TryMakeRoom()
    {
        if (Count < _capacity)
            return true;

        return _overflowStrategy switch
        {
            QueueStrategy.DropOldest => TryDropOldest(),
            QueueStrategy.DropNewest => false, // Reject new item
            _ => false
        };
    }

    /// <summary>
    /// Drops the oldest item to make room. Tries normal channel first (preserves priority items).
    /// </summary>
    private bool TryDropOldest()
    {
        // First check if we have a cached head - that's the oldest item (requires lock)
        lock (_headLock)
        {
            if (_headSource != HeadSource.None)
            {
                var sourceChannel = _headSource == HeadSource.Priority
                    ? _priorityChannel.Reader
                    : _normalChannel.Reader;

                _cachedHead = default;
                _headSource = HeadSource.None;

                // Consume from channel
                if (sourceChannel.TryRead(out _))
                {
                    Interlocked.Decrement(ref _count);
                }

                return true;
            }
        }

        // Try to drop from normal channel first (preserves priority items) - lock-free path
        if (_normalChannel.Reader.TryRead(out _))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }

        // If normal is empty, drop from priority channel
        if (_priorityChannel.Reader.TryRead(out _))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Drops multiple oldest items in a single pass with optimized count updates.
    /// Tries normal channel first (preserves priority items).
    /// </summary>
    /// <param name="count">The number of items to drop.</param>
    private void DropOldestItems(int count)
    {
        var dropped = 0;

        // First clear cached head if present (single lock acquisition)
        lock (_headLock)
        {
            if (_headSource != HeadSource.None && dropped < count)
            {
                var sourceChannel = _headSource == HeadSource.Priority
                    ? _priorityChannel.Reader
                    : _normalChannel.Reader;

                _cachedHead = default;
                _headSource = HeadSource.None;

                if (sourceChannel.TryRead(out _))
                    dropped++;
            }
        }

        // Drop from normal channel first (preserves priority items)
        while (dropped < count && _normalChannel.Reader.TryRead(out _))
            dropped++;

        // If needed, drop from priority channel
        while (dropped < count && _priorityChannel.Reader.TryRead(out _))
            dropped++;

        // Single count update for all dropped items
        if (dropped > 0)
            Interlocked.Add(ref _count, -dropped);
    }

    private void CacheHead(T item, HeadSource source)
    {
        lock (_headLock)
        {
            // Cache the head for peek/mark semantics
            // The item is still in the channel, count doesn't change
            _cachedHead = item;
            _headSource = source;
        }
    }
}
