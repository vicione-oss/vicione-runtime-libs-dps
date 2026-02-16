using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Prependable;

/// <summary>
/// Async enumerator for <see cref="PrependableChannelQueue{T}"/> that reads items
/// from priority channel first, then normal channel.
/// </summary>
/// <typeparam name="T">The type of items in the queue.</typeparam>
internal sealed class PrependableChannelQueueEnumerator<T> : IAsyncEnumerator<T> where T : notnull
{
    private readonly PrependableChannelQueue<T> _queue;
    private readonly CancellationToken _cancellationToken;
    private T? _current;

    internal PrependableChannelQueueEnumerator(PrependableChannelQueue<T> queue, CancellationToken cancellationToken)
    {
        _queue = queue;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the current item in the enumeration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before MoveNextAsync or after it returns false.</exception>
    public T Current => _current ?? throw new InvalidOperationException("No current item available.");

    /// <summary>
    /// Advances the enumerator to the next item in the queue.
    /// Priority items are returned before normal items.
    /// </summary>
    /// <returns>True if an item was read; false if both channels are completed.</returns>
    public async ValueTask<bool> MoveNextAsync()
    {
        if (await _queue.WaitForWorkAsync(_cancellationToken))
        {
            if (_queue.TryTake(out var item))
            {
                _current = item;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Disposes the enumerator.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
