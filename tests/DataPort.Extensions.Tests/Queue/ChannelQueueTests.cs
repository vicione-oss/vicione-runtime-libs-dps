using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.Queue;

namespace ViciOne.Suite.DataPort.Extension.Tests.Queue;

public class ChannelQueueTests
{
    #region IsAddingCompleted and IsWorking Tests

    [Fact]
    public void IsAddingCompleted_InitiallyFalse()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        // Assert
        queue.IsAddingCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsAddingCompleted_TrueAfterCompleteAdding()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        // Act
        queue.CompleteAdding();

        // Assert
        queue.IsAddingCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsWorking_TrueWhenHasItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(1);

        // Assert
        queue.IsWorking.Should().BeTrue();
    }

    [Fact]
    public void IsWorking_TrueWhenAddingNotCompleted()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        // Assert - even with no items, IsWorking is true because adding is not completed
        queue.IsWorking.Should().BeTrue();
    }

    [Fact]
    public void IsWorking_FalseWhenEmptyAndAddingCompleted()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        // Act
        queue.CompleteAdding();

        // Assert
        queue.IsWorking.Should().BeFalse();
    }

    #endregion

    #region Batch Append Tests

    [Fact]
    public void AppendList_AddsAllItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        var items = new List<int>
        {
            1,
            2,
            3,
            4,
            5
        };

        // Act
        queue.Append(items);

        // Assert
        queue.Count.Should().Be(5);
    }

    [Fact]
    public void AppendEnumerable_AddsAllItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        IEnumerable<int> items = Enumerable.Range(1, 10);

        // Act
        queue.Append(items);

        // Assert
        queue.Count.Should().Be(10);
    }

    private IReadOnlyCollection<int> GetItems(int count)
    {
        return new List<int>(Enumerable.Range(1, count));
    }

    [Fact]
    public void AppendSpan_AddsAllItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        ReadOnlySpan<int> items = stackalloc int[] { 1, 2, 3, 4, 5 };

        // Act
        queue.Append(items);

        // Assert
        queue.Count.Should().Be(5);
    }

    [Fact]
    public void AppendSpan_MaintainsFifoOrder()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        ReadOnlySpan<int> items = stackalloc int[] { 1, 2, 3 };

        // Act
        queue.Append(items);

        // Assert
        queue.TryTake(out var first).Should().BeTrue();
        first.Should().Be(1);
        queue.TryTake(out var second).Should().BeTrue();
        second.Should().Be(2);
        queue.TryTake(out var third).Should().BeTrue();
        third.Should().Be(3);
    }

    [Fact]
    public void AppendSpan_EmptySpan_DoesNothing()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        ReadOnlySpan<int> items = ReadOnlySpan<int>.Empty;

        // Act
        queue.Append(items);

        // Assert
        queue.Count.Should().Be(0);
    }

    #endregion

    #region TakeTo Tests

    [Fact]
    public void TakeTo_FillsBufferCompletely()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });
        Span<int> buffer = stackalloc int[3];

        // Act
        var taken = queue.TakeTo(buffer);

        // Assert
        taken.Should().Be(3);
        buffer[0].Should().Be(1);
        buffer[1].Should().Be(2);
        buffer[2].Should().Be(3);
        queue.Count.Should().Be(2);
    }

    [Fact]
    public void TakeTo_PartialFill_WhenQueueHasFewerItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int> { 1, 2 });
        Span<int> buffer = stackalloc int[5];

        // Act
        var taken = queue.TakeTo(buffer);

        // Assert
        taken.Should().Be(2);
        buffer[0].Should().Be(1);
        buffer[1].Should().Be(2);
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void TakeTo_ReturnsZero_WhenQueueIsEmpty()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        Span<int> buffer = stackalloc int[5];

        // Act
        var taken = queue.TakeTo(buffer);

        // Assert
        taken.Should().Be(0);
    }

    [Fact]
    public void TakeTo_MaintainsFifoOrder()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int>
        {
            10,
            20,
            30,
            40,
            50
        });
        Span<int> buffer = stackalloc int[5];

        // Act
        var taken = queue.TakeTo(buffer);

        // Assert
        taken.Should().Be(5);
        buffer[0].Should().Be(10);
        buffer[1].Should().Be(20);
        buffer[2].Should().Be(30);
        buffer[3].Should().Be(40);
        buffer[4].Should().Be(50);
    }

    #endregion

    #region TryTake Tests

    [Fact]
    public void TryTake_ReturnsTrueAndItem_WhenQueueHasItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(42);

        // Act
        var result = queue.TryTake(out var item);

        // Assert
        result.Should().BeTrue();
        item.Should().Be(42);
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void TryTake_ReturnsFalse_WhenQueueIsEmpty()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        // Act
        var result = queue.TryTake(out var item);

        // Assert
        result.Should().BeFalse();
        item.Should().Be(default);
    }

    [Fact]
    public void TryTake_RemovesItemsInFifoOrder()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        // Act & Assert
        queue.TryTake(out var first).Should().BeTrue();
        first.Should().Be(1);

        queue.TryTake(out var second).Should().BeTrue();
        second.Should().Be(2);

        queue.TryTake(out var third).Should().BeTrue();
        third.Should().Be(3);
    }

    #endregion

    #region Take Tests

    [Fact]
    public void TakeCount_ReturnsRequestedNumberOfItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });

        // Act
        var items = queue.Take(3);

        // Assert
        items.Should().HaveCount(3);
        items.Should().ContainInOrder(1, 2, 3);
        queue.Count.Should().Be(2);
    }

    [Fact]
    public void TakeCount_ReturnsFewerItems_WhenNotEnoughAvailable()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int> { 1, 2 });

        // Act
        var items = queue.Take(5);

        // Assert
        items.Should().HaveCount(2);
        items.Should().ContainInOrder(1, 2);
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void TakeCountOut_ReturnsTrueWhenItemsTaken()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int> { 1, 2, 3 });

        // Act
        var result = queue.Take(2, out var items);

        // Assert
        result.Should().BeTrue();
        items.Should().HaveCount(2);
    }

    [Fact]
    public void TakeCountOut_ReturnsFalseWhenQueueEmpty()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        // Act
        var result = queue.Take(2, out var items);

        // Assert
        result.Should().BeFalse();
        items.Should().BeEmpty();
    }

    #endregion

    #region WaitForWorkAsync Tests

    [Fact]
    public async Task WaitForWorkAsync_ReturnsImmediately_WhenItemsAvailable()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(1);

        // Act
        var result = await queue.WaitForWorkAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForWorkAsync_ReturnsFalse_WhenAddingCompletedAndEmpty()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.CompleteAdding();

        // Act
        var result = await queue.WaitForWorkAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WaitForWorkAsync_WaitsUntilItemAdded()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        // Act
        var waitTask = queue.WaitForWorkAsync();

        // Simulate delay before adding item
        await Task.Delay(50);
        queue.Append(42);

        var result = await waitTask;

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });

        // Act
        queue.Clear();

        // Assert
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_DoesNotAffectIsAddingCompleted()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int> { 1, 2, 3 });

        // Act
        queue.Clear();

        // Assert
        queue.IsAddingCompleted.Should().BeFalse();
    }

    #endregion

    #region IAsyncEnumerable Tests

    [Fact]
    public async Task AsyncEnumerable_YieldsAllItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });
        queue.CompleteAdding();

        var results = new List<int>();

        // Act
        await foreach (var item in queue)
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(5);
        results.Should().ContainInOrder(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task AsyncEnumerable_SupportsEarlyTerminationViaBreak()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });

        var results = new List<int>();

        // Act
        await foreach (var item in queue)
        {
            results.Add(item);
            if (results.Count == 2)
                break;
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().ContainInOrder(1, 2);
    }

    #endregion

    [Fact]
    public async Task PeekAsync_AfterDequeue_ReturnsNextItem()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(1);
        queue.Append(2);

        // Act
        var first = await queue.PeekAsync();
        await queue.MarkItemAsReadAsync(first, CancellationToken.None);
        var second = await queue.PeekAsync();

        // Assert
        first.Should().Be(1);
        second.Should().Be(2);
    }

    [Fact]
    public async Task DequeueAsync_RemovesPeekedItem()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(1);
        queue.Append(2);

        // Act
        var item = await queue.PeekAsync();
        await queue.MarkItemAsReadAsync(item, CancellationToken.None);
        var next = await queue.PeekAsync();

        // Assert
        next.Should().Be(2); // First item removed
        queue.Count.Should().Be(1);
    }

    [Fact]
    public async Task Count_IncludesPeekedItem()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        // Act
        await queue.PeekAsync();

        // Assert
        queue.Count.Should().Be(3); // All 3 items still counted
    }

    [Fact]
    public async Task Count_AfterDequeue_DecrementsByOne()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        // Act
        var item = await queue.PeekAsync();
        await queue.MarkItemAsReadAsync(item, CancellationToken.None);

        // Assert
        queue.Count.Should().Be(2);
    }

    [Fact]
    public void Capacity_ReturnsCorrectValue()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(50, QueueStrategy.DropOldest, mockLogger, "test");

        // Assert
        queue.Capacity.Should().Be(50);
    }

    [Fact]
    public async Task CompleteAdding_DuringPeek_PeekCompletes()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");
        queue.Append(42);

        // Act
        var item = await queue.PeekAsync();
        queue.CompleteAdding();

        // Assert
        item.Should().Be(42);
    }

    [Fact]
    public async Task ConcurrentPeekAndAppend_NoRaceConditions()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();

        // Pre-fill with initial items
        using var queue = new ChannelQueue<int>(1000, QueueStrategy.DropOldest, mockLogger, "test");

        for (var i = 1; i <= 100; i++)
        {
            queue.Append(i);
        }

        // Act - Concurrent peek/dequeue and append operations
        var results = await Task.WhenAll(PeekItemsAsync(queue), AppendItemsAsync(queue));

        // Assert
        results[0].Should().Be(50);
        results[1].Should().Be(100);
        queue.Count.Should().Be(150); // 100 initial + 100 appended - 50 dequeued
    }

    private static async Task<int> PeekItemsAsync(ChannelQueue<int> queue)
    {
        var count = 0;
        for (var i = 0; i < 50; i++)
        {
            var item = await queue.PeekAsync();
            await queue.MarkItemAsReadAsync(item, CancellationToken.None);
            count++;
        }

        return count;
    }

    private static Task<int> AppendItemsAsync(ChannelQueue<int> queue)
    {
        var count = 0;
        for (var i = 101; i <= 200; i++)
        {
            queue.Append(i);
            count++;
        }

        return Task.FromResult(count);
    }

    [Fact]
    public async Task Append_ExceedingCapacity_DropsOldestItems()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        using var queue = new ChannelQueue<int>(10, QueueStrategy.DropOldest, mockLogger, "test");

        // Act - Add 20 items to a queue with capacity 10
        for (var i = 1; i <= 20; i++)
        {
            queue.Append(i);
            await Task.Delay(1); // Small delay to ensure ordering
        }

        // Assert - Should have only the last 10 items (11-20)
        var items = new List<int>();
        while (queue.Count > 0)
        {
            var item = await queue.PeekAsync();
            items.Add(item);
            await queue.MarkItemAsReadAsync(item, CancellationToken.None);
        }

        items.Count.Should().BeLessThanOrEqualTo(10);
        items.Should().NotContain(1); // First item should be dropped
        items.Should().Contain(20); // Last item should be present
    }

    [Fact]
    public async Task Dispose_CompletesChannel()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var queue = new ChannelQueue<int>(100, QueueStrategy.DropOldest, mockLogger, "test");

        queue.Append(1);

        // Act - Peek, dequeue (drains queue), then dispose
        var item = await queue.PeekAsync();
        item.Should().Be(1);
        await queue.MarkItemAsReadAsync(item, CancellationToken.None);
        queue.Dispose();

        // Assert - PeekAsync should not throw after disposal when queue is empty
        var result = await queue.PeekAsync();
        result.Should().Be(0);
    }
}
