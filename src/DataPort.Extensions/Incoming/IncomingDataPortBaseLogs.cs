using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extensions.Incoming;

/// <summary>
/// Common logging methods for IncomingDataPortBase operations.
/// Uses generic ILogger to support all implementations.
/// </summary>
internal static partial class IncomingDataPortBaseLogs
{
    [LoggerMessage(103, LogLevel.Information, "Connecting to device {deviceAddress}")]
    internal static partial void ConnectingToDevice(this ILogger logger, DeviceIdentifier deviceAddress);

    [LoggerMessage(104, LogLevel.Error,
        "Invalid data point configuration for '{dataPointIdentifier}': {Misconfigurations}")]
    internal static partial void MisConfiguredDataPoint(
        this ILogger logger,
        string dataPointIdentifier,
        string misconfigurations);

    [LoggerMessage(105, LogLevel.Information, "Disconnecting from device {deviceAddress}")]
    internal static partial void DisconnectingFromDevice(this ILogger logger, DeviceIdentifier deviceAddress);

    [LoggerMessage(107, LogLevel.Debug, "Publishing {externalValuesCount} external values: {externalValuesFormatted}")]
    internal static partial void PublishingExternalValues(this ILogger logger, int externalValuesCount,
        FormattedExternalValues externalValuesFormatted);
}

/// <summary>
/// A wrapper that lazily formats ExternalValue collections only when ToString() is called.
/// This allows conditional logging without explicit if-checks - the formatting only happens
/// when the logger actually needs to write the log message.
/// </summary>
public record FormattedExternalValues(IReadOnlyCollection<ExternalValue> Values)
{
    /// <summary>
    /// Formats the ExternalValue collection into a human-readable string representation.
    /// </summary>
    /// <returns>A formatted string containing all external values with their details.</returns>
    public override string ToString() => string.Join(", ", Values.Select(FormatExternalValue));

    private static string FormatExternalValue(ExternalValue externalValue) =>
        $"[Channel={externalValue.Channel}, Value={externalValue.Value}, Type={externalValue.Value?.GetType().Name ?? "unknown"}, Validity={externalValue.Validity}, Timestamp={externalValue.Timestamp:yyyy-MM-dd HH:mm:ss.fff}]";
}
