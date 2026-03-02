using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

/// <summary>
/// Retry policy with configurable maximum retry attempts.
/// </summary>
public sealed class BoundedRetryPolicy(long maxRetryAttempts, ILogger? logger = null) : IRetryPolicy
{
    private readonly long _maxRetries = maxRetryAttempts < 0 ? long.MaxValue : maxRetryAttempts;

    /// <inheritdoc />
    public bool ShouldRetry(long retryCount) => retryCount < _maxRetries;

    /// <inheritdoc />
    public void OnMaxRetriesExceeded(long retryCount, Exception exception)
    {
        logger?.MaxRetriesExceeded(_maxRetries, retryCount, exception);
    }
}
