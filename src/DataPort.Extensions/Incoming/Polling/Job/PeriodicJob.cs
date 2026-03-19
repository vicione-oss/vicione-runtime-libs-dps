using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace ViciOne.Suite.DataPort.Extensions.Incoming.Polling.Job;

/// <summary>
/// Base class for implementing periodic jobs that execute at regular intervals with resilience support.
/// Allows overlapping executions and provides customizable error handling and resilience policies.
/// </summary>
/// <typeparam name="TJobInput">The type of input data for the job.</typeparam>
/// <typeparam name="TJobOutput">The type of output data produced by the job.</typeparam>
/// <remarks>
/// <para><b>Thread Safety:</b> This class is not thread-safe. The following methods must be called from a single thread:</para>
/// <list type="bullet">
///   <item><see cref="StartPeriodicExecution"/> - Must be called once before any execution begins</item>
///   <item><see cref="StopPeriodicExecution"/> - Must be called once to stop execution</item>
///   <item><see cref="DisposeAsync"/> - Must be called once for cleanup</item>
/// </list>
/// <para>The <see cref="PerformJob"/> callback may execute concurrently with itself if execution takes longer than the interval.</para>
/// </remarks>
public abstract class PeriodicJob<TJobInput, TJobOutput> : IAsyncDisposable
{
    private readonly CancellationTokenSource _forCancelling;
    private readonly TJobInput _input;
    private readonly ILogger<PeriodicJob<TJobInput, TJobOutput>> _logger;
    private readonly ResiliencePipeline<TJobOutput> _resiliencePipeline;
    private readonly Action<TJobOutput> _onJobOutputAvailable;
    private readonly Lazy<PeriodicTimer> _timer;
    private string? _jobName;
    private Task? _periodExecutionLoop;

    /// <param name="executionInterval">The time interval between job executions.</param>
    /// <param name="input">The input data for the job.</param>
    /// <param name="onJobOutputAvailable">Callback invoked when job output is available.</param>
    /// <param name="resiliencePipeline">Optional resilience pipeline. If not provided, a default pipeline with circuit breaker and timeout is used.</param>
    /// <param name="timeProvider">Optional time provider for testing purposes.</param>
    /// <param name="logger">Optional logger for job execution events.</param>
    /// <param name="externalCancellation">Optional external cancellation token to coordinate with other components.</param>
    protected PeriodicJob(TimeSpan executionInterval,
        TJobInput input,
        Action<TJobOutput> onJobOutputAvailable,
        ResiliencePipeline<TJobOutput>? resiliencePipeline = null,
        TimeProvider? timeProvider = null,
        ILogger<PeriodicJob<TJobInput, TJobOutput>>? logger = null,
        CancellationToken? externalCancellation = null)
    {
        _input = input;
        _onJobOutputAvailable = onJobOutputAvailable;
        _forCancelling =
            CancellationTokenSource.CreateLinkedTokenSource(externalCancellation ?? CancellationToken.None);
        _logger = logger ?? new NullLogger<PeriodicJob<TJobInput, TJobOutput>>();
        _resiliencePipeline = resiliencePipeline ??
                              DefaultResiliencePipeline.CreateDefaultResiliencePipeline<TJobOutput>(executionInterval,
                                  _logger);
        _timer =
            new Lazy<PeriodicTimer>(() => new PeriodicTimer(executionInterval, timeProvider ?? TimeProvider.System));

        ExecutionInterval = executionInterval;
    }

    /// <summary>
    /// Gets the time interval between job executions.
    /// </summary>
    protected TimeSpan ExecutionInterval { get; }

    /// <summary>
    /// Performs the actual job logic. This method is called periodically based on the execution interval.
    /// </summary>
    /// <param name="input">The input data for the job.</param>
    /// <param name="token">Cancellation token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous job operation, containing the job output.</returns>
    protected abstract ValueTask<TJobOutput> PerformJob(TJobInput input, CancellationToken token);

    /// <summary>
    /// Gets the name of the job based on the input object. This is used for logging and identification purposes.
    /// </summary>
    /// <param name="input">The input data for the job.</param>
    /// <returns>A string representing the job name.</returns>
    protected abstract string GetJobName(TJobInput input);

    /// <summary>
    /// Starts the periodic execution of the job.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if periodic execution has already been started.</exception>
    public void StartPeriodicExecution()
    {
        if (_periodExecutionLoop is not null)
            throw new InvalidOperationException("Periodic execution has been already started");
        _logger.JobStarting(JobName);
        _periodExecutionLoop = StartPeriodExecutionLoop();
    }

    private async Task StartPeriodExecutionLoop()
    {
        try
        {
            while (!_forCancelling.IsCancellationRequested &&
                   await _timer.Value.WaitForNextTickAsync(_forCancelling.Token).ConfigureAwait(false))
            {
                // Fire-and-forget to allow overlapping executions when a job takes longer than the interval.
                // Parallelism is bounded by the resilience pipeline (timeout at 110% of interval by default).
                // Note: StopPeriodicExecution awaits the timer loop but not these in-flight tasks.
                // In-flight iterations will terminate via cancellation token propagation, but callers
                // should be aware that output callbacks may still fire briefly after stop returns.
                _ = ExecuteJobIteration();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation, do not treat as error
            _logger.JobCancelled(JobName);
        }
        catch (Exception ex)
        {
            _logger.JobError(JobName, ex);
        }
    }


    private async Task ExecuteJobIteration()
    {
        try
        {
            _logger.JobIterationStarting(JobName);
            var jobOutput = await _resiliencePipeline.ExecuteAsync(
                async pipeLineCancellation => await PerformJob(_input, pipeLineCancellation).ConfigureAwait(false),
                _forCancelling.Token).ConfigureAwait(false);
            _onJobOutputAvailable.Invoke(jobOutput);
            _logger.JobIterationFinished(JobName);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown, do not log as error
            _logger.JobIterationCancelled(JobName);
        }
        catch (Exception ex)
        {
            OnJobIterationExecutionException(ex);
        }
    }

    /// <summary>
    /// Called when an exception occurs during an iteration execution. Override to customize iteration exception handling.
    /// </summary>
    /// <param name="ex">The exception that was thrown.</param>
    private void OnJobIterationExecutionException(Exception ex)
    {
        _logger.JobIterationExecutionError(JobName, ex.GetType().Name);
        _logger.JobIterationExecutionErrorWithStackTrace(JobName, ex);
    }

    /// <summary>
    /// Stops the periodic execution of the job by cancelling the timer loop.
    /// </summary>
    /// <remarks>
    /// Awaits the timer loop task, but does not await any iterations that were already
    /// fire-and-forgotten before cancellation was requested. Those iterations will terminate
    /// on their own via the cancellation token; see the inline comment in
    /// <see cref="StartPeriodExecutionLoop"/> for details.
    /// </remarks>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopPeriodicExecution()
    {
        _logger.JobStopping(JobName);
        await _forCancelling.CancelAsync().ConfigureAwait(false);
        if (_periodExecutionLoop != null)
        {
            await _periodExecutionLoop.ConfigureAwait(false);
        }

        _logger.JobStopped(JobName);
    }

    /// <summary>
    /// Disposes the periodic job, stopping execution and releasing all resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await StopPeriodicExecution().ConfigureAwait(false);
        if (_timer.IsValueCreated)
        {
            _timer.Value.Dispose();
        }

        _forCancelling.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the job name, computing it on first access and caching it.
    /// </summary>
    private string JobName => _jobName ??= GetJobName(_input);
}
