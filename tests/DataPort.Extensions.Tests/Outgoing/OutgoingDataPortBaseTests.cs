#pragma warning disable CA2012 // Use ValueTasks correctly
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Outgoing;
using ViciOne.Suite.DataPort.Extensions.Queue;
using ViciOne.Suite.DataPort.Extensions.Resilience;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Comprehensive tests for OutgoingDataPortBase template method pattern.
/// Tests retry logic, queue backpressure, cancellation, edge cases, and lifecycle.
/// </summary>
public sealed class OutgoingDataPortBaseTests
{
    private readonly IWriteClient<TestDataPointValue> _mockWriteClient;
    private readonly ILogger<TestOutgoingDataPort> _logger;
    private readonly InstantDelayProvider _instantDelayProvider;

    public OutgoingDataPortBaseTests()
    {
        _mockWriteClient = Substitute.For<IWriteClient<TestDataPointValue>>();
        _logger = Substitute.For<ILogger<TestOutgoingDataPort>>();
        _instantDelayProvider = new InstantDelayProvider();
    }

    private static TestCommunication CreateTestCommunication(int maxPendingMessages = 10)
    {
        return new TestCommunication
        {
            IpAddress = "192.168.1.10",
            Port = 102,
            DataPoints = [new() { ChannelName = "channel1" }, new() { ChannelName = "channel2" }],
            MaxPendingMessages = maxPendingMessages,
            OverflowStrategy = QueueStrategy.DropOldest
        };
    }

    private static ExternalValue CreateExternalValue(string channel, object? value)
    {
        return new ExternalValue { Channel = channel, Value = value };
    }

    #region Retry Logic Tests (4 tests)

