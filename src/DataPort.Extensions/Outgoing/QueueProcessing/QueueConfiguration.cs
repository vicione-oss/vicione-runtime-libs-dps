namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;

/// <summary>Configuration for an outgoing data port queue.</summary>
/// <param name="Strategy">The overflow strategy to apply when the queue is full.</param>
/// <param name="QueueSize">The maximum number of items the queue can hold.</param>
public record QueueConfiguration(QueueStrategy Strategy, QueueSize QueueSize);

/// <summary>
/// Specifies the strategy for handling backpressure when a queue reaches its maximum capacity.
/// </summary>
public enum QueueStrategy
{
    /// <summary>
    /// When the queue is full, discard the oldest message to make room for the new one.
    /// This strategy prioritizes the most recent data, ensuring that the latest values are always delivered.
    /// Use this when data freshness is more important than completeness.
    /// </summary>
    DropOldest,

    /// <summary>
    /// When the queue is full, reject the new message and keep existing messages.
    /// This strategy prioritizes preserving the existing queue order and avoiding data loss for already-queued items.
    /// Use this when maintaining message order and not losing already-accepted data is critical.
    /// </summary>
    DropNewest
}

/// <summary>The maximum number of items an outgoing data port queue can hold.</summary>
/// <param name="Value">The capacity value.</param>
public readonly record struct QueueSize(int Value);
