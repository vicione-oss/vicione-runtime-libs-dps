using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

internal static partial class DataPointValuesValidatorLogs
{
    [LoggerMessage(202, LogLevel.Warning,
        "Value for channel '{Channel}' at {Address} has wrong data-type '{ActualType}'. Only {allowedTypes} are allowed.")]
    internal static partial void ValueHasWrongType(this ILogger logger,
        string channel, string address, string allowedTypes, string actualType);

    [LoggerMessage(210, LogLevel.Warning,
        "Value '{Value}' with dataType '{DataType}' for channel '{Channel}' at {Address} is outside allowed range")]
    internal static partial void ValueOutOfRange(this ILogger logger,
        string channel, string address, object? value, string dataType);
}
