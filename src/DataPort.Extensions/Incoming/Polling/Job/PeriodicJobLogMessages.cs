using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Incoming.Polling.Job;

/// <summary>
/// Provides structured log messages for periodic job execution and lifecycle events.
/// </summary>
public static partial class PeriodicJobLogMessages
{
    private static class PeriodicJobEventIds
    {
        // Job lifecycle events: 1000-1099
        public const int JobStartingId = 1000;
        public const int JobStoppingId = 1001;
        public const int JobStoppedId = 1002;
        public const int JobErrorId = 1003;
        public const int JobCancelledId = 1004;

        // Iteration/execution events: 1100-1199
        public const int JobIterationStartingId = 1100;
        public const int JobIterationFinishedId = 1101;
        public const int JobIterationExecutionErrorId = 1102;
        public const int JobIterationExecutionExceptionWithStackTraceId = 1103;
        public const int JobIterationCancelledId = 1104;

        // Resilience events: 1200-1299
        public const int CircuitBreakerStateChangedId = 1200;
        public const int ExecutionTimeoutId = 1201;
    }

    // Job lifecycle events
    /// <summary>
    /// Logs when a periodic job is starting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    [LoggerMessage(PeriodicJobEventIds.JobStartingId, LogLevel.Information, "Periodic job '{JobName}' is starting")]
    public static partial void JobStarting(this ILogger logger, string jobName);

    /// <summary>
    /// Logs when a periodic job is stopping.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    [LoggerMessage(PeriodicJobEventIds.JobStoppingId, LogLevel.Information, "Periodic job '{JobName}' is stopping")]
    public static partial void JobStopping(this ILogger logger, string jobName);

    /// <summary>
    /// Logs when a periodic job has stopped successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    [LoggerMessage(PeriodicJobEventIds.JobStoppedId, LogLevel.Information,
        "Periodic job '{JobName}' has been stopped successfully")]
    public static partial void JobStopped(this ILogger logger, string jobName);

    /// <summary>
    /// Logs when a periodic job was cancelled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    [LoggerMessage(PeriodicJobEventIds.JobCancelledId, LogLevel.Information, "Periodic job '{JobName}' was cancelled")]
    public static partial void JobCancelled(this ILogger logger, string jobName);

    /// <summary>
    /// Logs when a periodic job encounters an error.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(PeriodicJobEventIds.JobErrorId, LogLevel.Error, "Periodic job '{JobName}' encountered an error")]
    public static partial void JobError(this ILogger logger, string jobName, Exception exception);

    // Iteration/execution events
    /// <summary>
    /// Logs when a job iteration is starting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    [LoggerMessage(PeriodicJobEventIds.JobIterationStartingId, LogLevel.Debug,
        "Iteration of job '{JobName}' is starting")]
    public static partial void JobIterationStarting(this ILogger logger, string jobName);

    /// <summary>
    /// Logs when a job iteration finished successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    [LoggerMessage(PeriodicJobEventIds.JobIterationFinishedId, LogLevel.Debug,
        "Iteration of job '{JobName}' finished successfully")]
    public static partial void JobIterationFinished(this ILogger logger, string jobName);

    /// <summary>
    /// Logs when a job iteration was cancelled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    [LoggerMessage(PeriodicJobEventIds.JobIterationCancelledId, LogLevel.Debug,
        "Iteration of job '{JobName}' was cancelled")]
    public static partial void JobIterationCancelled(this ILogger logger, string jobName);

    /// <summary>
    /// Logs when a job iteration encounters a transient error that will be retried.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="exceptionName">The name of the exception type.</param>
    [LoggerMessage(PeriodicJobEventIds.JobIterationExecutionErrorId, LogLevel.Warning,
        "Iteration of job '{JobName}' encountered a transient exception '{ExceptionName}' and will retry on next iteration. Enable debug logs for stack trace")]
    public static partial void JobIterationExecutionError(this ILogger logger, string jobName, string exceptionName);

    /// <summary>
    /// Logs the full exception details including stack trace for a job iteration error.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(PeriodicJobEventIds.JobIterationExecutionExceptionWithStackTraceId, LogLevel.Debug,
        "Iteration of job '{JobName}' encountered an exception")]
    public static partial void JobIterationExecutionErrorWithStackTrace(this ILogger logger, string jobName,
        Exception exception);

    /// <summary>
    /// Logs when the circuit breaker state changes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="circuitState">The current state of the circuit breaker.</param>
    [LoggerMessage(PeriodicJobEventIds.CircuitBreakerStateChangedId, LogLevel.Information,
        "Circuit is now: {CircuitState}")]
    public static partial void LogCircuitBreakerState(this ILogger logger, string circuitState);

    /// <summary>
    /// Logs when a job execution times out.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="actualTimeout">The actual timeout duration in milliseconds.</param>
    [LoggerMessage(PeriodicJobEventIds.ExecutionTimeoutId, LogLevel.Warning,
        "Execution timed out after {actualTimeout} milliseconds")]
    public static partial void LogExecutionTimeout(this ILogger logger, double actualTimeout);
}
