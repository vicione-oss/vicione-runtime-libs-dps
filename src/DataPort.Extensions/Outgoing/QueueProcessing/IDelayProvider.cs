using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;

/// <summary>
/// Provides an abstraction for delays to enable testing without actual time delays.
/// </summary>
public interface IDelayProvider
{
    /// <summary>
    /// Delays for the specified duration.
    /// </summary>
    /// <param name="delay">The time span to delay.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that completes when the delay is finished.</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation that uses Task.Delay for production use.
/// </summary>
public sealed class DefaultDelayProvider : IDelayProvider
{
    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        => Task.Delay(delay, cancellationToken);
}

/// <summary>
/// Test implementation that completes immediately without actual delays.
/// Useful for fast test execution while still validating retry logic.
/// </summary>
public sealed class InstantDelayProvider : IDelayProvider
{
    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        // Verify cancellation token is respected
        cancellationToken.ThrowIfCancellationRequested();
        // Return immediately without delay
        return Task.CompletedTask;
    }
}
