using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort;

internal static partial class HttpServerDataPortLog
{
    [LoggerMessage(0, LogLevel.Error, "Failed to upload entries.")]
    public static partial void LogSendFailed(this ILogger<HttpServerDataPortOutgoing> logger, Exception exception);

    [LoggerMessage(2, LogLevel.Error, "Failed to receive entries.")]
    public static partial void LogReceiveFailed(this ILogger<HttpServerDataPortIncoming> logger, Exception exception);
}
