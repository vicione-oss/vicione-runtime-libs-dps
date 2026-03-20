using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AutoResetEventAsync_
{
    [Fact]
    public async Task Skips_wait_if_signal_is_already_set_Async()
    {
        using AutoResetEventAsync autoResetEvent = new();

        autoResetEvent.Set();
        await autoResetEvent.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Waits_for_signal_Async()
    {
        using AutoResetEventAsync autoResetEvent = new();
        using SemaphoreSlim taskStartedSemaphore = new(0, 1);

        var task = Task.Run(async () =>
        {
            taskStartedSemaphore.Release();
            await autoResetEvent.WaitAsync(TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        await taskStartedSemaphore.WaitAsync(1_000, TestContext.Current.CancellationToken);
        task.Status.Should().NotBe(TaskStatus.RanToCompletion);
        autoResetEvent.Set();
        var awaitFunc = async () => await task;
        await awaitFunc.Should().CompleteWithinAsync(1.Seconds());
    }

    [Fact]
    public async Task Resets_signal_automatically_Async()
    {
        using AutoResetEventAsync autoResetEvent = new();

        autoResetEvent.Set();
        await autoResetEvent.WaitAsync(TestContext.Current.CancellationToken);

        using SemaphoreSlim taskStartedSemaphore = new(0, 1);
        var task = Task.Run(async () =>
        {
            taskStartedSemaphore.Release();
            await autoResetEvent.WaitAsync(TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);
        await taskStartedSemaphore.WaitAsync(1_000, TestContext.Current.CancellationToken);
        task.Status.Should().NotBe(TaskStatus.RanToCompletion);
        autoResetEvent.Set();
        var awaitFunc = async () => await task;
        await awaitFunc.Should().CompleteWithinAsync(1.Seconds());
    }

    [Fact]
    public async Task Can_handle_canceled_cancellation_token_Async()
    {
        using AutoResetEventAsync autoResetEvent = new();
        using CancellationTokenSource tokenSource = new();

        await tokenSource.CancelAsync();
        await autoResetEvent.WaitAsync(tokenSource.Token);
    }

    [Fact]
    public async Task Can_handle_cancel_of_cancellation_token_Async()
    {
        using AutoResetEventAsync autoResetEvent = new();
        using CancellationTokenSource tokenSource = new();

        var task = Task.Run(async () => await autoResetEvent.WaitAsync(tokenSource.Token), TestContext.Current.CancellationToken);
        await tokenSource.CancelAsync();

        await task;
    }
}
