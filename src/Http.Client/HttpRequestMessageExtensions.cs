using System.Collections.Generic;
using System.Net.Http;

namespace ViciOne.Suite.DataPort;

internal static class HttpRequestMessageExtensions
{
    internal static HttpRequestMessage AddHeaders(this HttpRequestMessage message, IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var header in headers)
            message.Headers.Add(header.Key, header.Value);
        return message;
    }
}
