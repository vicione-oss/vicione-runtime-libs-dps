using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.Outgoing;
using ViciOne.Suite.DataPort.Extensions.Queue;
using ViciOne.Suite.DataPort.Extensions.Resilience;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Smoke tests to verify TestOutgoingDataPort infrastructure works correctly.
/// </summary>
public class TestOutgoingDataPortSmokeTests
{
    [Fact]
    public async Task Constructor_ShouldCreateValidInstance()
    {
        // Arrange
        var communication = new TestCommunication
        {
            IpAddress = "192.168.1.1",
            Port = 102,
            DataPoints = [new() { ChannelName = "Channel1", Description = "Test channel" }],
            MaxPendingMessages = 10,
            OverflowStrategy = QueueStrategy.DropOldest
        };

        var writeClient = Substitute.For<IWriteClient<TestDataPointValue>>();
        var logger = Substitute.For<ILogger<TestOutgoingDataPort>>();
        var delayProvider = new InstantDelayProvider();

        // Act
        await using var sut = new TestOutgoingDataPort(
            communication,
            writeClient,
            logger,
            delayProvider);

        // Assert
        sut.Should().NotBeNull();
        sut.TestClient.Should().NotBeNull();
        sut.TestClient.IsConnected.Should().BeFalse();
        sut.TestClient.ConnectionCount.Should().Be(0);
    }

    [Fact]
    public async Task ConnectAsync_ShouldAcquireConnection()
    {
        // Arrange
        var communication = new TestCommunication
        {
            IpAddress = "192.168.1.1",
            Port = 102,
            DataPoints = [new() { ChannelName = "Channel1" }]
        };

        var writeClient = Substitute.For<IWriteClient<TestDataPointValue>>();
        var logger = Substitute.For<ILogger<TestOutgoingDataPort>>();
        var delayProvider = new InstantDelayProvider();

        await using var sut = new TestOutgoingDataPort(
            communication,
            writeClient,
            logger,
            delayProvider);

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        // Assert
        sut.TestClient.IsConnected.Should().BeTrue();
        sut.TestClient.ConnectionCount.Should().Be(1);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldReleaseConnection()
    {
        // Arrange
        var communication = new TestCommunication
        {
            IpAddress = "192.168.1.1",
            Port = 102,
            DataPoints = [new() { ChannelName = "Channel1" }]
        };

        var writeClient = Substitute.For<IWriteClient<TestDataPointValue>>();
        var logger = Substitute.For<ILogger<TestOutgoingDataPort>>();
        var delayProvider = new InstantDelayProvider();

        await using var sut = new TestOutgoingDataPort(
            communication,
            writeClient,
            logger,
            delayProvider);

        await sut.ConnectAsync(CancellationToken.None);

        // Act
        await sut.DisconnectAsync(CancellationToken.None);

        // Assert
        sut.TestClient.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var communication = new TestCommunication
        {
            IpAddress = "192.168.1.1",
            Port = 102,
            DataPoints = [new() { ChannelName = "Channel1" }]
        };

        var writeClient = Substitute.For<IWriteClient<TestDataPointValue>>();
        var logger = Substitute.For<ILogger<TestOutgoingDataPort>>();
        var delayProvider = new InstantDelayProvider();

        var sut = new TestOutgoingDataPort(
            communication,
            writeClient,
            logger,
            delayProvider);

        await sut.ConnectAsync(CancellationToken.None);

        // Act
        await sut.DisposeAsync();

        // Assert - should not throw
        sut.TestClient.IsConnected.Should().BeFalse();
    }
}
