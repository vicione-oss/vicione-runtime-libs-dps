using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Authentication;

namespace ViciOne.Suite.DataPort;

internal sealed class WebServerManager : IDisposable, IHttpListenerManager
{
    private readonly Dictionary<string, AppEntry> _webApps = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, Dictionary<(string Path, string Method), Func<IHttpContext, Task>>> _routes = [];

    public static readonly WebServerManager Instance = new();

    private WebServerManager() { }

    public async Task RegisterOrCreateWebApp(HttpServerDataPortCommunication communication)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_webApps.TryGetValue(communication.BaseAddress, out var existing))
                existing.UserCount++;
            else
                CreateApp(communication);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CreateApp(HttpServerDataPortCommunication communication)
    {
        var certificate = GetCertificate(communication);

#pragma warning disable CA2000 // Dispose objects before losing scope
        var server = new WebServer(o =>
        {
            o.WithUrlPrefix(communication.BaseAddress).WithMode(HttpListenerMode.EmbedIO);
            if (communication.BaseAddress.StartsWith("https", StringComparison.Ordinal) && certificate is not null)
                o.WithCertificate(certificate);
        });
#pragma warning restore CA2000 // Dispose objects before losing scope

        var entry = new AppEntry(server)
        {
            UserCount = 1,
            StartRequested = 0,
            IsRunning = false,
        };

        _webApps.TryAdd(communication.BaseAddress, entry);
    }

    private static X509Certificate2? GetCertificate(HttpServerDataPortCommunication communication)
    {
        X509Certificate2? certificate = null;
        if (!string.IsNullOrEmpty(communication.CertificateFile))
        {
            if (!string.IsNullOrEmpty(communication.CertificateFilePassword))
                certificate = Certificate.FromPFX(communication.CertificateFile, communication.CertificateFilePassword);
            else if (!string.IsNullOrWhiteSpace(communication.CertificatePrivateKeyFile))
                certificate = Certificate.FromPEM(communication.CertificateFile, communication.CertificatePrivateKeyFile);
        }

        return certificate;
    }

    public async Task StartApp(HttpServerDataPortCommunication communication)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_webApps.TryGetValue(communication.BaseAddress, out var entry) && !entry.IsRunning)
            {
                entry.StartRequested++;

                if (entry.UserCount == entry.StartRequested)
                {
                    ConfigureServerModules(communication, entry.Server);

                    _ = entry.Server.RunAsync();
                    entry.IsRunning = true;
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void ConfigureServerModules(HttpServerDataPortCommunication communication, WebServer server)
    {
        var properties = new HttpServerDataPortProperties(communication);

        if (properties.AuthenticationType == AuthenticationType.Basic)
        {
            var authentication = new BasicAuthenticationModule("/", "Access");
            authentication.Accounts.AddOrUpdate(communication.Username, communication.Password, (value, newValue) => newValue);
            server.WithModule(authentication);
        }
        if (properties.AuthenticationType == AuthenticationType.Bearer)
        {
            server.WithModule(new BearerAuthenticationModule(properties.BearerToken));
        }

        if (_routes.TryGetValue(communication.BaseAddress, out var routeMap))
        {
            server.WithModule(new ActionModule("/", HttpVerbs.Any, ctx =>
            {
                if (routeMap.TryGetValue((ctx.Route.Path, ctx.Request.HttpMethod), out var handler))
                {
                    return handler(ctx);
                }
                else
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;

                    return ctx.SendStringAsync("Not Found", "text/plain", Encoding.UTF8);
                }
            }));
        }

        server.HandleHttpException(async (ctx, ex) =>
        {
            ctx.Response.StatusCode = ex.StatusCode;
            await ctx.SendStringAsync(ex.Message ?? "Exception", "text/plain", Encoding.UTF8);
        });
    }

    public async Task MapGet(HttpServerDataPortCommunication communication, string path, Func<IHttpContext, Task> handler)
        => await AddRoute(communication, path, "GET", handler);

    public async Task MapPost(HttpServerDataPortCommunication communication, string path, Func<IHttpContext, Task> handler)
        => await AddRoute(communication, path, "POST", handler);

    private async Task AddRoute(HttpServerDataPortCommunication communication, string path, string method, Func<IHttpContext, Task> handler)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (!_routes.TryGetValue(communication.BaseAddress, out var routeMap))
            {
                routeMap = [];
                _routes.TryAdd(communication.BaseAddress, routeMap);
            }

            var key = ("/" + path.TrimStart('/'), method.ToUpperInvariant());
            routeMap[key] = handler;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CloseConnection(HttpServerDataPortCommunication communication)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_webApps.TryGetValue(communication.BaseAddress, out var entry))
            {
                entry.UserCount--;

                if (entry.UserCount <= 0)
                {
                    entry.Server.Dispose();
                    _webApps.Remove(communication.BaseAddress);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Wait();
        try
        {
            foreach (var kv in _webApps)
                kv.Value.Server.Dispose();

            _webApps.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
        _semaphore.Dispose();
    }
}
