using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort;

internal class AutoResetEventAsync : IDisposable
{
    private readonly SemaphoreSlim _mutex = new(0, 1);
    private int _signal;

    public void Set()
    {
        if (Interlocked.CompareExchange(ref _signal, 1, 0) == 0)
            _mutex.Release();
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { }
        finally
        {
            Interlocked.Exchange(ref _signal, 0);
        }
    }

    public void Dispose()
        => _mutex.Dispose();
}
