using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

internal static partial class DataPointValuesValidatorLogs
{
    [LoggerMessage(210, LogLevel.Warning,
        "Value '{Value}' with dataType '{DataType}' for channel '{Channel}' at {Address} is outside allowed range")]
    internal static partial void ValueOutOfRange(this ILogger logger,
        string channel, string address, object? value, string dataType);
}
