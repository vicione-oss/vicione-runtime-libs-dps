using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Incoming.Client;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Incoming.Polling;

/// <summary>
/// Manages the lifecycle of polling jobs for data point groups.
/// Encapsulates job creation, execution, cancellation, and disposal.
/// </summary>
/// <typeparam name="TDataPointGroup">Data point group type</typeparam>
/// <typeparam name="TDataPoint">Polling data point type</typeparam>
/// <typeparam name="TDataPointValue">Read result type with value</typeparam>
/// <typeparam name="TReadClient">Client type for read operations</typeparam>
/// <remarks>
/// <para><b>Responsibilities:</b></para>
/// <list type="bullet">
/// <item>Creates one <see cref="DataPointGroupPollingJob{TDataPointGroup,TDataPoint,TDataPointValue,TClient}"/> per polling group</item>
/// <item>Manages CancellationTokenSource for coordinated job cancellation</item>
/// <item>Coordinates parallel job start, stop, and disposal operations</item>
/// </list>
/// <para><b>Lifecycle:</b></para>
/// <list type="number">
/// <item>Create via constructor with all dependencies including client, groups, and callback</item>
/// <item>Call <see cref="Start"/> to create and start polling jobs</item>
/// <item>Call <see cref="DisposeAsync"/> to stop and dispose all jobs</item>
/// </list>
/// </remarks>
internal sealed class PollingJobOrchestrator<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>
    : IAsyncDisposable
    where TDataPoint : IPollingDataPoint
    where TDataPointValue : IDataPointValue<TDataPoint>
    where TDataPointGroup : IDataPointPollingGroup<TDataPoint>
    where TReadClient : IReadClient<TDataPointGroup, TDataPointValue>
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TimeProvider _timeProvider;
    private List<DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>> _pollingJobs = [];
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly TReadClient _readClient;
    private readonly IReadOnlyList<TDataPointGroup> _groups;
    private readonly Action<IReadOnlyList<TDataPointValue>> _onDataReceived;

    /// <summary>
    /// Initializes a new instance of the PollingJobOrchestrator class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    /// <param name="timeProvider">Time provider for job scheduling.</param>
    /// <param name="readClient">Client for reading data point values.</param>
    /// <param name="groups">Data point groups to poll.</param>
    /// <param name="onDataReceived">Callback invoked when data is received.</param>
    internal PollingJobOrchestrator(
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider,
        TReadClient readClient,
        IReadOnlyList<TDataPointGroup> groups,
        Action<IReadOnlyList<TDataPointValue>> onDataReceived)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(GetType());
        _timeProvider = timeProvider;
        _readClient = readClient;
        _groups = groups;
        _onDataReceived = onDataReceived;
    }

    /// <summary>
    /// Creates polling jobs for each group and starts periodic execution.
    /// </summary>
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    internal void Start()
    {
        _logger.CreatingPollingJobs(_groups.Count);

        var jobs =
            new List<DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>>(
                _groups.Count);
        foreach (var group in _groups)
        {
            jobs.Add(CreatePollingJob(group, _cancellationSource.Token));
        }

        _pollingJobs = jobs;
        foreach (var job in _pollingJobs)
        {
            job.StartPeriodicExecution();
        }

        _logger.PollingJobsCreatedAndStarted(_pollingJobs.Count);
    }

    /// <summary>
    /// Stops and disposes all jobs, clears the list, and disposes the CancellationTokenSource.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _logger.StoppingPollingJobs(_pollingJobs.Count);
        await _cancellationSource.CancelAsync().ConfigureAwait(false);
        await DisposeAllJobsAsync().ConfigureAwait(false);
        _logger.PollingJobsStopped();
        _pollingJobs.Clear();
        _cancellationSource.Dispose();
    }

    /// <summary>
    /// Creates a single polling job for a data point group.
    /// </summary>
    private DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient> CreatePollingJob(
        TDataPointGroup group,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);

        return new DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>(
            group.PollFrequency,
            group,
            _readClient,
            _onDataReceived,
            timeProvider: _timeProvider,
            logger: _loggerFactory
                .CreateLogger<DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>>(),
            externalCancellation: cancellationToken);
    }

    /// <summary>
    /// Disposes all polling jobs.
    /// </summary>
    private async Task DisposeAllJobsAsync()
    {
        var tasks = new Task[_pollingJobs.Count];
        for (var i = 0; i < _pollingJobs.Count; i++)
        {
            tasks[i] = _pollingJobs[i].DisposeAsync().AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
