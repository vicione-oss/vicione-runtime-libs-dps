using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class ManagedQueuePipeline_ProcessBatchAsync
{
    [Fact]
    public async Task Retries_to_publish_Async()
    {
        using ManagedQueue<int> queue = new();
        List<int> values = [1, 2,];
        var count = 0;
        var resiliencePipeline = new TaskPipeline<bool>(
        3,
        retrycount =>
        {
            var delay = retrycount switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(0),
                2 => TimeSpan.FromSeconds(0),
                _ => TimeSpan.FromSeconds(0),
            };

            return delay;
        },
        false);

        await ManagedQueuePipeline<int>.ProcessBatchAsync(values, queue, Publish, resiliencePipeline, () => { }, () => { });

        queue.Count.Should().Be(0);

        ValueTask<bool> Publish(List<int> _1, CancellationToken _2)
        {
            count++;

            if (count == 3)
                return new ValueTask<bool>(true);

            return new ValueTask<bool>(false);
        }
    }

    [Fact]
    public async Task Prepends_failed_items_publish_Async()
    {
        using ManagedQueue<int> queue = new();
        List<int> values = [1, 2,];
        var resiliencePipeline = new TaskPipeline<bool>(
        3,
        retrycount =>
        {
            var delay = retrycount switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(0),
                2 => TimeSpan.FromSeconds(0),
                _ => TimeSpan.FromSeconds(0),
            };

            return delay;
        },
        false);

        await ManagedQueuePipeline<int>.ProcessBatchAsync(values, queue, (_, _) => ValueTask.FromResult(false), resiliencePipeline, () => { }, () => { });

        queue.Take(queue.Count).Should().HaveCount(2).And.ContainInOrder(1, 2);
    }

    [Fact]
    public async Task Clears_queue_if_adding_is_completed_and_publish_failed_Async()
    {
        using ManagedQueue<int> queue = new();
        queue.Append(1);
        queue.CompleteAdding();
        List<int> values = [1, 2,];
        var resiliencePipeline = new TaskPipeline<bool>(
        3,
        retrycount =>
        {
            var delay = retrycount switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(0),
                2 => TimeSpan.FromSeconds(0),
                _ => TimeSpan.FromSeconds(0),
            };

            return delay;
        },
        false);

        await ManagedQueuePipeline<int>.ProcessBatchAsync(values, queue, (_, _) => ValueTask.FromResult(false), resiliencePipeline, () => { }, () => { });

        queue.Count.Should().Be(0);
    }
}
