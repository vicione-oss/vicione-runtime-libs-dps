using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort;

internal static partial class ManagedQueuePipelineLog
{
    [LoggerMessage(0, LogLevel.Error, "Queue processing failed.")]
    internal static partial void LogQueueProcessingFailed(this ILogger logger, Exception exception);
}
