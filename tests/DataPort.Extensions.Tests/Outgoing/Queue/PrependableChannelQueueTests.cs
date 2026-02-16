using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Prependable;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing.Queue;

public class PrependableChannelQueueTests
{
    private readonly ILogger _mockLogger = Substitute.For<ILogger>();
    private readonly CancellationToken _testCancellationToken = TestContext.Current.CancellationToken;

    #region Basic Properties Tests

    [Fact]
    public void IsAddingCompleted_InitiallyFalse()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.IsAddingCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsAddingCompleted_TrueAfterCompleteAdding()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.CompleteAdding();

        queue.IsAddingCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsWorking_TrueWhenHasItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);

        queue.IsWorking.Should().BeTrue();
    }

    [Fact]
    public void IsWorking_TrueWhenAddingNotCompleted()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.IsWorking.Should().BeTrue();
    }

    [Fact]
    public void IsWorking_FalseWhenEmptyAndAddingCompleted()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.CompleteAdding();

        queue.IsWorking.Should().BeFalse();
    }

    [Fact]
    public void Capacity_ReturnsCorrectValue()
    {
        using var queue = new PrependableChannelQueue<int>(50, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Capacity.Should().Be(50);
    }

    #endregion

    #region Append Tests

    [Fact]
    public void Append_AddsItemToQueue()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Append(42);

        queue.Count.Should().Be(1);
    }

    [Fact]
    public void AppendList_AddsAllItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        var items = new List<int>
        {
            1,
            2,
            3,
            4,
            5
        };

        queue.Append(items);

        queue.Count.Should().Be(5);
    }

    [Fact]
    public void AppendEnumerable_AddsAllItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        IEnumerable<int> items = Enumerable.Range(1, 10);

        queue.Append(items);

        queue.Count.Should().Be(10);
    }

    [Fact]
    public void AppendSpan_AddsAllItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        ReadOnlySpan<int> items = [1, 2, 3, 4, 5];

        queue.Append(items);

        queue.Count.Should().Be(5);
    }

    [Fact]
    public void AppendSpan_MaintainsFifoOrder()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        ReadOnlySpan<int> items = stackalloc int[] { 1, 2, 3 };

        queue.Append(items);

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
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        ReadOnlySpan<int> items = ReadOnlySpan<int>.Empty;

        queue.Append(items);

        queue.Count.Should().Be(0);
    }

    [Fact]
    public void Append_MaintainsFifoOrder()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        queue.TryTake(out var first).Should().BeTrue();
        first.Should().Be(1);
        queue.TryTake(out var second).Should().BeTrue();
        second.Should().Be(2);
        queue.TryTake(out var third).Should().BeTrue();
        third.Should().Be(3);
    }

    #endregion

    #region Prepend Tests

    [Fact]
    public void Prepend_AddsItemToQueue()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Prepend(42);

        queue.Count.Should().Be(1);
    }

    [Fact]
    public void Prepend_ShouldBeReadBeforeAppend()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        // Append first
        queue.Append(1);
        queue.Append(2);

        // Then prepend
        queue.Prepend(99);

        // Prepended item should be read first
        queue.TryTake(out var item).Should().BeTrue();
        item.Should().Be(99);

        // Then the appended items
        queue.TryTake(out var second).Should().BeTrue();
        second.Should().Be(1);
    }

    [Fact]
    public void MultiplePrepends_MaintainFifoOrderWithinPriority()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        // Prepend multiple items
        queue.Prepend(1);
        queue.Prepend(2);
        queue.Prepend(3);

        // They should come out in FIFO order (1, 2, 3) as each goes to the end of priority channel
        queue.TryTake(out var first).Should().BeTrue();
        first.Should().Be(1);
        queue.TryTake(out var second).Should().BeTrue();
        second.Should().Be(2);
        queue.TryTake(out var third).Should().BeTrue();
        third.Should().Be(3);
    }

    [Fact]
    public void PrependEnumerable_AddsAllItemsToFront()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Append(100);
        IEnumerable<int> itemsToAdd = [1, 2, 3];
        queue.Prepend(itemsToAdd);

        // Prepended items should be read first (in order 1, 2, 3)
        var items = queue.Take(4);
        items.Should().ContainInOrder(1, 2, 3, 100);
    }

    [Fact]
    public void PrependSpan_AddsAllItemsToFront()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Append(100);
        ReadOnlySpan<int> items = stackalloc int[] { 1, 2, 3 };
        queue.Prepend(items);

        var result = queue.Take(4);
        result.Should().ContainInOrder(1, 2, 3, 100);
    }

    [Fact]
    public void PrependSpan_MaintainsFifoOrderWithinPriority()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        ReadOnlySpan<int> items = stackalloc int[] { 1, 2, 3 };

        queue.Prepend(items);

        queue.TryTake(out var first).Should().BeTrue();
        first.Should().Be(1);
        queue.TryTake(out var second).Should().BeTrue();
        second.Should().Be(2);
        queue.TryTake(out var third).Should().BeTrue();
        third.Should().Be(3);
    }

    [Fact]
    public void PrependSpan_EmptySpan_DoesNothing()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        ReadOnlySpan<int> items = ReadOnlySpan<int>.Empty;

        queue.Prepend(items);

        queue.Count.Should().Be(0);
    }

    [Fact]
    public void PrependList_AddsAllItemsToFront()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Append(100);
        queue.Prepend(new List<int>
        {
            1,
            2,
            3
        });

        var result = queue.Take(4);
        result.Should().ContainInOrder(1, 2, 3, 100);
    }

    [Fact]
    public void InterleavedPrependAndAppend_CorrectOrder()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        queue.Append(1); // Normal: [1]
        queue.Prepend(10); // Priority: [10]
        queue.Append(2); // Normal: [1, 2]
        queue.Prepend(20); // Priority: [10, 20]
        queue.Append(3); // Normal: [1, 2, 3]

        // Should read all priority first, then all normal
        var items = queue.Take(5);
        items.Should().ContainInOrder(10, 20, 1, 2, 3);
    }

    #endregion

    #region TryTake Tests

    [Fact]
    public void TryTake_ReturnsTrueAndItem_WhenQueueHasItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(42);

        var result = queue.TryTake(out var item);

        result.Should().BeTrue();
        item.Should().Be(42);
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void TryTake_ReturnsFalse_WhenQueueIsEmpty()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        var result = queue.TryTake(out var item);

        result.Should().BeFalse();
        item.Should().Be(default);
    }

    #endregion

    #region Take Tests

    [Fact]
    public void TakeCount_ReturnsRequestedNumberOfItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });

        var items = queue.Take(3);

        items.Should().HaveCount(3);
        items.Should().ContainInOrder(1, 2, 3);
        queue.Count.Should().Be(2);
    }

    [Fact]
    public void TakeCount_ReturnsFewerItems_WhenNotEnoughAvailable()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2
        });

        var items = queue.Take(5);

        items.Should().HaveCount(2);
        items.Should().ContainInOrder(1, 2);
    }

    [Fact]
    public void TakeCountOut_ReturnsTrueWhenItemsTaken()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3
        });

        var result = queue.Take(2, out var items);

        result.Should().BeTrue();
        items.Should().HaveCount(2);
    }

    [Fact]
    public void TakeCountOut_ReturnsFalseWhenQueueEmpty()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        var result = queue.Take(2, out var items);

        result.Should().BeFalse();
        items.Should().BeEmpty();
    }

    #endregion

    #region TakeTo Tests

    [Fact]
    public void TakeTo_FillsBufferCompletely()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });
        Span<int> buffer = stackalloc int[3];

        var taken = queue.TakeTo(buffer);

        taken.Should().Be(3);
        buffer[0].Should().Be(1);
        buffer[1].Should().Be(2);
        buffer[2].Should().Be(3);
        queue.Count.Should().Be(2);
    }

    [Fact]
    public void TakeTo_PartialFill_WhenQueueHasFewerItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2
        });
        Span<int> buffer = stackalloc int[5];

        var taken = queue.TakeTo(buffer);

        taken.Should().Be(2);
        buffer[0].Should().Be(1);
        buffer[1].Should().Be(2);
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void TakeTo_ReturnsZero_WhenQueueIsEmpty()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        Span<int> buffer = stackalloc int[5];

        var taken = queue.TakeTo(buffer);

        taken.Should().Be(0);
    }

    [Fact]
    public void TakeTo_PriorityItemsFirst()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2
        });
        queue.Prepend(new List<int>
        {
            10,
            20
        });
        Span<int> buffer = stackalloc int[4];

        var taken = queue.TakeTo(buffer);

        taken.Should().Be(4);
        buffer[0].Should().Be(10);
        buffer[1].Should().Be(20);
        buffer[2].Should().Be(1);
        buffer[3].Should().Be(2);
    }

    #endregion

    #region WaitForWorkAsync Tests

    [Fact]
    public async Task WaitForWorkAsync_ReturnsImmediately_WhenItemsAvailable()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);

        var result = await queue.WaitForWorkAsync(_testCancellationToken);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForWorkAsync_ReturnsFalse_WhenAddingCompletedAndEmpty()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.CompleteAdding();

        var result = await queue.WaitForWorkAsync(_testCancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task WaitForWorkAsync_WaitsUntilItemAdded()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        var waitTask = queue.WaitForWorkAsync(_testCancellationToken);

        await Task.Delay(50, _testCancellationToken);
        queue.Append(42);

        var result = await waitTask;

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForWorkAsync_WakesOnPrepend()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");

        var waitTask = queue.WaitForWorkAsync(_testCancellationToken);

        await Task.Delay(50, _testCancellationToken);
        queue.Prepend(42);

        var result = await waitTask;

        result.Should().BeTrue();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3
        });
        queue.Prepend(new List<int>
        {
            10,
            20
        });

        queue.Clear();

        queue.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_DoesNotAffectIsAddingCompleted()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3
        });

        queue.Clear();

        queue.IsAddingCompleted.Should().BeFalse();
    }

    #endregion

    #region Peek Tests

    [Fact]
    public async Task PeekAsync_ReturnsPriorityItemFirst()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);
        queue.Prepend(99);

        var item = await queue.PeekAsync(_testCancellationToken);

        item.Should().Be(99);
    }

    [Fact]
    public async Task PeekAsync_DoesNotRemoveItem()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(42);

        var first = await queue.PeekAsync(_testCancellationToken);
        var second = await queue.PeekAsync(_testCancellationToken);

        first.Should().Be(42);
        second.Should().Be(42);
        queue.Count.Should().Be(1);
    }

    [Fact]
    public async Task PeekAsync_AfterMarkAsRead_ReturnsNextItem()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);
        queue.Append(2);

        var first = await queue.PeekAsync(_testCancellationToken);
        await queue.MarkItemAsReadAsync(first, CancellationToken.None);
        var second = await queue.PeekAsync(_testCancellationToken);

        first.Should().Be(1);
        second.Should().Be(2);
    }

    #endregion

    #region IsItemStillHead Tests

    [Fact]
    public async Task IsItemStillHead_ReturnsTrueForCurrentHead()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(42);
        var item = await queue.PeekAsync(_testCancellationToken);

        queue.IsItemStillHead(item).Should().BeTrue();
    }

    [Fact]
    public async Task IsItemStillHead_ReturnsFalseAfterItemRemoved()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);
        queue.Append(2);

        var first = await queue.PeekAsync(_testCancellationToken);
        await queue.MarkItemAsReadAsync(first, CancellationToken.None);

        queue.IsItemStillHead(first).Should().BeFalse();
    }

    [Fact]
    public async Task IsItemStillHead_WorksAcrossChannels()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);

        var appended = await queue.PeekAsync(_testCancellationToken);
        appended.Should().Be(1);

        // Now prepend a higher priority item
        queue.Prepend(99);

        // The appended item is no longer the head
        queue.IsItemStillHead(appended).Should().BeFalse();

        // The prepended item is now the head
        queue.IsItemStillHead(99).Should().BeTrue();
    }

    #endregion

    #region Overflow Strategy Tests

    [Fact]
    public void DropOldest_DropsNormalChannelFirst()
    {
        using var queue = new PrependableChannelQueue<int>(3, QueueStrategy.DropOldest, _mockLogger, "test");

        // Fill the queue
        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        // Add one more - should drop oldest (1)
        queue.Append(4);

        var items = queue.Take(3);
        items.Should().Contain(4);
        items.Should().NotContain(1);
    }

    [Fact]
    public void DropOldest_PreservesPriorityItems()
    {
        using var queue = new PrependableChannelQueue<int>(3, QueueStrategy.DropOldest, _mockLogger, "test");

        // Add priority items
        queue.Prepend(10);
        queue.Prepend(20);

        // Add normal item to fill
        queue.Append(1);

        // Add more - should drop normal item first
        queue.Append(2);

        var items = queue.Take(3);
        items.Should().Contain(10);
        items.Should().Contain(20);
        items.Should().Contain(2);
        items.Should().NotContain(1);
    }

    [Fact]
    public void DropNewest_RejectsNewItems()
    {
        using var queue = new PrependableChannelQueue<int>(3, QueueStrategy.DropNewest, _mockLogger, "test");

        // Fill the queue
        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        // Try to add more - should be rejected
        queue.Append(4);
        queue.Append(5);

        queue.Count.Should().Be(3);
        var items = queue.Take(3);
        items.Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void DropNewest_RejectsPrependedItemsToo()
    {
        using var queue = new PrependableChannelQueue<int>(3, QueueStrategy.DropNewest, _mockLogger, "test");

        // Fill the queue
        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        // Try to prepend - should be rejected
        queue.Prepend(99);

        queue.Count.Should().Be(3);
        var items = queue.Take(3);
        items.Should().ContainInOrder(1, 2, 3);
        items.Should().NotContain(99);
    }

    #endregion

    #region Batch Overflow Strategy Tests

    [Fact]
    public void AppendSpan_DropOldest_DropsOldestItems()
    {
        using var queue = new PrependableChannelQueue<int>(5, QueueStrategy.DropOldest, _mockLogger, "test");

        // Fill the queue
        queue.Append(new List<int>
        {
            1,
            2,
            3
        });

        // Batch append that exceeds capacity
        ReadOnlySpan<int> items = stackalloc int[] { 10, 20, 30, 40 };
        queue.Append(items);

        // Should have dropped oldest items to make room
        queue.Count.Should().Be(5);
        var result = queue.Take(5);
        result.Should().Contain(40); // Newest should be present
        result.Should().Contain(30);
    }

    [Fact]
    public void AppendSpan_DropNewest_OnlyAddsWhatFits()
    {
        using var queue = new PrependableChannelQueue<int>(5, QueueStrategy.DropNewest, _mockLogger, "test");

        // Fill the queue partially
        queue.Append(new List<int>
        {
            1,
            2,
            3
        });

        // Batch append that would exceed capacity
        ReadOnlySpan<int> items = stackalloc int[] { 10, 20, 30, 40 };
        queue.Append(items);

        // Should only have added what fits (2 items)
        queue.Count.Should().Be(5);
        var result = queue.Take(5);
        result.Should().ContainInOrder(1, 2, 3, 10, 20);
    }

    [Fact]
    public void PrependSpan_DropOldest_DropsFromNormalChannelFirst()
    {
        using var queue = new PrependableChannelQueue<int>(5, QueueStrategy.DropOldest, _mockLogger, "test");

        // Add normal items
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4
        });

        // Batch prepend that exceeds capacity - should drop from normal channel
        ReadOnlySpan<int> items = stackalloc int[] { 10, 20, 30 };
        queue.Prepend(items);

        queue.Count.Should().Be(5);
        var result = queue.Take(5);
        // Priority items should all be present
        result.Should().Contain(10);
        result.Should().Contain(20);
        result.Should().Contain(30);
    }

    [Fact]
    public void PrependSpan_DropNewest_OnlyAddsWhatFits()
    {
        using var queue = new PrependableChannelQueue<int>(5, QueueStrategy.DropNewest, _mockLogger, "test");

        // Fill the queue partially
        queue.Append(new List<int>
        {
            1,
            2,
            3
        });

        // Batch prepend that would exceed capacity
        ReadOnlySpan<int> items = stackalloc int[] { 10, 20, 30, 40 };
        queue.Prepend(items);

        // Should only have added what fits (2 items)
        queue.Count.Should().Be(5);
        var result = queue.Take(5);
        result.Should().ContainInOrder(10, 20, 1, 2, 3);
    }

    [Fact]
    public void AppendSpan_LargeBatch_HandlesOverflowCorrectly()
    {
        using var queue = new PrependableChannelQueue<int>(10, QueueStrategy.DropOldest, _mockLogger, "test");

        // Fill with initial items
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });

        // Large batch that forces multiple drops
        var largeData = new int[15];
        for (var i = 0; i < 15; i++)
            largeData[i] = 100 + i;

        queue.Append(largeData.AsSpan());

        // Queue should be at capacity with newest items
        queue.Count.Should().Be(10);
        var result = queue.Take(10);
        result.Should().Contain(114); // Last item from large batch
    }

    #endregion

    #region Async Enumeration Tests

    [Fact]
    public async Task AsyncEnumerable_YieldsAllItems()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
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
        await foreach (var item in queue)
        {
            results.Add(item);
        }

        results.Should().HaveCount(5);
        results.Should().ContainInOrder(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task AsyncEnumerable_YieldsPriorityFirst()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);
        queue.Append(2);
        queue.Prepend(10);
        queue.Prepend(20);
        queue.CompleteAdding();

        var results = new List<int>();
        await foreach (var item in queue)
        {
            results.Add(item);
        }

        results.Should().ContainInOrder(10, 20, 1, 2);
    }

    [Fact]
    public async Task AsyncEnumerable_SupportsEarlyTermination()
    {
        using var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(new List<int>
        {
            1,
            2,
            3,
            4,
            5
        });

        var results = new List<int>();
        await foreach (var item in queue)
        {
            results.Add(item);
            if (results.Count == 2)
                break;
        }

        results.Should().HaveCount(2);
        results.Should().ContainInOrder(1, 2);
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task ConcurrentAppendAndPrepend_NoRaceConditions()
    {
        using var queue = new PrependableChannelQueue<int>(1000, QueueStrategy.DropOldest, _mockLogger, "test");

        // Pre-fill
        for (var i = 1; i <= 100; i++)
            queue.Append(i);

        // Concurrent operations
        var appendTask = Task.Run(() =>
        {
            for (var i = 101; i <= 200; i++)
                queue.Append(i);
            return 100;
        });

        var prependTask = Task.Run(() =>
        {
            for (var i = 1001; i <= 1050; i++)
                queue.Prepend(i);
            return 50;
        });

        var consumeTask = Task.Run(() =>
        {
            var count = 0;
            for (var i = 0; i < 50; i++)
            {
                if (queue.TryTake(out _))
                    count++;
            }

            return count;
        });

        var results = await Task.WhenAll(appendTask, prependTask, consumeTask);

        results[0].Should().Be(100); // appended
        results[1].Should().Be(50); // prepended
        // 100 initial + 100 appended + 50 prepended - consumed
        queue.Count.Should().BeGreaterThanOrEqualTo(100);
    }

    [Fact]
    public async Task ConcurrentPeekAndAppend_NoRaceConditions()
    {
        using var queue = new PrependableChannelQueue<int>(1000, QueueStrategy.DropOldest, _mockLogger, "test");

        // Pre-fill
        for (var i = 1; i <= 100; i++)
            queue.Append(i);

        var peekTask = Task.Run(async () =>
        {
            var count = 0;
            for (var i = 0; i < 50; i++)
            {
                var item = await queue.PeekAsync(_testCancellationToken);
                await queue.MarkItemAsReadAsync(item, CancellationToken.None);
                count++;
            }

            return count;
        });

        var appendTask = Task.Run(() =>
        {
            var count = 0;
            for (var i = 101; i <= 200; i++)
            {
                queue.Append(i);
                count++;
            }

            return count;
        });

        var results = await Task.WhenAll(peekTask, appendTask);

        results[0].Should().Be(50);
        results[1].Should().Be(100);
        queue.Count.Should().Be(150); // 100 + 100 - 50
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_CompletesChannels()
    {
        var queue = new PrependableChannelQueue<int>(100, QueueStrategy.DropOldest, _mockLogger, "test");
        queue.Append(1);

        var item = await queue.PeekAsync(_testCancellationToken);
        item.Should().Be(1);
        await queue.MarkItemAsReadAsync(item, CancellationToken.None);
        queue.Dispose();

        // PeekAsync should return default after disposal when queue is empty
        var result = await queue.PeekAsync(_testCancellationToken);
        result.Should().Be(0);
    }

    #endregion
}
