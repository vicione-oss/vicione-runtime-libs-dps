using System;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Exceptions;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;

/// <summary>
/// Logging methods for OutgoingQueueProcessor operations.
/// Uses LoggerMessage source generation for high-performance logging.
/// </summary>
internal static partial class QueueProcessorLogs
{
    /// <summary>
    /// Logs when retry is abandoned due to queue backpressure.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="retryCount">Number of retries attempted before abandoning</param>
    [LoggerMessage(205, LogLevel.Warning, "Abandoning retry after {RetryCount} attempts due to queue backpressure (item no longer at head)")]
    internal static partial void AbandoningRetryDueToBackpressure(this ILogger logger, long retryCount);

    /// <summary>
    /// Logs when maximum retries have been exceeded.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="attemptCount">Number of attempts made</param>
    /// <param name="exception">Exception that caused the failure</param>
    [LoggerMessage(206, LogLevel.Error, "Maximum retry attempts exceeded after {AttemptCount} retries")]
    internal static partial void MaxRetriesExceededCore(this ILogger logger, long attemptCount, Exception exception);

    /// <summary>
    /// Logs when maximum retries have been exceeded (overload accepting MaxRetriesExceededException).
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="exception">Exception containing retry information</param>
    public static void MaxRetriesExceeded(this ILogger logger, MaxRetriesExceededException exception)
    {
        logger.MaxRetriesExceededCore(exception.AttemptCount, exception);
    }

    /// <summary>
    /// Logs when an item is dropped from the queue due to backpressure.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    [LoggerMessage(207, LogLevel.Warning, "Item dropped from queue head due to backpressure while retrying write operation")]
    internal static partial void ItemDroppedDueToBackpressure(this ILogger logger);
}
