using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Client;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Incoming;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Mapping;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Verification;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Testing.Logging;
using ViciOne.Suite.DataPort.Extensions.Verification;

namespace ViciOne.Suite.DataPort.Extension.Tests.Incoming;

public class IncomingDataPortBaseTests
{
    private readonly ILoggerFactory _loggerFactory = TestLogging.CreateLoggerFactory();
    private readonly TestClientLifecycleManager _connectionManager;
    private readonly TestClient _readClient;
    private readonly FakeTimeProvider _timeProvider = new();

    public IncomingDataPortBaseTests()
    {
        _readClient = new TestClient();
        _connectionManager = new TestClientLifecycleManager(_ => _readClient);
    }

    [Fact]
    public async Task ConnectAsync_ShouldAcquireClientAndStartPolling()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodes(4); // 4 points with same frequency = 1 group
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
            _connectionManager,
            timeProvider: _timeProvider);

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        // Small delay to allow polling jobs to start
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert
        _connectionManager.AcquireCallCount.Should().Be(1);

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsync_WhenValidationFails_ShouldDisconnect()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodes(2);
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);


        var misConfiguredDataPoint = new MisconfiguredDataPoint<TestDataPoint>(
            new TestDataPoint(),
            [new MismatchingConfiguration("Test mismatch")]);

        var verifier = new TestDataPointConfigurationVerifier
        {
            VerifyResult = [misConfiguredDataPoint]
        };

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
            _connectionManager,
            timeProvider: _timeProvider,
            configurationVerifier: verifier);


        // Act
        var act = async () => await sut.ConnectAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidConfigurationException>()
            .WithMessage("*misconfigured dataPoints*");

        // Verify DisconnectAsync was called (releases client)
        _connectionManager.ReleaseCallCount.Should().Be(1);

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldStopPollingAndReleaseClient()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodes(4);
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
            _connectionManager,
            timeProvider: _timeProvider);

        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100, TestContext.Current.CancellationToken); // Allow polling to start

        // Act
        await sut.DisconnectAsync(CancellationToken.None);

        // Assert
        _connectionManager.ReleaseCallCount.Should().Be(1);

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task StartPollingAndPublishingDataPoints_ShouldCreateJobsForEachGroup()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodesWithDifferentFrequencies(3); // 3 groups with different frequencies
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
            _connectionManager,
            timeProvider: _timeProvider);

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        // Allow initial connection and job setup
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Advance time to trigger polling for all groups (groups have 100ms, 150ms, 200ms frequencies)
        _timeProvider.Advance(TimeSpan.FromMilliseconds(250));
        await Task.Delay(200, TestContext.Current.CancellationToken); // Allow jobs to execute

        // Assert
        // Each group should have been read at least once
        _readClient.ReadCallCount.Should().BeGreaterThanOrEqualTo(3);

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task PublishDataPointValues_ShouldRaiseReceivedEvent()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodes(2);
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);

        var testDataPointValues = new List<TestDataPointValue>
        {
            new()
            {
                TransferredChannels = ["Channel1", "Channel2"],
                Value = 42
            }
        };

        _readClient.ReadResult = testDataPointValues;

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
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
        await Task.Delay(100, TestContext.Current.CancellationToken); // Allow job to execute and event to fire

        // Assert
        receivedEventFired.Should().BeTrue();
        receivedValues.Should().NotBeNull();
        receivedValues.Count.Should().Be(2); // 2 transferred channels

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task ToExternalValues_ShouldApplyTimestampToAllValues()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodes(2);
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);
        var expectedTimestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        _timeProvider.SetUtcNow(expectedTimestamp);

        var testDataPointValues = new List<TestDataPointValue>
        {
            new()
            {
                TransferredChannels = ["Channel1"],
                Value = 123
            },
            new()
            {
                TransferredChannels = ["Channel2"],
                Value = 456
            }
        };

        _readClient.ReadResult = testDataPointValues;

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
            _connectionManager,
            timeProvider: _timeProvider);

        IReadOnlyCollection<ExternalValue>? receivedValues = null;
        sut.Received += values => receivedValues = values;

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert
        receivedValues.Should().NotBeNull();
        var firstTimestamp = receivedValues.First().Timestamp;
        receivedValues.All(v => v.Timestamp == firstTimestamp).Should().BeTrue();

        // Cleanup
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupAllResources()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodes(4);
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
            _connectionManager,
            timeProvider: _timeProvider);

        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Act
        await sut.DisposeAsync();

        // Assert
        _connectionManager.ReleaseCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ConnectAsync_WithReconnection_ShouldResetCancellationToken()
    {
        // Arrange
        var communication = new TestCommunication();
        var dataPointNodes = CreateTestDataPointNodes(2);
        var nodeMapper = CreateTestNodeMapper("192.168.1.100", dataPointNodes);

        var sut = new TestIncomingDataPort(
            communication,
            _loggerFactory,
            nodeMapper,
            new TestDataPointGroupsMapper(),
            _connectionManager,
            timeProvider: _timeProvider);

        // Act - Connect, disconnect, then reconnect
        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await sut.DisconnectAsync(CancellationToken.None);
        await sut.ConnectAsync(CancellationToken.None);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert - Should successfully reconnect without errors
        _connectionManager.AcquireCallCount.Should().Be(2);

        // Cleanup
        await sut.DisposeAsync();
    }

    // Helper methods

    private static TypedNodeMapper<TestCommunication, TestDeviceNode> CreateTestNodeMapper(
        string deviceAddress,
        List<TestDataPointNode>? dataPointNodes = null)
    {
        var rootMapper = Substitute.For<IRootConfigurationNodeMapper<TestDeviceNode, TestCommunication>>();
        rootMapper.CreateRootNode(Arg.Any<TestCommunication>())
            .Returns(callInfo =>
            {
                var node = new TestDeviceNode(deviceAddress, callInfo.Arg<TestCommunication>());
                if (dataPointNodes != null)
                {
                    foreach (var dpNode in dataPointNodes)
                    {
                        node.DataPointNodes.Add(dpNode);
                    }
                }

                return node;
            });
        rootMapper.Validate(Arg.Any<TestCommunication>())
            .Returns(new ValidationResult());

        return new TypedNodeMapper<TestCommunication, TestDeviceNode>(
            rootMapper,
            [],
            []);
    }

    private static List<TestDataPointNode> CreateTestDataPointNodesWithDifferentFrequencies(int groupCount)
    {
        var dataPointNodes = new List<TestDataPointNode>();

        for (var i = 0; i < groupCount; i++)
        {
            var pollFrequency = new PollFrequency(TimeSpan.FromMilliseconds(100 + (i * 50)));
            dataPointNodes.AddRange(CreateTestDataPointNodes(2, pollFrequency));
        }

        return dataPointNodes;
    }

    private static List<TestDataPointNode> CreateTestDataPointNodes(int count, PollFrequency? pollFrequency = null)
    {
        var dataPointNodes = new List<TestDataPointNode>(count);
        var frequency = pollFrequency ?? new PollFrequency(TimeSpan.FromMilliseconds(100));

        for (var i = 0; i < count; i++)
        {
            var originalNode = new LinkedNode(
                new Node
                {
                    TransferredChannels = [$"Channel{i}"]
                });
            dataPointNodes.Add(new TestDataPointNode(originalNode)
            {
                PollFrequency = frequency
            });
        }

        return dataPointNodes;
    }
}
