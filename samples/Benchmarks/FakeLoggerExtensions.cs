using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Benchmarks;

internal static class FakeLoggerExtensions
{
    extension<T>(FakeLogger<T> logger)
    {
        internal FakeLogger<T> ConfigureLogLevel(LogLevel logLevel)
        {
            for (var level = LogLevel.Trace; level < logLevel; level++)
                logger.ControlLevel(level, false);
            return logger;
        }
    }
}
