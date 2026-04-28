using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Client;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Mapping;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Outgoing;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Smoke tests to verify TestOutgoingDataPort infrastructure works correctly.
/// </summary>
public class TestOutgoingDataPortSmokeTests
{
    private readonly TestClient _testClient = new();
    private readonly TestClientLifecycleManager _connectionManager;

    public TestOutgoingDataPortSmokeTests()
    {
        _connectionManager = new TestClientLifecycleManager(_ => _testClient);
    }

    private TestOutgoingDataPort CreateTestOutgoingDataPort(
        TestCommunication communication,
        ILoggerFactory loggerFactory,
        IDelayProvider? delayProvider = null,
        IRetryPolicy? retryPolicy = null)
    {
        var typedNodeMapper = CreateTestNodeMapper();
        var mapper = new TestDataPointGroupsMapper();
        return new TestOutgoingDataPort(
            communication,
            typedNodeMapper,
            mapper,
            communication.QueueConfiguration,
            _connectionManager,
            loggerFactory,
            delayProvider,
            retryPolicy);
    }

    private static TypedNodeMapper<TestCommunication, TestDeviceNode> CreateTestNodeMapper(
        List<TestDataPointNode>? dataPointNodes = null)
    {
        var rootMapper = Substitute.For<IRootConfigurationNodeMapper<TestDeviceNode, TestCommunication>>();
        rootMapper.CreateRootNode(Arg.Any<TestCommunication>())
            .Returns(callInfo =>
            {
                var node = new TestDeviceNode { OriginalCommunication = callInfo.Arg<TestCommunication>() };
                foreach (var dp in dataPointNodes ?? [])
                    node.DataPointNodes.Add(dp);
                return node;
            });
        rootMapper.Validate(Arg.Any<TestCommunication>())
            .Returns(new ValidationResult());
        return new TypedNodeMapper<TestCommunication, TestDeviceNode>(rootMapper, [], []);
    }

    [Fact]
    public async Task Constructor_ShouldCreateValidInstance()
    {
        // Arrange
        var communication = new TestCommunication
        {
            IpAddress = "192.168.1.1",
            Port = 102,
            QueueConfiguration = new QueueConfiguration(QueueStrategy.DropOldest, new QueueSize(10))
        };
        var loggerFactory = NullLoggerFactory.Instance;
        var delayProvider = new InstantDelayProvider();

        // Act
        await using var sut = CreateTestOutgoingDataPort(communication, loggerFactory, delayProvider);

        // Assert
        sut.Should().NotBeNull();
        _connectionManager.AcquireCallCount.Should().Be(0, "no connection acquired before ConnectAsync");
    }

    [Fact]
    public async Task ConnectAsync_ShouldAcquireConnection()
    {
        // Arrange
        var communication = new TestCommunication { IpAddress = "192.168.1.1", Port = 102 };
        var loggerFactory = NullLoggerFactory.Instance;
        var delayProvider = new InstantDelayProvider();

        await using var sut = CreateTestOutgoingDataPort(communication, loggerFactory, delayProvider);

        // Act
        await sut.ConnectAsync(CancellationToken.None);

        // Assert
        _connectionManager.AcquireCallCount.Should().Be(1, "one connection should be acquired");
    }

    [Fact]
    public async Task DisconnectAsync_ShouldReleaseConnection()
    {
        // Arrange
        var communication = new TestCommunication { IpAddress = "192.168.1.1", Port = 102 };
        var loggerFactory = NullLoggerFactory.Instance;
        var delayProvider = new InstantDelayProvider();

        await using var sut = CreateTestOutgoingDataPort(communication, loggerFactory, delayProvider);
        await sut.ConnectAsync(CancellationToken.None);

        // Act
        await sut.DisconnectAsync(CancellationToken.None);

        // Assert
        _connectionManager.ReleaseCallCount.Should().Be(1, "connection should be released");
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var communication = new TestCommunication { IpAddress = "192.168.1.1", Port = 102 };
        var loggerFactory = NullLoggerFactory.Instance;
        var delayProvider = new InstantDelayProvider();

        var sut = CreateTestOutgoingDataPort(communication, loggerFactory, delayProvider);
        await sut.ConnectAsync(CancellationToken.None);

        // Act
        await sut.DisposeAsync();

        // Assert
        _connectionManager.ReleaseCallCount.Should().Be(1, "connection should be released on dispose");
    }
}
