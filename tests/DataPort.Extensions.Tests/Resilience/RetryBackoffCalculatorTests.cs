using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Suite.DataPort.Extensions.Resilience;

namespace ViciOne.Suite.DataPort.Extension.Tests.Resilience;

public class RetryBackoffCalculatorTests
{
    [Theory]
    [InlineData(0, 100, 5000, 100)] // First retry: 2^0 * 100 = 100
    [InlineData(1, 100, 5000, 200)] // Second retry: 2^1 * 100 = 200
    [InlineData(2, 100, 5000, 400)] // Third retry: 2^2 * 100 = 400
    [InlineData(3, 100, 5000, 800)] // Fourth retry: 2^3 * 100 = 800
    [InlineData(4, 100, 5000, 1600)] // Fifth retry: 2^4 * 100 = 1600
    [InlineData(5, 100, 5000, 3200)] // Sixth retry: 2^5 * 100 = 3200
    public void CalculateDelay_WithExponentialBackoff_ReturnsExponentialDelay(
        int retryCount,
        int initialDelayMs,
        int maxDelayMs,
        int expectedBaseDelayMs)
    {
        // Act
        var result = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: true);

        // Assert
        // Allow for jitter (up to 10% of exponential delay)
        var maxJitter = (int)(expectedBaseDelayMs * 0.1);
        var maxDelay = expectedBaseDelayMs + maxJitter;

