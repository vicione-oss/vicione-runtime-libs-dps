using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class ManagedQueueBufferEnumerator_
{
    [Fact]
    public async Task Returns_next_buffer_chunk_by_count_Async()
    {
        using ManagedQueue<int> queue = new();
        await using var enumerator = queue.GetBufferEnumerator(2, cancellationToken: TestContext.Current.CancellationToken);

        queue.Append(1);
        queue.Append(2);
        queue.Append(3);

        await enumerator.MoveNextAsync();
        enumerator.Current.Should().HaveCount(2).And.ContainInOrder(1, 2);
    }

    [Fact]
    public async Task Returns_next_buffer_chunk_by_time_Async()
    {
        using ManagedQueue<int> queue = new();
        FakeTimeProvider timeProvider = new();
        await using var enumerator = new ManagedQueueBufferEnumerator<int>(queue, 2, 1.Seconds(), [], timeProvider);

        queue.Append(1);
        timeProvider.Advance(1.Seconds());

        FluentActions.Invoking(async () => await enumerator.MoveNextAsync()).ExecutionTime().Should().BeLessThan(1.Seconds());
        enumerator.Current.Should().ContainSingle();
    }

    [Fact]
    public async Task Waits_for_next_value_Async()
    {
        using ManagedQueue<int> queue = new();
        await using var enumerator = queue.GetBufferEnumerator(1, cancellationToken: TestContext.Current.CancellationToken);
        var moveNextTask = Task.Run(enumerator.MoveNextAsync);

        queue.Append(1);
        await Task.Yield();

        FluentActions.Invoking(() => moveNextTask).ExecutionTime().Should().BeLessThan(1.Seconds());
        enumerator.Current.Should().ContainSingle();
    }

    [Fact]
    public async Task Does_process_prefilled_queue_Async()
    {
        using ManagedQueue<int> queue = new();
        queue.Append(1);
        await using var enumerator = queue.GetBufferEnumerator(1, cancellationToken: TestContext.Current.CancellationToken);
        var act = async () => await enumerator.MoveNextAsync();

        act.ExecutionTime().Should().BeLessThan(1.Seconds());
        enumerator.Current.Should().ContainSingle();
    }

    [Fact]
    public async Task Returns_empty_list_if_queue_is_empty_and_canceled_Async()
    {
        using ManagedQueue<int> queue = new();
        using CancellationTokenSource tokenSource = new();
        await tokenSource.CancelAsync();
        await using var enumerator = queue.GetBufferEnumerator(2, cancellationToken: tokenSource.Token);

        (await enumerator.MoveNextAsync()).Should().BeTrue();

        enumerator.Current.Should().BeEmpty();
    }
}
