using System;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

/// <summary>
/// Calculates retry backoff delays with jitter for resilient external system communication.
/// </summary>
/// <remarks>
/// <para>
/// This calculator implements exponential and linear backoff strategies with
/// randomized jitter to prevent the "thundering herd" problem - when multiple
/// clients retry simultaneously after a failure, overwhelming the target system.
/// </para>
/// <para>
/// <strong>Rationale for Random.Shared usage:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>Not a cryptographic use case:</strong> Jitter is used purely for timing
/// variance in retry logic, not for security-sensitive operations like token generation,
/// encryption, or authentication. The predictability of <see cref="Random"/> has no
/// security implications in this context.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Performance:</strong> <see cref="Random.Shared"/> is thread-safe and
/// allocation-free, making it ideal for hot paths in retry logic where multiple
/// concurrent connections may be calculating backoff delays.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Thundering herd prevention:</strong> Adding 10% randomized jitter to
/// retry delays ensures that multiple clients that fail simultaneously will retry
/// at staggered intervals, reducing load spikes on the target system during recovery.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>CA5394 suppression justification:</strong> The analyzer warning "Do not use
/// insecure randomness" is designed to catch cryptographic misuse of <see cref="Random"/>.
/// Since this code uses randomness only for non-security-critical timing variance in
/// distributed systems resilience patterns, the warning is safely suppressed. For
/// cryptographic operations, use <see cref="System.Security.Cryptography.RandomNumberGenerator"/>.
/// </para>
/// </remarks>
public static class RetryBackoffCalculator
{
    /// <summary>
    /// Calculates the delay for a retry attempt using exponential or linear backoff.
    /// </summary>
    /// <param name="retryCount">The zero-based retry attempt number (0 = first retry).</param>
    /// <param name="initialDelayMs">The base delay in milliseconds before the first retry.</param>
    /// <param name="maxDelayMs">The maximum delay in milliseconds to cap exponential growth.</param>
    /// <param name="exponential">
    /// If <c>true</c>, uses exponential backoff (2^retryCount * initialDelay).
    /// If <c>false</c>, uses linear backoff (retryCount * initialDelay).
    /// </param>
    /// <returns>
    /// A <see cref="TimeSpan"/> representing the calculated delay with jitter applied (exponential mode only).
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Exponential backoff:</strong> Delays grow as 2^retryCount, with 10% randomized jitter
    /// added to prevent synchronized retries across multiple connections. Capped at <paramref name="maxDelayMs"/>.
    /// </para>
    /// <para>
    /// <strong>Linear backoff:</strong> Delays grow linearly (retryCount * initialDelay), capped at
    /// <paramref name="maxDelayMs"/>. No jitter is applied in linear mode.
    /// </para>
    /// </remarks>
    public static TimeSpan CalculateDelay(long retryCount,
        int initialDelayMs,
        int maxDelayMs,
        bool exponential = true)
    {
        if (exponential)
        {
            // Exponential backoff: 2^retryCount * initialDelay
            var exponentialDelay = Math.Pow(2, retryCount) * initialDelayMs;

            // Add 10% randomized jitter to prevent thundering herd problem.
            // When multiple connections fail simultaneously (e.g., network outage),
            // jitter ensures they don't all retry at exactly the same time, which would
            // create a load spike on the target system. Random.Shared is appropriate here because
            // this is NOT a cryptographic operation - we only need timing variance.
#pragma warning disable CA5394 // Do not use insecure randomness (see class remarks for justification)
            var jitter = Random.Shared.Next(0, (int)(exponentialDelay * 0.1)); // 10% jitter range
#pragma warning restore CA5394

            var totalDelay = Math.Min(exponentialDelay + jitter, maxDelayMs);
            return TimeSpan.FromMilliseconds(totalDelay);
        }

        // Linear backoff: retryCount * initialDelay (no jitter)
        var linearDelay = Math.Min(retryCount * initialDelayMs, maxDelayMs);
        return TimeSpan.FromMilliseconds(linearDelay);
    }
}