        result.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(expectedBaseDelayMs);
        result.TotalMilliseconds.Should().BeLessThanOrEqualTo(maxDelay);
    }

    [Theory]
    [InlineData(10, 100, 5000)] // 2^10 * 100 = 102400, should cap at 5000
    [InlineData(15, 100, 5000)] // 2^15 * 100 = 3276800, should cap at 5000
    [InlineData(20, 100, 10000)] // 2^20 * 100 = very large, should cap at 10000
    public void CalculateDelay_WithExponentialBackoff_CapsAtMaxDelay(
        int retryCount,
        int initialDelayMs,
        int maxDelayMs)
    {
        // Act
        var result = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: true);

        // Assert
        result.TotalMilliseconds.Should().BeLessThanOrEqualTo(maxDelayMs);
    }

    [Theory]
    [InlineData(1, 100, 5000, 100)] // First retry: 1 * 100 = 100
    [InlineData(2, 100, 5000, 200)] // Second retry: 2 * 100 = 200
    [InlineData(3, 100, 5000, 300)] // Third retry: 3 * 100 = 300
    [InlineData(5, 100, 5000, 500)] // Fifth retry: 5 * 100 = 500
    [InlineData(10, 100, 5000, 1000)] // Tenth retry: 10 * 100 = 1000
    public void CalculateDelay_WithLinearBackoff_ReturnsLinearDelay(
        int retryCount,
        int initialDelayMs,
        int maxDelayMs,
        int expectedDelayMs)
    {
        // Act
        var result = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: false);

        // Assert
        result.TotalMilliseconds.Should().Be(expectedDelayMs);
    }

    [Theory]
    [InlineData(50, 100, 4000, 4000)] // 50 * 100 = 5000, should cap at 4000
    [InlineData(100, 100, 5000, 5000)] // 100 * 100 = 10000, should cap at 5000
    [InlineData(1000, 50, 10000, 10000)] // 1000 * 50 = 50000, should cap at 10000
    public void CalculateDelay_WithLinearBackoff_CapsAtMaxDelay(
        int retryCount,
        int initialDelayMs,
        int maxDelayMs,
        int expectedDelayMs)
    {
        // Act
        var result = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: false);

        // Assert
        result.TotalMilliseconds.Should().Be(expectedDelayMs);
    }

    [Fact]
    public void CalculateDelay_WithZeroRetryCount_ReturnsInitialDelay()
    {
        // Arrange
        var retryCount = 0;
        var initialDelayMs = 100;
        var maxDelayMs = 5000;

        // Act
        var result = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: false);

        // Assert
        result.TotalMilliseconds.Should().Be(0);
    }

    [Fact]
    public void CalculateDelay_WithZeroRetryCount_ExponentialBackoff_ReturnsInitialDelayWithJitter()
    {
        // Arrange
        var retryCount = 0;
        var initialDelayMs = 100;
        var maxDelayMs = 5000;

        // Act
        var result = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: true);

        // Assert
        // 2^0 * 100 = 100, with up to 10 jitter
        result.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(100);
        result.TotalMilliseconds.Should().BeLessThanOrEqualTo(110);
    }

    [Theory]
    [InlineData(1, 50, 1000)]
    [InlineData(2, 100, 2000)]
    [InlineData(3, 200, 5000)]
    public void CalculateDelay_WithDifferentInitialDelays_ScalesCorrectly(
        int retryCount,
        int initialDelayMs,
        int maxDelayMs)
    {
        // Act - Linear
        var linearResult =
            RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: false);

        // Assert - Linear
        var expectedLinearDelay = Math.Min(retryCount * initialDelayMs, maxDelayMs);
        linearResult.TotalMilliseconds.Should().Be(expectedLinearDelay);

        // Act - Exponential
        var exponentialResult =
            RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: true);

        // Assert - Exponential
        var expectedExponentialBase = Math.Pow(2, retryCount) * initialDelayMs;
        var minExpected = Math.Min(expectedExponentialBase, maxDelayMs);
        var maxExpected = Math.Min(expectedExponentialBase * 1.1, maxDelayMs);

        exponentialResult.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(minExpected);
        exponentialResult.TotalMilliseconds.Should().BeLessThanOrEqualTo(maxExpected);
    }

    [Fact]
    public void CalculateDelay_ExponentialBackoff_IncludesJitter()
    {
        // Arrange
        var retryCount = 3;
        var initialDelayMs = 100;
        var maxDelayMs = 10000;
        var results = new List<double>();

        // Act - Run multiple times to verify jitter varies
        for (var i = 0; i < 20; i++)
        {
            var result =
                RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: true);
            results.Add(result.TotalMilliseconds);
        }

        // Assert - There should be variation in the results due to jitter
        var uniqueResults = results.Distinct().Count();
        uniqueResults.Should().BeGreaterThan(1, "jitter should cause variation in delay times");

        // All results should be within the expected range
        var expectedBase = Math.Pow(2, retryCount) * initialDelayMs; // 800
        foreach (var result in results)
        {
            result.Should().BeGreaterThanOrEqualTo(expectedBase);
            result.Should().BeLessThanOrEqualTo(expectedBase * 1.1); // 10% jitter
        }
    }

    [Fact]
    public void CalculateDelay_LinearBackoff_NoJitter()
    {
        // Arrange
        var retryCount = 3;
        var initialDelayMs = 100;
        var maxDelayMs = 10000;
        var results = new List<double>();

        // Act - Run multiple times
        for (var i = 0; i < 10; i++)
        {
            var result =
                RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: false);
            results.Add(result.TotalMilliseconds);
        }

        // Assert - All results should be identical (no jitter in linear mode)
        var uniqueResults = results.Distinct().Count();
        uniqueResults.Should().Be(1, "linear backoff should not have jitter");

        var expected = retryCount * initialDelayMs;
        results.Should().HaveElementAt(0, expected);
    }

    [Theory]
    [InlineData(1, 1, 100)]
    [InlineData(5, 10, 500)]
    [InlineData(10, 50, 1000)]
    public void CalculateDelay_ReturnsTimeSpan(
        int retryCount,
        int initialDelayMs,
        int maxDelayMs)
    {
        // Act
        var result = RetryBackoffCalculator.CalculateDelay(retryCount, initialDelayMs, maxDelayMs, exponential: false);

        // Assert
        result.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