    [Fact]
    public async Task SendAsync_WriteFailsThenSucceeds_RetriesUntilSuccess()
    {
        // Arrange
        var callCount = 0;
        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount < 4)
                    throw new InvalidOperationException("Write failed");
                return ValueTask.CompletedTask;
            });

        var communication = CreateTestCommunication();
        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(500); // Let retry loop complete

        // Assert
        callCount.Should().Be(4, "should retry 3 times then succeed");
        await _mockWriteClient.Received(4).WriteAsync(
            Arg.Any<IReadOnlyList<TestDataPointValue>>(),
            Arg.Any<CancellationToken>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_WriteFails5Times_LogsProgressiveWarnings()
    {
        // Arrange
        var callCount = 0;
        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount <= 5)
                    throw new InvalidOperationException("Write failed");
                return ValueTask.CompletedTask;
            });

        // Enable logging at all levels
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var communication = CreateTestCommunication();
        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(500); // Let retry loop complete

        // Assert
        callCount.Should().Be(6, "should fail 5 times then succeed");

        // Verify Debug logs for attempts 1-4 (EventId 200)
        _logger.Received(4).Log(
            LogLevel.Debug,
            new EventId(200),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        // Verify Warning log for attempt 5 (EventId 201)
        _logger.Received(1).Log(
            LogLevel.Warning,
            new EventId(201),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_WriteFails20Times_LogsAtAttempts5And10And20()
    {
        // Arrange
        var callCount = 0;
        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount <= 20)
                    throw new InvalidOperationException("Write failed");
                return ValueTask.CompletedTask;
            });

        // Enable logging at all levels
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var communication = CreateTestCommunication();
        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(500); // Let retry loop complete

        // Assert
        callCount.Should().Be(21, "should fail 20 times then succeed");

        // Verify Debug logs for attempts 1-4 (EventId 200)
        _logger.Received(4).Log(
            LogLevel.Debug,
            new EventId(200),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        // Verify Warning logs only at attempts 5, 10, 20 (EventId 201)
        // Total warnings should be 3 (not 16 for all attempts 5-20)
        _logger.Received(3).Log(
            LogLevel.Warning,
            new EventId(201),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_RetryUsesCancellationTokenNone()
    {
        // Arrange
        CancellationToken? capturedToken = null;
        var callCount = 0;

        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                capturedToken = callInfo.Arg<CancellationToken>();

                if (callCount < 3)
                    throw new InvalidOperationException("Write failed");
                return ValueTask.CompletedTask;
            });

        var communication = CreateTestCommunication();
        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(500); // Let retry loop complete

        // Assert
        capturedToken.Should().NotBeNull();
        capturedToken.Should().Be(CancellationToken.None,
            "retries should always use CancellationToken.None to prevent mid-write cancellation");

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    #endregion

    #region Queue Backpressure Tests (4 tests)

    [Fact]
    public async Task SendAsync_QueueBackpressure_DropsHeadItem()
    {
        // Arrange - Small queue with capacity=2
        var communication = CreateTestCommunication(maxPendingMessages: 2);

        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Write always fails to trigger retries"));

        // Enable warning logging
        _logger.IsEnabled(LogLevel.Warning).Returns(true);

        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act - Enqueue 3 items rapidly (causes backpressure with capacity=2)
        await dataPort.SendAsync([CreateExternalValue("channel1", 1)]);
        await Task.Delay(50); // Let first item start retrying
        await dataPort.SendAsync([CreateExternalValue("channel1", 2)]);
        await dataPort.SendAsync([CreateExternalValue("channel1", 3)]);
        await Task.Delay(500); // Let backpressure detection trigger

        // Assert - Oldest item should be dropped due to queue overflow (DropOldest strategy)
        // Verify backpressure log (EventId 205)
        _logger.Received().Log(
            LogLevel.Warning,
            new EventId(205),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_QueueBackpressure_ContinuesWithNextItem()
    {
        // Arrange - Small queue with capacity=2
        var communication = CreateTestCommunication(maxPendingMessages: 2);
        var writeCallCount = 0;

        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                writeCallCount++;
                // First 10 calls fail, then succeed
                if (writeCallCount <= 10)
                    throw new InvalidOperationException("Write failed");
                return ValueTask.CompletedTask;
            });

        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act - Enqueue multiple items to trigger backpressure
        await dataPort.SendAsync([CreateExternalValue("channel1", 1)]);
        await Task.Delay(50);
        await dataPort.SendAsync([CreateExternalValue("channel1", 2)]);
        await dataPort.SendAsync([CreateExternalValue("channel1", 3)]);
        await Task.Delay(500); // Let processing continue

        // Assert - Queue processing should continue after backpressure
        writeCallCount.Should().BeGreaterThan(10, "should continue processing after backpressure");

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_QueueNotFull_DoesNotDropItem()
    {
        // Arrange - Large queue (capacity=100)
        var communication = CreateTestCommunication(maxPendingMessages: 100);
        var writeCallCount = 0;

        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                writeCallCount++;
                // Fail 5 times then succeed
                if (writeCallCount <= 5)
                    throw new InvalidOperationException("Write failed");
                return ValueTask.CompletedTask;
            });

        // Enable warning logging
        _logger.IsEnabled(LogLevel.Warning).Returns(true);

        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act - Enqueue single item with retries
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(500); // Let retries complete

        // Assert - No backpressure warning should be logged (EventId 205)
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            new EventId(205),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        writeCallCount.Should().Be(6, "should retry 5 times then succeed without dropping");

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_BackpressureDuringRetry_VerifiesIsItemStillHeadDetection()
    {
        // Arrange - Queue with capacity=2
        var communication = CreateTestCommunication(maxPendingMessages: 2);

        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Write always fails"));

        // Enable warning logging
        _logger.IsEnabled(LogLevel.Warning).Returns(true);

        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act
        // 1. Enqueue item1 (starts retrying due to write failure)
        await dataPort.SendAsync([CreateExternalValue("channel1", 1)]);
        await Task.Delay(50); // Let item1 start retrying

        // 2. Enqueue item2 (queue now has: item1 at head, item2 in queue)
        await dataPort.SendAsync([CreateExternalValue("channel1", 2)]);

        // 3. Enqueue item3 (triggers overflow: DropOldest removes item1, queue now has: item2, item3)
        await dataPort.SendAsync([CreateExternalValue("channel1", 3)]);

        await Task.Delay(500); // Let IsItemStillHead detection trigger

        // Assert - item1 should be abandoned due to backpressure (not at head anymore)
        _logger.Received().Log(
            LogLevel.Warning,
            new EventId(205), // AbandoningRetryDueToBackpressure
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    #endregion

    #region Cancellation Test (1 test)

    [Fact]
    public async Task SendAsync_InfiniteRetry_CanBeCancelledViaDisconnect()
    {
        // Arrange - InfiniteRetryPolicy that never gives up
        var communication = CreateTestCommunication();
        var retryPolicy = new InfiniteRetryPolicy();

        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Write always fails"));

        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider,
            retryPolicy: retryPolicy);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act - Start infinite retry loop
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(200); // Let retries start

        // Disconnect should cancel the retry loop
        var disconnectTask = dataPort.DisconnectAsync(CancellationToken.None);
        var completedTask = await Task.WhenAny(disconnectTask, Task.Delay(2000));

        // Assert - Disconnect should complete within timeout (not hang forever)
        completedTask.Should().Be(disconnectTask, "DisconnectAsync should complete and cancel infinite retry loop");
        await disconnectTask; // Verify no exceptions
    }

    #endregion

    #region Edge Case Tests (2 tests)

    [Fact]
    public async Task SendAsync_EmptyValues_DoesNotThrow()
    {
        // Arrange
        var communication = CreateTestCommunication();
        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act & Assert - Invoke immediately to avoid capturing disposed variable
        var act = async () => await dataPort.SendAsync([]);
        await act.Should().NotThrowAsync("empty collection should be handled gracefully");

        // Verify WriteAsync was never called
        await _mockWriteClient.DidNotReceive().WriteAsync(
            Arg.Any<IReadOnlyList<TestDataPointValue>>(),
            Arg.Any<CancellationToken>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_NonExistentChannel_LogsWarning()
    {
        // Arrange
        _logger.IsEnabled(LogLevel.Warning).Returns(true);

        var communication = CreateTestCommunication();
        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act
        await dataPort.SendAsync([CreateExternalValue("nonexistent_channel", 42)]);
        await Task.Delay(200);

        // Assert - Verify UnmappedChannelsBatch warning (EventId 208) for batch logging
        _logger.Received().Log(
            LogLevel.Warning,
            new EventId(208),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        // Verify WriteAsync was never called (no valid channels)
        await _mockWriteClient.DidNotReceive().WriteAsync(
            Arg.Any<IReadOnlyList<TestDataPointValue>>(),
            Arg.Any<CancellationToken>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    #endregion

    #region Dispose Lifecycle Tests (3 tests)

    [Fact]
    public async Task DisposeAsync_WithoutConnect_DoesNotThrow()
    {
        // Arrange
        var communication = CreateTestCommunication();
#pragma warning disable CA2000 // Dispose objects before losing scope - intentional for test
        var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);
#pragma warning restore CA2000

        // Act
        var act = async () => await dataPort.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync("DisposeAsync should be safe without ConnectAsync");
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var communication = CreateTestCommunication();
        var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);

        // Act
        await dataPort.DisposeAsync();
        var act = async () => await dataPort.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync("second DisposeAsync call should be idempotent");
    }

    [Fact]
    public async Task DisposeAsync_CancelsCancellationTokenSource()
    {
        // Arrange
        var writeCallCount = 0;
        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref writeCallCount);
                throw new InvalidOperationException("Write fails to keep queue processing active");
            });

        var communication = CreateTestCommunication();
        var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        await dataPort.ConnectAsync(CancellationToken.None);
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(100); // Let queue processing start

        // Verify queue was active
        writeCallCount.Should().BeGreaterThan(0, "queue processing should have started");

        // Act - DisposeAsync should complete without hanging
        var disposeTask = dataPort.DisposeAsync().AsTask();
        var completedTask = await Task.WhenAny(disposeTask, Task.Delay(5000));

        // Assert
        completedTask.Should().Be(disposeTask, "DisposeAsync should complete within timeout (cancellation works)");
        await disposeTask; // Ensure no exceptions

        // Capture count immediately after dispose completes
        var countAfterDispose = writeCallCount;

        // Wait and verify no new writes occur after dispose completed
        await Task.Delay(100);
        var countAfterWait = writeCallCount;

        countAfterWait.Should().Be(countAfterDispose,
            "no new writes should occur after DisposeAsync completes");
    }

    #endregion

    #region Connection Lifecycle Test (1 test)

    [Fact]
    public async Task ConnectAsync_InitializesQueueProcessing()
    {
        // Arrange
        var communication = CreateTestCommunication();
        await using var dataPort = new TestOutgoingDataPort(
            communication,
            _mockWriteClient,
            _logger,
            _instantDelayProvider);

        _mockWriteClient
            .WriteAsync(Arg.Any<IReadOnlyList<TestDataPointValue>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await dataPort.ConnectAsync(CancellationToken.None);

        // Assert - Verify connection established
        dataPort.TestClient.IsConnected.Should().BeTrue("connection should be acquired");
        dataPort.TestClient.ConnectionCount.Should().Be(1, "AcquireConnectionAsync should be called once");

        // Verify queue processing is active by sending a value
        await dataPort.SendAsync([CreateExternalValue("channel1", 42)]);
        await Task.Delay(200); // Let queue process

        // Verify WriteAsync was called (queue processing is active)
        await _mockWriteClient.Received(1).WriteAsync(
            Arg.Any<IReadOnlyList<TestDataPointValue>>(),
            Arg.Any<CancellationToken>());

        await dataPort.DisconnectAsync(CancellationToken.None);
    }

    #endregion
}
