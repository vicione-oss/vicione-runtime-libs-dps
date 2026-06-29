using System;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Model.DataPoints;

public class PollFrequencyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(3600)]
    public void FromSeconds_ReturnsFrequencyWithMatchingValue(int seconds)
    {
        // Arrange
        var expected = TimeSpan.FromSeconds(seconds);

        // Act
        var frequency = PollFrequency.FromSeconds(seconds);

        // Assert
        frequency.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(500)]
    public void FromMilliseconds_ReturnsFrequencyWithMatchingValue(int milliseconds)
    {
        // Arrange
        var expected = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        var frequency = PollFrequency.FromMilliseconds(milliseconds);

        // Assert
        frequency.Value.Should().Be(expected);
    }

    [Fact]
    public void FromSeconds_IsEquivalentToConstructingFromTimeSpan()
    {
        // Arrange
        var expected = new PollFrequency(TimeSpan.FromSeconds(5));

        // Act
        var frequency = PollFrequency.FromSeconds(5);

        // Assert
        frequency.Should().Be(expected);
    }

    [Fact]
    public void FromMilliseconds_IsEquivalentToConstructingFromTimeSpan()
    {
        // Arrange
        var expected = new PollFrequency(TimeSpan.FromMilliseconds(250));

        // Act
        var frequency = PollFrequency.FromMilliseconds(250);

        // Assert
        frequency.Should().Be(expected);
    }
}
