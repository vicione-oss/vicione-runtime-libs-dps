using ViciOne.Suite.DataPort.Extensions.Queue;

namespace ViciOne.Suite.DataPort.Extensions.Model;

/// <summary>
/// Configuration settings for the internal message queue used by data ports.
/// </summary>
/// <param name="MaxPendingMessages">
/// The maximum number of messages that can be held in the queue before backpressure is applied.
/// When this limit is reached, the <paramref name="Strategy"/> determines which messages are discarded.
/// </param>
/// <param name="Strategy">
/// The strategy to use when the queue reaches capacity and a new message arrives.
/// See <see cref="QueueStrategy"/> for available options.
/// </param>
public sealed record QueueConfiguration(int MaxPendingMessages, QueueStrategy Strategy);
