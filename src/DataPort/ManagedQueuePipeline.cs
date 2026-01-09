using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort;

public sealed class ManagedQueuePipeline<T>(
    int queueSize,
    string queueName,
    int bulkSize,
    ILogger logger,
    Action publishFailed,
    Action publishWithCompletedQueueFailed,
    TaskPipeline<bool>? taskPipeline = null,
    TimeSpan? minimumPublishInterval = null) : IDisposable where T : notnull
{
    private readonly int _queueSize = queueSize;
    private readonly string _queueName = queueName;
    private readonly int _bulkSize = bulkSize;
    private readonly ILogger _logger = logger;
    private readonly TimeSpan? _minimumPublishInterval = minimumPublishInterval;
    private readonly TaskPipeline<bool> _taskPipeline = taskPipeline ?? new(3, retrycount =>
    {
        return retrycount switch
        {
            0 => TimeSpan.Zero,
            1 => TimeSpan.FromSeconds(1),
            2 => TimeSpan.FromSeconds(5),
            _ => TimeSpan.FromSeconds(10),
        };
    },
        false);

    private ManagedQueue<T>? _queue;
    private Task? _queueTask;
    private Func<List<T>, CancellationToken, ValueTask<bool>>? _publish;

    public int Count => _queue?.Count ?? 0;

    public void Start(Func<List<T>, CancellationToken, ValueTask<bool>> publishAsync, CancellationToken cancellationToken)
    {
        _queue ??= new ManagedQueue<T>(_queueSize, _logger, _queueName);
        _publish ??= publishAsync;
        _queueTask ??= Task.Run(ProcessQueueAsyncWithInterval, cancellationToken);
    }

    public void Append(T item) => _queue?.Append(item);

    public void Append(IEnumerable<T> items) => _queue?.Append(items);

    public async Task Complete()
    {
        _queue?.CompleteAdding();

        if (_queueTask is not null)
        {
            await _queueTask.ConfigureAwait(false);
            _queueTask.Dispose();
            _queueTask = null;
        }

        _queue?.Dispose();
        _queue = null;
    }

    private async Task ProcessQueueAsyncWithInterval()
    {
        if (_queue is null || _publish is null)
            throw new InvalidOperationException("Queue must be initialized.");

        await foreach (var items in _queue.GetBufferEnumerator(_bulkSize, _minimumPublishInterval))
        {
            if (items.Count == 0)
                continue;

            await ProcessBatchAsync(items, _queue, _publish, _taskPipeline, publishFailed, publishWithCompletedQueueFailed);
        }
    }

    internal static async Task ProcessBatchAsync(List<T> items, ManagedQueue<T> queue, Func<List<T>, CancellationToken, ValueTask<bool>> publishAsync, TaskPipeline<bool> resiliencePipeline, Action publishFailed, Action publishWithCompletedQueueFailed)
    {
        if (!await resiliencePipeline.ExecuteAsync(cancellationToken => publishAsync(items, cancellationToken), CancellationToken.None).ConfigureAwait(false))
        {
            if (queue.IsAddingCompleted)
            {
                publishWithCompletedQueueFailed();
                queue.Clear();
            }
            else
            {
                publishFailed();
                queue.Prepend(items);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _queue?.CompleteAdding();
            try
            {
                _queueTask?.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                _logger.LogQueueProcessingFailed(exception);
            }
            _queueTask?.Dispose();
            _queue?.Dispose();
        }
    }
}
