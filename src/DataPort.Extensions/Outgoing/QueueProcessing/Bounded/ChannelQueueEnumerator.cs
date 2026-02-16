using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Bounded;

/// <summary>
/// Async enumerator for <see cref="ChannelQueue{T}"/> that reads items from the underlying channel.
/// </summary>
/// <typeparam name="T">The type of items in the queue.</typeparam>
internal sealed class ChannelQueueEnumerator<T> : IAsyncEnumerator<T> where T : notnull
{
    private readonly ChannelReader<T> _reader;
    private readonly CancellationToken _cancellationToken;
    private T? _current;

    internal ChannelQueueEnumerator(ChannelReader<T> reader, CancellationToken cancellationToken)
    {
        _reader = reader;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the current item in the enumeration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before MoveNextAsync or after it returns false.</exception>
    public T Current => _current ?? throw new InvalidOperationException("No current item available.");

    /// <summary>
    /// Advances the enumerator to the next item in the queue.
    /// </summary>
    /// <returns>True if an item was read; false if the channel is completed.</returns>
    public async ValueTask<bool> MoveNextAsync()
    {
        if (await _reader.WaitToReadAsync(_cancellationToken))
        {
            if (_reader.TryRead(out var item))
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
