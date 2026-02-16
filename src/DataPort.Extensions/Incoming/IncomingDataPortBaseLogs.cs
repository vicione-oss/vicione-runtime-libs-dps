using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Incoming;

/// <summary>
/// Common logging methods for IncomingDataPortBase operations.
/// Uses generic ILogger to support all implementations.
/// </summary>
internal static partial class IncomingDataPortBaseLogs
{
    [LoggerMessage(101, LogLevel.Debug, "Creating {dataPointGroupsCount} polling jobs")]
    internal static partial void CreatingPollingJobs(this ILogger logger, int dataPointGroupsCount);

    [LoggerMessage(102, LogLevel.Debug, "Successfully created and started {pollingJobsCount} polling jobs")]
    internal static partial void PollingJobsCreatedAndStarted(this ILogger logger, int pollingJobsCount);

    [LoggerMessage(103, LogLevel.Information, "Connecting to device {deviceAddress}")]
    internal static partial void ConnectingToDevice(this ILogger logger, string deviceAddress);

    [LoggerMessage(104, LogLevel.Information, "Disconnecting from device {deviceAddress}")]
    internal static partial void DisconnectingFromDevice(this ILogger logger, string deviceAddress);

    [LoggerMessage(105, LogLevel.Debug, "Stopping {pollingJobsCount} polling jobs")]
    internal static partial void StoppingPollingJobs(this ILogger logger, int pollingJobsCount);

    [LoggerMessage(106, LogLevel.Debug, "Successfully stopped all polling jobs")]
    internal static partial void PollingJobsStopped(this ILogger logger);

    [LoggerMessage(107, LogLevel.Debug, "Publishing {externalValuesCount} external values: {externalValuesFormatted}")]
    internal static partial void PublishingExternalValues(this ILogger logger, int externalValuesCount,
        FormattedExternalValues externalValuesFormatted);
}
