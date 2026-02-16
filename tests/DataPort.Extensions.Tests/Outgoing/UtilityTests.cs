using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.Outgoing.Utilities;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

public class UtilityTests
{
    // ChannelLookupBuilder Tests
    [Fact]
    public void ChannelLookupBuilder_WithMultipleDataPoints_BuildsCorrectLookup()
    {
        // Arrange
        var dataPoints = new[]
        {
            new TestDataPoint { Id = "DP1", Channels = ["CH1", "CH2"] },
            new TestDataPoint { Id = "DP2", Channels = ["CH3"] },
            new TestDataPoint { Id = "DP3", Channels = ["CH4", "CH5", "CH6"] }
        };

        // Act
        var lookup = ChannelLookupBuilder.BuildLookup(
            dataPoints,
            dp => dp.Channels);

        // Assert
        lookup.Should().HaveCount(6);
        lookup["CH1"].Id.Should().Be("DP1");
        lookup["CH2"].Id.Should().Be("DP1");
        lookup["CH3"].Id.Should().Be("DP2");
        lookup["CH4"].Id.Should().Be("DP3");
        lookup["CH5"].Id.Should().Be("DP3");
        lookup["CH6"].Id.Should().Be("DP3");
    }

    [Fact]
    public void ChannelLookupBuilder_WithDuplicateChannels_UsesLastDataPoint()
    {
        // Arrange
        var dataPoints = new[]
        {
            new TestDataPoint { Id = "DP1", Channels = ["CH1"] },
            new TestDataPoint { Id = "DP2", Channels = ["CH1"] } // Duplicate
        };

        // Act
        var lookup = ChannelLookupBuilder.BuildLookup(
            dataPoints,
            dp => dp.Channels);

        // Assert
        lookup.Should().HaveCount(1);
        lookup["CH1"].Id.Should().Be("DP2", "last data point overwrites");
    }

    [Fact]
    public void ChannelLookupBuilder_WithEmptyDataPoints_ReturnsEmptyDictionary()
    {
        // Arrange
        var dataPoints = Array.Empty<TestDataPoint>();

        // Act
        var lookup = ChannelLookupBuilder.BuildLookup(
            dataPoints,
            dp => dp.Channels);

        // Assert
        lookup.Should().BeEmpty();
    }

    [Fact]
    public void ChannelLookupBuilder_WithEmptyChannels_ReturnsEmptyDictionary()
    {
        // Arrange
        var dataPoints = new[]
        {
            new TestDataPoint { Id = "DP1", Channels = [] }
        };

        // Act
        var lookup = ChannelLookupBuilder.BuildLookup(
            dataPoints,
            dp => dp.Channels);

        // Assert
        lookup.Should().BeEmpty();
    }

    // RetryLogger Tests
    [Fact]
    public void RetryLogger_Attempt1To4_LogsAtDebugLevel()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);
        var delay = TimeSpan.FromMilliseconds(200);

        // Act
        RetryLogger.LogRetryAttempt(1, delay, logger);
        RetryLogger.LogRetryAttempt(2, delay, logger);
        RetryLogger.LogRetryAttempt(3, delay, logger);
        RetryLogger.LogRetryAttempt(4, delay, logger);

        // Assert - LoggerMessage generates calls with specific EventId (200 for Debug)
        logger.Received(4).Log(
            LogLevel.Debug,
            Arg.Is<EventId>(e => e.Id == 200),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void RetryLogger_Attempt5_LogsAtWarningLevel()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var delay = TimeSpan.FromMilliseconds(500);

        // Act
        RetryLogger.LogRetryAttempt(5, delay, logger);

        // Assert - LoggerMessage generates calls with specific EventId (201 for Warning)
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 201),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void RetryLogger_Attempt6To9_DoesNotLog()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var delay = TimeSpan.FromMilliseconds(500);

        // Act
        RetryLogger.LogRetryAttempt(6, delay, logger);
        RetryLogger.LogRetryAttempt(7, delay, logger);
        RetryLogger.LogRetryAttempt(8, delay, logger);
        RetryLogger.LogRetryAttempt(9, delay, logger);

        // Assert
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void RetryLogger_Every10thAttemptAfter5_LogsAtWarningLevel()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var delay = TimeSpan.FromMilliseconds(1000);

        // Act
        RetryLogger.LogRetryAttempt(10, delay, logger);
        RetryLogger.LogRetryAttempt(20, delay, logger);
        RetryLogger.LogRetryAttempt(30, delay, logger);

        // Assert - LoggerMessage generates calls with specific EventId (201 for Warning)
        logger.Received(3).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 201),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void RetryLogger_Attempt15_DoesNotLog()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var delay = TimeSpan.FromMilliseconds(1000);

        // Act
        RetryLogger.LogRetryAttempt(15, delay, logger);

        // Assert - 15 % 10 = 5, so it should NOT log based on code logic
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // Helper class for tests
    private class TestDataPoint
    {
        public required string Id { get; init; }
        public required string[] Channels { get; init; }
    }
}
