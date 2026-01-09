using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Logging;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.")]
public record TestLogEntry(LogLevel LogLevel, string Message, Exception? Exception = default, EventId EventId = default);
