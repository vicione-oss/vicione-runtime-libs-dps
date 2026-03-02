using System;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

/// <summary>
/// Defines retry behavior for failed write operations.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Determines if another retry attempt should be made.
    /// </summary>
    /// <param name="retryCount">Number of retries attempted so far</param>
    /// <returns>True if should retry, false if retries exhausted</returns>
    bool ShouldRetry(long retryCount);

    /// <summary>
    /// Called when max retries exceeded. Implementation can log or throw exception.
    /// </summary>
    /// <param name="retryCount">Final retry count</param>
    /// <param name="exception">Last exception encountered</param>
    void OnMaxRetriesExceeded(long retryCount, Exception exception);
}
