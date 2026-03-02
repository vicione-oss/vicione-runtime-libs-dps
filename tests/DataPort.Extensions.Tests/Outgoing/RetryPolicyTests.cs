using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

public class RetryPolicyTests
{
    [Fact]
    public void BoundedRetryPolicy_WithMaxRetries5_ShouldRetryUpTo5Times()
    {
        // Arrange
        var policy = new BoundedRetryPolicy(maxRetryAttempts: 5);

        // Assert
        policy.ShouldRetry(1).Should().BeTrue();
        policy.ShouldRetry(4).Should().BeTrue();
        policy.ShouldRetry(5).Should().BeFalse("retry count equals max retries");
        policy.ShouldRetry(6).Should().BeFalse("retry count exceeds max retries");
    }

    [Fact]
    public void BoundedRetryPolicy_WithNegativeMaxRetries_ShouldRetryIndefinitely()
    {
        // Arrange
        var policy = new BoundedRetryPolicy(maxRetryAttempts: -1);

        // Assert
        policy.ShouldRetry(1).Should().BeTrue();
        policy.ShouldRetry(100).Should().BeTrue();
        policy.ShouldRetry(10000).Should().BeTrue();
        policy.ShouldRetry(long.MaxValue - 1).Should().BeTrue();
    }

    [Fact]
    public void BoundedRetryPolicy_WithZeroMaxRetries_ShouldNeverRetry()
    {
        // Arrange
        var policy = new BoundedRetryPolicy(maxRetryAttempts: 0);

        // Assert
        policy.ShouldRetry(0).Should().BeFalse();
        policy.ShouldRetry(1).Should().BeFalse();
    }

    [Fact]
    public void BoundedRetryPolicy_OnMaxRetriesExceeded_ShouldLogError()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var policy = new BoundedRetryPolicy(maxRetryAttempts: 3, logger);
        var exception = new InvalidOperationException("Test failure");

        // Act
        policy.OnMaxRetriesExceeded(3, exception);

        // Assert - LoggerMessage generates calls with specific EventId (202 for Error)
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == 202),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void BoundedRetryPolicy_OnMaxRetriesExceeded_WithoutLogger_ShouldNotThrow()
    {
        // Arrange
        var policy = new BoundedRetryPolicy(maxRetryAttempts: 3, logger: null);
        var exception = new InvalidOperationException("Test failure");

        // Act
        var act = () => policy.OnMaxRetriesExceeded(3, exception);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InfiniteRetryPolicy_ShouldAlwaysRetry()
    {
        // Arrange
        var policy = new InfiniteRetryPolicy();

        // Assert
        policy.ShouldRetry(1).Should().BeTrue();
        policy.ShouldRetry(100).Should().BeTrue();
        policy.ShouldRetry(10000).Should().BeTrue();
        policy.ShouldRetry(long.MaxValue).Should().BeTrue();
    }

    [Fact]
    public void InfiniteRetryPolicy_OnMaxRetriesExceeded_ShouldNotThrow()
    {
        // Arrange
        var policy = new InfiniteRetryPolicy();
        var exception = new InvalidOperationException("Test failure");

        // Act
        var act = () => policy.OnMaxRetriesExceeded(999, exception);

        // Assert (should never be called, but shouldn't throw if it is)
        act.Should().NotThrow();
    }
}
