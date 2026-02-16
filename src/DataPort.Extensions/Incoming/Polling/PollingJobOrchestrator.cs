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
/// <item>Call <see cref="StartAll"/> to create and start polling jobs (supports reconnection)</item>
/// <item>Call <see cref="StopAllAsync"/> to cancel and stop all running jobs</item>
/// <item>Call <see cref="DisposeAsync"/> for final cleanup</item>
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
    private CancellationTokenSource _cancellationSource = new();

    /// <summary>
    /// Initializes a new instance of the PollingJobOrchestrator class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    /// <param name="timeProvider">Time provider for job scheduling.</param>
    internal PollingJobOrchestrator(
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(GetType());
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates polling jobs for each group and starts periodic execution.
    /// </summary>
    /// <param name="groups">Data point groups to poll.</param>
    /// <param name="readClient">Client for reading data point values.</param>
    /// <param name="onDataReceived">Callback invoked when data is received.</param>
    /// <remarks>
    /// Creates a fresh CancellationTokenSource on each call to support reconnection scenarios.
    /// </remarks>
    [SuppressMessage("ReSharper", "ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator")]
    internal void StartAll(
        List<TDataPointGroup> groups,
        TReadClient readClient,
        Action<IReadOnlyList<TDataPointValue>> onDataReceived)
    {
        // Reset cancellation if needed (reconnection scenario)
        if (_cancellationSource.IsCancellationRequested)
        {
            _cancellationSource.Dispose();
            _cancellationSource = new CancellationTokenSource();
        }

        _logger.CreatingPollingJobs(groups.Count);

        var jobs =
            new List<DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>>(groups.Count);
        foreach (var t in groups)
        {
            jobs.Add(CreatePollingJob(t, readClient, onDataReceived, _cancellationSource.Token));
        }

        _pollingJobs = jobs;
        foreach (var t in _pollingJobs)
        {
            t.StartPeriodicExecution();
        }

        _logger.PollingJobsCreatedAndStarted(_pollingJobs.Count);
    }

    /// <summary>
    /// Cancels the CancellationTokenSource and stops all running jobs.
    /// </summary>
    internal async Task StopAllAsync()
    {
        _logger.StoppingPollingJobs(_pollingJobs.Count);

        await _cancellationSource.CancelAsync().ConfigureAwait(false);

        var tasks = new Task[_pollingJobs.Count];
        for (var i = 0; i < _pollingJobs.Count; i++)
        {
            tasks[i] = _pollingJobs[i].StopPeriodicExecution();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        _logger.PollingJobsStopped();
    }

    /// <summary>
    /// Stops and disposes all jobs, clears the list, and disposes the CancellationTokenSource.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _cancellationSource.CancelAsync().ConfigureAwait(false);
        await StopAllJobsAsync().ConfigureAwait(false);
        await DisposeAllJobsAsync().ConfigureAwait(false);
        _pollingJobs.Clear();
        _cancellationSource.Dispose();
    }

    /// <summary>
    /// Creates a single polling job for a data point group.
    /// </summary>
    private DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient> CreatePollingJob(
        TDataPointGroup group,
        TReadClient readClient,
        Action<IReadOnlyList<TDataPointValue>> onDataReceived,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);

        return new DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>(
            group.PollFrequency,
            group,
            readClient,
            onDataReceived,
            timeProvider: _timeProvider,
            logger: _loggerFactory
                .CreateLogger<DataPointGroupPollingJob<TDataPointGroup, TDataPoint, TDataPointValue, TReadClient>>(),
            externalCancellation: cancellationToken);
    }

    /// <summary>
    /// Stops all polling jobs.
    /// </summary>
    private async Task StopAllJobsAsync()
    {
        var tasks = new Task[_pollingJobs.Count];
        for (var i = 0; i < _pollingJobs.Count; i++)
        {
            tasks[i] = _pollingJobs[i].StopPeriodicExecution();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
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
