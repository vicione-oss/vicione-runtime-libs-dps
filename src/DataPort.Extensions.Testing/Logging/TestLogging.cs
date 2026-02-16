using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort.Extensions.Testing.Logging;

/// <summary>
/// Provides helper methods for creating loggers and logger factories configured for testing environments.
/// </summary>
public static class TestLogging
{
    /// <summary>
    /// Default minimum log level for all tests. By default, we do not want logs in our test output.
    /// </summary>
    private const LogLevel DefaultMinimumLevel = LogLevel.None;

    /// <summary>
    /// Creates an <see cref="ILoggerFactory"/> configured for console output.
    /// Console output can be automatically routed to xUnit test output via [assembly: CaptureConsole].
    /// See <see href="https://xunit.net/docs/capturing-output#xunitnet-v3"/> for details.
    /// </summary>
    /// <remarks>
    /// The returned factory must be disposed by the caller.
    /// </remarks>
    /// <param name="minimumLevel">
    /// Optional minimum log level. If not specified, <see cref="DefaultMinimumLevel"/> is used.
    /// </param>
    /// <returns>A configured logger factory that must be disposed by the caller.</returns>
    public static ILoggerFactory CreateLoggerFactory(LogLevel minimumLevel = DefaultMinimumLevel) =>
        LoggerFactory.Create(builder => builder
            .AddConsole()
            .SetMinimumLevel(minimumLevel));

    /// <summary>
    /// Creates a logger for type <typeparamref name="T"/> configured for console output.
    /// This is a convenience method that creates a logger factory, creates the logger, and disposes the factory.
    /// Console output can be automatically routed to xUnit test output via [assembly: CaptureConsole].
    /// See <see href="https://xunit.net/docs/capturing-output#xunitnet-v3"/> for details.
    /// </summary>
    /// <typeparam name="T">The type for which to create the logger.</typeparam>
    /// <param name="minimumLevel">
    /// Optional minimum log level. If not specified, <see cref="DefaultMinimumLevel"/> is used.
    /// </param>
    /// <returns>A configured logger instance.</returns>
    public static ILogger<T> CreateLogger<T>(LogLevel minimumLevel = DefaultMinimumLevel)
    {
        using var loggerFactory = CreateLoggerFactory(minimumLevel);
        return loggerFactory.CreateLogger<T>();
    }
}
