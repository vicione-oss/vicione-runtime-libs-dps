using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.Outgoing.Client;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

public class QueueProcessorTests
{
    private readonly CancellationToken _testCancellationToken = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveQueueSize_Throws(int invalidSize)
    {
        var act = () => new QueueProcessor<int>(
            new QueueConfiguration(QueueStrategy.DropOldest, new QueueSize(invalidSize)),
            new InstantDelayProvider(),
            NullLogger.Instance,
            "TestQueue");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }


    [Fact]
    public async Task StopAsync_WhenNotStarted_DoesNotThrow()
    {
        await using var sut = CreateSut();

        await sut.Invoking(s => s.StopAsync()).Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotStarted_DoesNotThrow()
    {
        await using var sut = CreateSut();

        await sut.Invoking(s => s.DisposeAsync().AsTask()).Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_WhenStarted_CompletesCleanly()
    {
        await using var sut = CreateSut();
        sut.Start(Substitute.For<IWriteClient<int>>(), InfiniteRetryPolicy());

        await sut.Invoking(s => s.StopAsync()).Should().NotThrowAsync();
    }


    [Fact]
    public async Task EnqueuedValues_AreWrittenToClient()
    {
        var written = new TaskCompletionSource<IReadOnlyList<int>>();
        var writeClient = new WriteClientStub((values, _) =>
        {
            written.TrySetResult(values);
            return ValueTask.CompletedTask;
        });

        await using var sut = CreateSut();
        sut.Start(writeClient, InfiniteRetryPolicy());
        sut.Enqueue([1, 2, 3]);
        
        var result = await written.Task.WaitAsync(TimeSpan.FromSeconds(2),_testCancellationToken);
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task WriteFailure_RetriesUntilSuccess()
    {
        var written = new TaskCompletionSource();
        var callCount = 0;
        var writeClient = new WriteClientStub((_, _) =>
        {
            if (++callCount < 3) throw new InvalidOperationException("Transient write failure");
            written.TrySetResult();
            return ValueTask.CompletedTask;
        });

        await using var sut = CreateSut();
        sut.Start(writeClient, InfiniteRetryPolicy());
        sut.Enqueue([42]);

        await written.Task.WaitAsync(TimeSpan.FromSeconds(2),_testCancellationToken);
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task MaxRetriesExceeded_ItemIsDropped_NextItemIsStillProcessed()
    {
        var secondItemWritten = new TaskCompletionSource<IReadOnlyList<int>>();
        var callCount = 0;
        var writeClient = new WriteClientStub((values, _) =>
        {
            callCount++;
            if (callCount == 1) throw new InvalidOperationException("First item always fails");
            secondItemWritten.TrySetResult(values);
            return ValueTask.CompletedTask;
        });

        await using var sut = CreateSut();
        sut.Start(writeClient, NoRetryPolicy());

        sut.Enqueue([1]); // Fails — max retries immediately exceeded
        sut.Enqueue([2]); // Should still be processed

        var result = await secondItemWritten.Task.WaitAsync(TimeSpan.FromSeconds(2),_testCancellationToken);
        result.Should().Equal(2);
    }

    [Fact]
    public async Task MultipleItems_AreProcessedSequentially()
    {
        var writtenBatches = new List<IReadOnlyList<int>>();
        var allWritten = new TaskCompletionSource();
        var writeClient = new WriteClientStub((values, _) =>
        {
            writtenBatches.Add(values);
            if (writtenBatches.Count == 3) allWritten.TrySetResult();
            return ValueTask.CompletedTask;
        });

        await using var sut = CreateSut();
        sut.Start(writeClient, InfiniteRetryPolicy());
        sut.Enqueue([1]);
        sut.Enqueue([2]);
        sut.Enqueue([3]);

        await allWritten.Task.WaitAsync(TimeSpan.FromSeconds(2),_testCancellationToken);
        writtenBatches.Should().SatisfyRespectively(
            b => b.Should().Equal(1),
            b => b.Should().Equal(2),
            b => b.Should().Equal(3));
    }

    // ── LogRetryAttempt ────────────────────────────────────────────────────────

    [Fact]
    public async Task Attempt1To4_LogsAtDebugLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);
        var delay = TimeSpan.FromMilliseconds(200);

        await using var sut = CreateSut(logger);
        sut.LogRetryAttempt(1, delay);
        sut.LogRetryAttempt(2, delay);
        sut.LogRetryAttempt(3, delay);
        sut.LogRetryAttempt(4, delay);

        logger.Received(4).Log(
            LogLevel.Debug,
            Arg.Is<EventId>(e => e.Id == 200),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Attempt5_LogsAtWarningLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var delay = TimeSpan.FromMilliseconds(500);

        await using var sut = CreateSut(logger);
        sut.LogRetryAttempt(5, delay);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 201),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Attempt6To9_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        var delay = TimeSpan.FromMilliseconds(500);

        await using var sut = CreateSut(logger);
        sut.LogRetryAttempt(6, delay);
        sut.LogRetryAttempt(7, delay);
        sut.LogRetryAttempt(8, delay);
        sut.LogRetryAttempt(9, delay);

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Every10thAttemptAfter5_LogsAtWarningLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var delay = TimeSpan.FromMilliseconds(1000);

        await using var sut = CreateSut(logger);
        sut.LogRetryAttempt(10, delay);
        sut.LogRetryAttempt(20, delay);
        sut.LogRetryAttempt(30, delay);

        logger.Received(3).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 201),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Attempt15_DoesNotLog()
    {
        var logger = Substitute.For<ILogger>();
        var delay = TimeSpan.FromMilliseconds(1000);

        await using var sut = CreateSut(logger);
        sut.LogRetryAttempt(15, delay);

        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static QueueProcessor<int> CreateSut(IDelayProvider? delayProvider = null, int queueSize = 10)
        => new(
            new QueueConfiguration(QueueStrategy.DropOldest, new QueueSize(queueSize)),
            delayProvider ?? new InstantDelayProvider(),
            NullLogger.Instance,
            "TestQueue");

    private static QueueProcessor<int> CreateSut(ILogger logger, int queueSize = 10)
        => new(
            new QueueConfiguration(QueueStrategy.DropOldest, new QueueSize(queueSize)),
            new InstantDelayProvider(),
            logger,
            "TestQueue");

    private static IRetryPolicy InfiniteRetryPolicy()
    {
        var policy = Substitute.For<IRetryPolicy>();
        policy.ShouldRetry(Arg.Any<long>()).Returns(true);
        return policy;
    }

    private static IRetryPolicy NoRetryPolicy()
    {
        var policy = Substitute.For<IRetryPolicy>();
        policy.ShouldRetry(Arg.Any<long>()).Returns(false);
        return policy;
    }

    private sealed class WriteClientStub(Func<IReadOnlyList<int>, CancellationToken, ValueTask> onWrite)
        : IWriteClient<int>
    {
        public ValueTask WriteAsync(IReadOnlyList<int> values, CancellationToken ct) => onWrite(values, ct);
    }
}
