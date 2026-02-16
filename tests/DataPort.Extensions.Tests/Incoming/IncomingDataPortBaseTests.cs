using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Incoming;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Incoming;

public class IncomingDataPortBaseTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConnectionManager<TestClient> _connectionManager;
    private readonly IReadClient<TestDataPointGroup, TestDataPointValue> _readClient;
    private readonly FakeTimeProvider _timeProvider;

    public IncomingDataPortBaseTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _connectionManager = Substitute.For<IConnectionManager<TestClient>>();
        _readClient = Substitute.For<IReadClient<TestDataPointGroup, TestDataPointValue>>();
        _timeProvider = new FakeTimeProvider();
    }

    [Fact]
    public async Task ConnectAsync_ShouldAcquireClientAndStartPolling()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(2);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        _readClient.ReadAsync(Arg.Any<TestDataPointGroup>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            timeProvider: _timeProvider);

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        // Small delay to allow polling jobs to start
        await Task.Delay(100);

        // Assert
        await _connectionManager.Received(1).AcquireConnectedAsync(Arg.Any<CancellationToken>());

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsync_WhenValidationFails_ShouldDisconnect()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(1);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        var validator = Substitute.For<IDataPointValidator<IDataPoint>>();
        validator
            .When(v => v.Validate(Arg.Any<TestClient>(), Arg.Any<List<IDataPoint>>(), Arg.Any<ILogger>()))
            .Do(_ => throw new ConnectionFailureException("Validation failed"));

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            validator,
            timeProvider: _timeProvider);

        // Act & Assert
        var act = async () => await sut.ConnectAsync(CancellationToken.None);

        await act.Should().ThrowExactlyAsync<ConnectionFailureException>()
            .WithMessage("*Validation failed*");

        // Verify DisconnectAsync was called (releases client)
        await _connectionManager.Received(1).ReleaseAsync(testClient, Arg.Any<CancellationToken>());

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldStopPollingAndReleaseClient()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(2);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        _readClient.ReadAsync(Arg.Any<TestDataPointGroup>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            timeProvider: _timeProvider);

        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100); // Allow polling to start

        // Act
        await sut.DisconnectAsync(CancellationToken.None);

        // Assert
        await _connectionManager.Received(1).ReleaseAsync(testClient, Arg.Any<CancellationToken>());

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task StartPollingAndPublishingDataPoints_ShouldCreateJobsForEachGroup()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(3);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        var readCallCount = 0;
        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        _readClient.ReadAsync(Arg.Any<TestDataPointGroup>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                readCallCount++;
                return [];
            });

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            timeProvider: _timeProvider);

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        // Advance time to trigger polling
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));
        await Task.Delay(100); // Allow jobs to execute

        // Assert
        // Each group should have been read at least once
        readCallCount.Should().BeGreaterThanOrEqualTo(3);

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task PublishDataPointValues_ShouldRaiseReceivedEvent()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(1);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        var testDataPointValues = new List<TestDataPointValue>
        {
            new() { TransferredChannels = ["Channel1", "Channel2"], Value = 42 }
        };

        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        _readClient.ReadAsync(Arg.Any<TestDataPointGroup>(), Arg.Any<CancellationToken>())
            .Returns(testDataPointValues);

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            timeProvider: _timeProvider);

        var receivedEventFired = false;
        IReadOnlyCollection<ExternalValue>? receivedValues = null;

        sut.Received += values =>
        {
            receivedEventFired = true;
            receivedValues = values;
        };

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        // Advance time to trigger polling
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));
        await Task.Delay(100); // Allow job to execute and event to fire

        // Assert
        receivedEventFired.Should().BeTrue();
        receivedValues.Should().NotBeNull();
        receivedValues!.Count.Should().Be(2); // 2 transferred channels

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task ToExternalValues_ShouldApplyTimestampToAllValues()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(1);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        var expectedTimestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        _timeProvider.SetUtcNow(expectedTimestamp);

        var testDataPointValues = new List<TestDataPointValue>
        {
            new() { TransferredChannels = ["Channel1"], Value = 123 },
            new() { TransferredChannels = ["Channel2"], Value = 456 }
        };

        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        _readClient.ReadAsync(Arg.Any<TestDataPointGroup>(), Arg.Any<CancellationToken>())
            .Returns(testDataPointValues);

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            timeProvider: _timeProvider);

        IReadOnlyCollection<ExternalValue>? receivedValues = null;
        sut.Received += values => receivedValues = values;

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));
        await Task.Delay(100);

        // Assert
        receivedValues.Should().NotBeNull();
        var firstTimestamp = receivedValues!.First().Timestamp;
        receivedValues.All(v => v.Timestamp == firstTimestamp).Should().BeTrue();

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupAllResources()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(2);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        _readClient.ReadAsync(Arg.Any<TestDataPointGroup>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            timeProvider: _timeProvider);

        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100);

        // Act
        await sut.DisposeAsync();

        // Assert
        await _connectionManager.Received(1).ReleaseAsync(testClient, Arg.Any<CancellationToken>());
        await _connectionManager.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsync_WithReconnection_ShouldResetCancellationToken()
    {
        // Arrange
        var deviceNode = new TestDeviceNode("192.168.1.100", new TestCommunication());
        var dataPointGroups = CreateTestDataPointGroups(1);
#pragma warning disable CA2000 // TestClient lifecycle managed by ConnectionManager mock
        var testClient = new TestClient { ReadClient = _readClient };
#pragma warning restore CA2000

        _connectionManager.AcquireConnectedAsync(Arg.Any<CancellationToken>())
            .Returns(testClient);

        _readClient.ReadAsync(Arg.Any<TestDataPointGroup>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new TestIncomingDataPort(
            deviceNode,
            dataPointGroups,
            _loggerFactory,
            _connectionManager,
            timeProvider: _timeProvider);

        // Act - Connect, disconnect, then reconnect
        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100);
        await sut.DisconnectAsync(CancellationToken.None);
        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100);

        // Assert - Should successfully reconnect without errors
        await _connectionManager.Received(2).AcquireConnectedAsync(Arg.Any<CancellationToken>());

        // Cleanup
        await sut.DisposeAsync();
    }

    // Helper methods

    private static List<TestDataPointGroup> CreateTestDataPointGroups(int count)
    {
        var groups = new List<TestDataPointGroup>(count);

        for (var i = 0; i < count; i++)
        {
            groups.Add(new TestDataPointGroup
            {
                DataPoints = CreateTestDataPoints(2),
                PollFrequency = TimeSpan.FromMilliseconds(100)
            });
        }

        return groups;
    }

    private static List<IDataPoint> CreateTestDataPoints(int count)
    {
        var dataPoints = new List<IDataPoint>(count);

        for (var i = 0; i < count; i++)
        {
            dataPoints.Add(new TestDataPoint
            {
                Channels = new Channels(
                    [],
                    [new TransferredChannel($"Channel{i}")])
            });
        }

        return dataPoints;
    }
}
