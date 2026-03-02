using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Bounded;

/// <summary>
/// Abstract base class for channel-based queues providing common functionality
/// for Append(IEnumerable), Take, and TakeTo operations.
/// </summary>
/// <typeparam name="T">The type of items in the queue.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ChannelQueueBase{T}"/> class.
/// </remarks>
/// <param name="capacity">Maximum queue capacity.</param>
/// <param name="overflowStrategy">Strategy when queue is full.</param>
/// <param name="logger">Logger for queue operations.</param>
/// <param name="name">Name for logging purposes.</param>
#pragma warning disable CA1711
public abstract class ChannelQueueBase<T>(
    int capacity,
    QueueStrategy overflowStrategy,
    ILogger logger,
    string name) : IAsyncEnumerable<T>, IDisposable
    where T : notnull
{
    /// <summary>
    /// Maximum capacity of the queue.
    /// </summary>
    private protected readonly int _capacity = capacity;

    /// <summary>
    /// Strategy to use when the queue is full (DropOldest or DropNewest).
    /// </summary>
    private protected readonly QueueStrategy _overflowStrategy = overflowStrategy;

    /// <summary>
    /// Logger instance for queue operations.
    /// </summary>
    private protected readonly ILogger _logger = logger;

    /// <summary>
    /// Name of the queue for logging purposes.
    /// </summary>
    private protected readonly string _name = name;

    /// <summary>
    /// Gets the maximum capacity of the queue.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets whether the queue is still working (has items or can receive more).
    /// </summary>
    public bool IsWorking => Count > 0 || !IsAddingCompleted;

    /// <summary>
    /// Gets whether adding has been completed via <see cref="CompleteAdding"/>.
    /// </summary>
    public abstract bool IsAddingCompleted { get; protected set; }

    /// <summary>
    /// Gets the current number of items in the queue.
    /// </summary>
    public abstract int Count { get; }

    /// <summary>
    /// Appends an item to the queue.
    /// </summary>
    /// <param name="item">The item to append.</param>
    public abstract void Append(T item);

    /// <summary>
    /// Appends multiple items from a span.
    /// </summary>
    /// <param name="items">The items to append.</param>
    public abstract void Append(ReadOnlySpan<T> items);

    /// <summary>
    /// Appends multiple items to the queue from an enumerable.
    /// Optimized for arrays and lists (uses span internally).
    /// </summary>
    /// <param name="items">The items to append to the queue.</param>
    public void Append(IEnumerable<T> items)
    {
        switch (items)
        {
            case T[] array:
                Append(array.AsSpan());
                break;
            case List<T> list:
                Append(CollectionsMarshal.AsSpan(list));
                break;
            default:
                foreach (var item in items)
                    Append(item);
                break;
        }
    }

    /// <summary>
    /// Attempts to remove and return an item from the queue.
    /// </summary>
    /// <param name="item">When this method returns, contains the removed item if successful; otherwise, the default value.</param>
    /// <returns>True if an item was successfully removed; otherwise, false.</returns>
    public abstract bool TryTake([NotNullWhen(true)] out T? item);

    /// <summary>
    /// Takes up to the specified number of items from the queue.
    /// </summary>
    /// <param name="count">The maximum number of items to take.</param>
    /// <returns>A read-only collection containing the taken items (may contain fewer than requested if not enough items available).</returns>
    public IReadOnlyCollection<T> Take(int count)
    {
        var result = new List<T>(count);
        while (result.Count < count && TryTake(out var item))
            result.Add(item);
        return result;
    }

    /// <summary>
    /// Takes up to the specified number of items from the queue.
    /// </summary>
    /// <param name="count">The maximum number of items to take.</param>
    /// <param name="items">The read-only collection of taken items.</param>
    /// <returns>True if at least one item was taken; otherwise, false.</returns>
    public bool Take(int count, out IReadOnlyCollection<T> items)
    {
        items = Take(count);
        return items.Count > 0;
    }

    /// <summary>
    /// Takes items directly into caller's buffer. Zero-allocation batch operation.
    /// </summary>
    /// <param name="buffer">The buffer to receive items.</param>
    /// <returns>The actual number of items taken (may be less than buffer length if queue has fewer items).</returns>
    public int TakeTo(Span<T> buffer)
    {
        var count = 0;
        while (count < buffer.Length && TryTake(out var item))
        {
            buffer[count++] = item;
        }
        return count;
    }

    /// <summary>
    /// Signals that no more items will be added to the queue.
    /// </summary>
    public abstract void CompleteAdding();

    /// <summary>
    /// Removes all items from the queue.
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// Peeks at the next item in the queue without removing it.
    /// Waits asynchronously until an item is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the wait operation.</param>
    /// <returns>The peeked item, or default if the channel is completed.</returns>
    public abstract Task<T?> PeekAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified item from the queue if it is still at the head.
    /// </summary>
    /// <param name="item">The item to mark as read (must match the current head).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that completes when the item is removed (if it was still the head).</returns>
    public abstract Task MarkItemAsReadAsync(T item, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the specified item is still at the head of the queue.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item is still at the head of the queue; otherwise, false.</returns>
    public abstract bool IsItemStillHead(T item);

    /// <summary>
    /// Asynchronously waits for work to become available in the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the wait operation.</param>
    /// <returns>True if work is available; false if adding has completed and the queue is empty.</returns>
    public abstract Task<bool> WaitForWorkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the resources used by the queue.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// Disposes the queue by completing the underlying channel(s).
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns an async enumerator that iterates through the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the enumeration.</param>
    /// <returns>An async enumerator for the queue.</returns>
    public abstract IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
}
