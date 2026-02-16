using System;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Exceptions;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing;

/// <summary>
/// Common logging methods for OutgoingDataPortBase operations.
/// Uses LoggerMessage source generation for high-performance logging.
/// </summary>
internal static partial class OutgoingDataPortBaseLogs
{
    /// <summary>
    /// Logs when a channel is not found in the data point mapping.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="channel">Channel name that was not found</param>
    [LoggerMessage(203, LogLevel.Warning, "Channel '{Channel}' not found in data point mapping")]
    internal static partial void ChannelNotFound(this ILogger logger, string channel);

    /// <summary>
    /// Logs when validation fails for a data point value.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="channel">Channel name</param>
    /// <param name="errorMessage">Validation error message</param>
    [LoggerMessage(204, LogLevel.Error, "Validation failed for channel '{Channel}': {ErrorMessage}")]
    internal static partial void ValidationFailed(this ILogger logger, string channel, string? errorMessage);

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

    /// <summary>
    /// Logs when multiple channels are not found (batch reporting).
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="count">Number of unmapped channels</param>
    /// <param name="unmappedChannels">Comma-separated list of unmapped channels</param>
    [LoggerMessage(208, LogLevel.Warning, "{Count} unmapped channels: {UnmappedChannels}")]
    internal static partial void UnmappedChannelsBatch(this ILogger logger, int count, string unmappedChannels);

    /// <summary>
    /// Logs batch validation failure summary.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="failureCount">Number of values that failed validation</param>
    /// <param name="totalCount">Total number of values being validated</param>
    [LoggerMessage(209, LogLevel.Error, "Batch validation failed: {FailureCount} of {TotalCount} values invalid")]
    internal static partial void BatchValidationFailed(this ILogger logger, int failureCount, int totalCount);
}
