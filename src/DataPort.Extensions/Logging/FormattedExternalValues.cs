using System.Collections.Generic;
using System.Linq;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort.Extensions.Logging;

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
