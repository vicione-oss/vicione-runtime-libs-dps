using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort;

internal static partial class HttpClientDataPortIncomingLog
{
    [LoggerMessage(0, LogLevel.Error, "Failed to send entries to '{Address}'.")]
    public static partial void LogSendFailed(this ILogger<HttpClientDataPortIncoming> logger, string address, Exception exception);
}
