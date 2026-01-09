using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class ManagedQueueTests_
{
    [Fact]
    public void SetsIsWorkingToFalseIfQueueIsEmptyAndAddingIsCompleted()
    {
        var source = new List<int> { 0, 1, };
        using var queue = new ManagedQueue<int>(source);

        queue.CompleteAdding();
        queue.IsWorking.Should().BeTrue();
        queue.Take(2);
        queue.IsWorking.Should().BeFalse();
    }
}

public class ManagedQueue_Append_Items
{
    [Fact]
    public void AppendItems()
    {
        var source = new List<int>();
        using var queue = new ManagedQueue<int>(source);
        var batch1 = new[] { 0, 1, };
        var batch2 = new[] { 2, 3, };

        queue.Append(batch1);
        queue.Append(batch2);

        source.Should().HaveCount(4).And.ContainInOrder(0, 1, 2, 3);
    }

    [Fact]
    public void RemovesTwentyPercentIfQueueReachedCapacity()
    {
        var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, };
        var items = new[] { 11, 12, 13 };
        using var queue = new ManagedQueue<int>(source, 10);

        queue.Append(items);

        source.Should().HaveCount(8).And.ContainInOrder(6, 7, 8, 9, 10, 11, 12, 13);
    }

    [Theory]
    [InlineData(10, 10, 10)]
    [InlineData(13, 10, 8)]
    [InlineData(8, 10, 8)]
    [InlineData(5, 10, 5)]
    [InlineData(144, 111, 89)]
    public void RemovesTwentyPercentWithCorrectRounding(int count, int capacity, int expectedCount)
    {
        var source = Enumerable.Range(0, count).ToList();
        using var queue = new ManagedQueue<int>(source, capacity);

        queue.Append([]);

        source.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void CanHandleMoreItemsToAddThanCapacity()
    {
        var source = new List<int> { 1, 2, 3, 4, };
        var items = new[] { 5, 6, 7, 8, 9 };
        using var queue = new ManagedQueue<int>(source, 4);

        queue.Append(items);

        source.Should().HaveCount(3).And.ContainInOrder(7, 8, 9);
    }

    [Fact]
    public void Logs_warning_if_limit_is_reached()
    {
        TestLogger<ManagedQueue<int>> logger = new();
        using var queue = new ManagedQueue<int>([1], 1, logger, "my list");
        var items = new[] { 2, 3, };

        queue.Append(items);

        logger.Calls.Should().Be(1);
        logger.Message.Should().MatchEquivalentOf("*limit*my list*2*remove*");
    }
}

public class ManagedQueue_Append_Item
{
    [Fact]
    public void Appends_item()
    {
        List<int> source = [];
        using ManagedQueue<int> queue = new(source);

        queue.Append(1);
        queue.Append(2);

        source.Should().HaveCount(2).And.ContainInOrder(1, 2);
    }
}

public class ManagedQueue_Take
{
    [Fact]
    public void CanTakeItems()
    {
        var source = new List<int> { 0, 1, 2, 3, };
        using var queue = new ManagedQueue<int>(source);

        var items = queue.Take(2);

        var expectedItems = new[] { 0, 1, };
        source.Should().HaveCount(2).And.ContainInOrder(2, 3);
        items.Should().HaveCount(2).And.Contain(expectedItems);
    }

    [Fact]
    public void CanHandleCountThatIsGreaterThenQueueCount()
    {
        var source = new List<int> { 0, 1, 2, 3, };
        using var queue = new ManagedQueue<int>(source);

        var items = queue.Take(10);

        source.Should().BeEmpty();
        items.Should().HaveCount(4);
    }
}

public class ManagedQueue_Take_OUT
{
    [Fact]
    public void SetsItemsVariableIfResultIsTrue()
    {
        var source = new List<int> { 0, 1, 2, 3, };
        using var queue = new ManagedQueue<int>(source);

        var result = queue.Take(2, out var items);

        result.Should().BeTrue();
        items.Should().HaveCount(2);
    }
}

public class ManagedQueue_Prepend
{
    [Fact]
    public void PrependsItems()
    {
        var source = new List<int> { 0, 1, };
        var items = new[] { 2, 3, };
        using var queue = new ManagedQueue<int>(source);

        queue.Prepend(items);

        source.Should().HaveCount(4).And.ContainInOrder(2, 3, 0, 1);
    }
}
