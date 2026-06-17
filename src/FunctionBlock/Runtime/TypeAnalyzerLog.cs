using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.System.Runtime;

internal static partial class TypeAnalyzerLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "Adding TypeInfo for enum '{InputType}' at path: {Path}.")]
    public static partial void AddingEnumTypeInfo(this ILogger logger, Type inputType, string Path);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Adding TypeInfo for simple type '{InputType}' at path: {Path}.")]
    public static partial void AddingSimpleTypeInfo(this ILogger logger, Type inputType, string Path);
}
