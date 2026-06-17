using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using ViciOne.Suite.System.Contracts;
using ViciOne.Suite.System.Runtime;

namespace ViciOne.Suite.System.Transfer;

internal class PeriodicTransferHandler : ITransferHandler, IAsyncDisposable
{
    private readonly IRuntimeFunctionBlock _functionBlock;
    private readonly IIncomingInputLinkGroup _linkGroup;

    private readonly PeriodicTimer _timer;
    private readonly Task _task;

    private bool _pool;

    internal PeriodicTransferHandler(IRuntimeFunctionBlock functionBlock, PeriodicTransferMode transferMode, IIncomingInputLinkGroup linkGroup)
    {
        _timer = new(transferMode.Interval);
        _task = Task.Run(TickAsync);
        _functionBlock = functionBlock;
        _linkGroup = linkGroup;
    }

    private async Task TickAsync()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            Interlocked.Exchange(ref _pool, true);
            _functionBlock.MarkAsChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _timer.Dispose();
        await _task;
    }

    public void ProcessCycle()
    {
        // Pool if the timer has elapsed since the last call
        if (Interlocked.CompareExchange(ref _pool, false, true))
            _linkGroup.Pool();
    }
}
