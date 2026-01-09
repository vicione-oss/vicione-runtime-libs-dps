using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.DataPort;

internal static partial class HttpClientDataPortOutgoingLog
{
    [LoggerMessage(0, LogLevel.Error, "Failed to send entries to '{Address}'.")]
    public static partial void LogSendFailed(this ILogger<HttpClientDataPortOutgoing> logger, string address, Exception exception);

    [LoggerMessage(1, LogLevel.Trace, "Sending data to '{Address}' with method '{Method}'.")]
    public static partial void LogTraceSendingData(this ILogger<HttpClientDataPortOutgoing> logger, Uri? address, HttpMethod method);

    [LoggerMessage(2, LogLevel.Trace, "'{Address}' responded with '{Status}'.")]
    public static partial void LogResponse(this ILogger<HttpClientDataPortOutgoing> logger, Uri? address, int status);
}
