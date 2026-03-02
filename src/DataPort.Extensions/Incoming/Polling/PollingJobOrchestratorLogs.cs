using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Incoming.Polling;

/// <summary>
/// Logging methods for PollingJobOrchestrator operations.
/// </summary>
internal static partial class PollingJobOrchestratorLogs
{
    [LoggerMessage(101, LogLevel.Debug, "Creating {dataPointGroupsCount} polling jobs")]
    internal static partial void CreatingPollingJobs(this ILogger logger, int dataPointGroupsCount);

    [LoggerMessage(102, LogLevel.Debug, "Successfully created and started {pollingJobsCount} polling jobs")]
    internal static partial void PollingJobsCreatedAndStarted(this ILogger logger, int pollingJobsCount);

    [LoggerMessage(105, LogLevel.Debug, "Stopping {pollingJobsCount} polling jobs")]
    internal static partial void StoppingPollingJobs(this ILogger logger, int pollingJobsCount);

    [LoggerMessage(106, LogLevel.Debug, "Successfully stopped all polling jobs")]
    internal static partial void PollingJobsStopped(this ILogger logger);
}
