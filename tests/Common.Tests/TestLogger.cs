using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Logging;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.")]
public sealed class TestLogger<T>(LogLevel logLevel = LogLevel.Trace) : ILogger<T>
{
    private readonly LogLevel _logLevel = logLevel;

    public int Calls => Entries.Count;
    public LogLevel LogLevel { get; private set; }
    public EventId EventId { get; private set; }
    public Exception? Exception { get; private set; }
    public string? Message { get; private set; }
    public List<TestLogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _logLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
            Message = formatter(state, exception);
            Entries.Add(new(logLevel, Message));
        }
    }
}
