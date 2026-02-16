using System;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

/// <summary>
/// Retry policy that never gives up.
/// Used by Symbolic implementation (AGLink expected to succeed eventually).
/// </summary>
public sealed class InfiniteRetryPolicy : IRetryPolicy
{
    /// <inheritdoc />
    public bool ShouldRetry(long retryCount) => true;

    /// <inheritdoc />
    public void OnMaxRetriesExceeded(long retryCount, Exception exception)
    {
        // Never called for infinite policy
    }
}
