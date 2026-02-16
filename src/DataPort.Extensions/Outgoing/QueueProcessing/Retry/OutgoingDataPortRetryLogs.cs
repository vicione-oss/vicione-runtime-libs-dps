using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

/// <summary>
/// Common logging methods for outgoing data port retry operations.
/// Uses LoggerMessage source generation for high-performance logging.
/// </summary>
internal static partial class OutgoingDataPortRetryLogs
{
    /// <summary>
    /// Logs retry attempts at Debug level for attempts 1-4.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="retryCount">Current retry attempt number</param>
    /// <param name="delayMs">Backoff delay in milliseconds</param>
    [LoggerMessage(200, LogLevel.Debug, "Retrying write operation. Attempt {RetryCount} after {DelayMs}ms delay")]
    internal static partial void RetryingWriteOperation(this ILogger logger, long retryCount, double delayMs);

    /// <summary>
    /// Logs extended retry attempts at Warning level for attempt 5 and every 10th attempt.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="retryCount">Current retry attempt number</param>
    /// <param name="delayMs">Backoff delay in milliseconds</param>
    [LoggerMessage(201, LogLevel.Warning, "Extended retry in progress. Attempt {RetryCount} after {DelayMs}ms delay")]
    internal static partial void ExtendedRetryInProgress(this ILogger logger, long retryCount, double delayMs);

    /// <summary>
    /// Logs when maximum retry attempts have been exceeded.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="maxRetries">Maximum number of retry attempts allowed</param>
    /// <param name="retryCount">Number of retries that were attempted</param>
    /// <param name="exception">Exception that caused the failure</param>
    [LoggerMessage(202, LogLevel.Error, "Maximum retry attempts ({MaxRetries}) exceeded after {RetryCount} retries")]
    internal static partial void MaxRetriesExceeded(this ILogger logger, long maxRetries, long retryCount, Exception exception);
}
