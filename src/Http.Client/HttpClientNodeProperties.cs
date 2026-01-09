using System;
using System.Collections.Generic;
using System.Net.Http;

namespace ViciOne.Suite.DataPort;

internal static class HttpClientNodeProperties
{
    internal const string HeadersProperty = "Headers";
    internal const string HttpMethodProperty = "HttpMethod";

    internal static Dictionary<string, string> GetHeaders(this INode node)
    {
        if (node.TryGetProperty(HeadersProperty, out var property))
            return property.GetValue<Dictionary<string, string>>();
        return [];
    }

    internal static HttpMethod GetHttpMethod(this INode node)
        => Convert(node.GetPropertyValueOrDefault<sbyte>(HttpMethodProperty, 0));

    private static HttpMethod Convert(sbyte value)
        => value switch
        {
            0 => HttpMethod.Get,
            1 => HttpMethod.Post,
            2 => HttpMethod.Patch,
            3 => HttpMethod.Put,
            4 => HttpMethod.Delete,
            _ => throw new NotSupportedException($"The HTTP method '{value}' is not supported.")
        };
}
