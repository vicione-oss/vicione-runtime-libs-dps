using System;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ViciOne.Suite.DataPort.Extensions.Incoming.Polling.Job;

/// <summary>
/// Provides default resilience pipeline configuration for periodic jobs with timeout and circuit breaker policies.
/// Configurations are optimized for jobs with execution intervals ranging from 100ms to hours.
/// </summary>
public static class DefaultResiliencePipeline
{
    private const double TimeoutMultiplier = 1.1; // 110% of execution interval
    private static TimeSpan MinimumBreakDuration => TimeSpan.FromMilliseconds(500);
    private static TimeSpan MaximumTimeout => TimeSpan.FromSeconds(30);
    private static TimeSpan MaximumSamplingDuration => TimeSpan.FromMinutes(5);
    private static TimeSpan MaximumBreakDuration => TimeSpan.FromMinutes(2);

    /// <summary>
    /// Creates the default resilience pipeline combining timeout and circuit breaker strategies.
    /// Timeout is set to 110% of execution interval (max 30 seconds).
    /// Circuit breaker opens after 50% failures over 10 calls within a sampling window (max 5 minutes).
    /// </summary>
    /// <param name="executionInterval">The interval between executions.</param>
    /// <param name="logger">The logger to use for resilience events.</param>
    /// <returns>The resilience pipeline to use for execution.</returns>
    public static ResiliencePipeline<TJobOutput> CreateDefaultResiliencePipeline<TJobOutput>(TimeSpan executionInterval,
        ILogger logger) =>
        new ResiliencePipelineBuilder<TJobOutput>()
            .AddTimeout(CreateTimeoutOptions(executionInterval, logger))
            .AddCircuitBreaker(CreateCircuitBreakerOptions<TJobOutput>(executionInterval, logger))
            .Build();


    /// <summary>
    /// Creates a timeout strategy set to 110% of the execution interval, capped at 30 seconds.
    /// For jobs with intervals &lt;= 27 seconds, timeout scales with the interval.
    /// For jobs with intervals &gt; 27 seconds, timeout is fixed at 30 seconds.
    /// </summary>
    /// <param name="executionInterval">The interval between executions.</param>
    /// <param name="logger">The logger to use for timeout events.</param>
    /// <returns>Configured timeout strategy options.</returns>
    private static TimeoutStrategyOptions CreateTimeoutOptions(TimeSpan executionInterval, ILogger logger)
    {
        var calculatedTimeout = executionInterval * TimeoutMultiplier;
        var timeout = calculatedTimeout < MaximumTimeout ? calculatedTimeout : MaximumTimeout;

        return new TimeoutStrategyOptions
        {
            Timeout = timeout,
            OnTimeout = args =>
            {
                logger.LogExecutionTimeout(args.Timeout.TotalMilliseconds);
                return default;
            }
        };
    }


    /// <summary>
    /// Creates a circuit breaker that opens after 50% failure rate over 10 calls.
    /// Sampling duration: 10× execution interval, capped at 5 minutes.
    /// Break duration: 2× execution interval, between 500ms (min) and 2 minutes (max).
    /// Break durations should not be multiples of execution interval to avoid immediate retries, therefor we add 50ms.
    /// During break, executions are skipped until the circuit attempts recovery.
    /// </summary>
    /// <typeparam name="TJobOutput">The type of output produced by the job.</typeparam>
    /// <param name="executionInterval">The interval between executions.</param>
    /// <param name="logger">The logger to use for circuit breaker state changes.</param>
    /// <returns>Configured circuit breaker strategy options.</returns>
    public static CircuitBreakerStrategyOptions<TJobOutput> CreateCircuitBreakerOptions<TJobOutput>(
        TimeSpan executionInterval, ILogger logger)
    {
        var calculatedSamplingDuration = executionInterval * 10;
        var samplingDuration = calculatedSamplingDuration < MaximumSamplingDuration
            ? calculatedSamplingDuration
            : MaximumSamplingDuration;

        var calculatedBreakDuration = CalculateBreakDuration(executionInterval);

        return new CircuitBreakerStrategyOptions<TJobOutput>
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = samplingDuration,
            BreakDuration = calculatedBreakDuration,
            OnClosed = _ =>
            {
                logger.LogCircuitBreakerState("Closed");
                return default;
            },
            OnOpened = _ =>
            {
                logger.LogCircuitBreakerState("Open");
                return default;
            },
            OnHalfOpened = _ =>
            {
                logger.LogCircuitBreakerState("Half-Open");
                return default;
            }
        };
    }

    /// <summary>
    /// Calculates the circuit breaker break duration with bounds checking and jitter.
    /// </summary>
    /// <param name="executionInterval">The interval between executions.</param>
    /// <returns>The calculated break duration between minimum and maximum bounds.</returns>
    private static TimeSpan CalculateBreakDuration(TimeSpan executionInterval)
    {
        var breakDuration = executionInterval * 2;

        breakDuration = ClampBreakDuration(breakDuration);
        breakDuration = AddJitterIfNeeded(breakDuration, executionInterval);

        return breakDuration;
    }

    /// <summary>
    /// Clamps the break duration between minimum and maximum allowed values.
    /// </summary>
    private static TimeSpan ClampBreakDuration(TimeSpan duration)
    {
        if (duration < MinimumBreakDuration)
            return MinimumBreakDuration;

        return duration > MaximumBreakDuration ? MaximumBreakDuration : duration;
    }

    /// <summary>
    /// Adds a small jitter to the break duration if it's a multiple of the execution interval.
    /// This prevents immediate retries after the break duration expires.
    /// </summary>
    private static TimeSpan AddJitterIfNeeded(TimeSpan breakDuration, TimeSpan executionInterval)
    {
        const int JitterMilliseconds = 50;

        var isMultiple = breakDuration.Ticks % executionInterval.Ticks == 0;

        return isMultiple
            ? breakDuration + TimeSpan.FromMilliseconds(JitterMilliseconds)
            : breakDuration;
    }
}
