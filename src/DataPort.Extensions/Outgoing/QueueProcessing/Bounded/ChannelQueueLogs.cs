using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Bounded;

/// <summary>
/// Centralized logging for queue operations using source-generated logging.
/// Keeps the main queue classes non-partial.
/// </summary>
internal static partial class ChannelQueueLogs
{
    [LoggerMessage(LogLevel.Warning, "Failed to write item to channel {Name}")]
    public static partial void LogFailedToWriteItemToChannel(this ILogger logger, string name);

    [LoggerMessage(LogLevel.Debug, "Queue {Name} dropped newest item (DropNewest strategy, capacity: {Capacity})")]
    public static partial void LogQueueDroppedNewestItem(this ILogger logger, string name, int capacity);

    [LoggerMessage(LogLevel.Debug, "Queue {Name} dropped oldest item (DropOldest strategy, capacity: {Capacity})")]
    public static partial void LogQueueDroppedOldestItem(this ILogger logger, string name, int capacity);
}
