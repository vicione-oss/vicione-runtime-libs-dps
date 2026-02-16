using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.Utilities;

/// <summary>
/// Progressive retry logging helper (identical logic in both implementations).
/// </summary>
public static class RetryLogger
{
    /// <summary>
    /// Logs retry attempts with progressive verbosity:
    /// - Debug level: Attempts 1-4
    /// - Warning level: Attempt 5, then every 10th attempt (15, 20, 30, ...)
    /// </summary>
    /// <param name="retryCount">Current retry attempt number</param>
    /// <param name="delay">Backoff delay duration</param>
    /// <param name="logger">Logger instance</param>
    public static void LogRetryAttempt(long retryCount, TimeSpan delay, ILogger logger)
    {
        var delayMs = delay.TotalMilliseconds;

        switch (retryCount)
        {
            // Extended retry (5+): Log at 5, then every 10th
            case >= 5 when retryCount == 5 || retryCount % 10 == 0:
                logger.ExtendedRetryInProgress(retryCount, delayMs);
                break;

            // Initial retries (1-4): Debug level
            case < 5:
                logger.RetryingWriteOperation(retryCount, delayMs);
                break;
        }
    }
}
