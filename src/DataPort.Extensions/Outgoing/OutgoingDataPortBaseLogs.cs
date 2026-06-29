using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing;

/// <summary>
/// Uses LoggerMessage source generation for high-performance logging.
/// </summary>
internal static partial class OutgoingDataPortBaseLogs
{
    [LoggerMessage(204, LogLevel.Error, "Validation failed for channel '{Channel}': {ErrorMessage}")]
    internal static partial void ValidationFailed(this ILogger logger, string channel, string? errorMessage);

    [LoggerMessage(205, LogLevel.Warning, "Conversion failed for channel '{Channel}': {Details}")]
    internal static partial void ConversionFailed(this ILogger logger, string channel, string details);

    [LoggerMessage(210, LogLevel.Error,
        "Invalid data point configuration for '{dataPointIdentifier}': {Misconfigurations}")]
    internal static partial void MisConfiguredDataPoint(
        this ILogger logger,
        string dataPointIdentifier,
        string misconfigurations);

    [LoggerMessage(208, LogLevel.Warning, "{Count} value(s) will not be written — no data point configured for channel(s): {Channels}")]
    internal static partial void UnresolvedChannels(this ILogger logger, int count, string channels);

    [LoggerMessage(209, LogLevel.Error, "{FailureCount}/{TotalCount} values are incompatible with the configured data points, nothing will be written")]
    internal static partial void DataPointValuesInvalid(this ILogger logger, int failureCount, int totalCount);
}
