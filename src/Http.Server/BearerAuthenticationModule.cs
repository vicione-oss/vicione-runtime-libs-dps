using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;

namespace ViciOne.Suite.DataPort;

internal sealed class BearerAuthenticationModule(string token, IEnumerable<string>? excludedPaths = null) : WebModuleBase("/")
{
    private readonly string _token = token;
    private readonly HashSet<string> _excludedPaths = excludedPaths != null
            ? new HashSet<string>(excludedPaths, StringComparer.OrdinalIgnoreCase)
            : [];

    public override bool IsFinalHandler => false;

    protected override Task OnRequestAsync(IHttpContext context)
    {
        var path = context.RequestedPath;

        if (_excludedPaths.Contains(path))
            return Task.CompletedTask;

        var authHeader = context.Request.Headers["Authorization"];

        if (!string.IsNullOrEmpty(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();

            if (FixedTimeEquals(token, _token))
                return Task.CompletedTask;
        }

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.Headers["WWW-Authenticate"] = "Bearer";
        return context.SendStringAsync("Unauthorized", "text/plain", Encoding.UTF8);
    }

    internal static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
