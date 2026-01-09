using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public sealed class HttpClientDataPortOutgoing : IExternalOutgoingCommunication<HttpClientDataPortCommunication>, IDisposable
{
    private readonly ILogger<HttpClientDataPortOutgoing> _logger;
    private readonly Uri _baseAddress;
    private readonly HttpClient _httpClient;
    private readonly HttpTreeHandlerOutgoing _treeHandlerOutgoing;

    public HttpClientDataPortOutgoing(HttpClientDataPortCommunication communication, ILogger<HttpClientDataPortOutgoing> logger) : this(communication, logger, CreateHttpClient)
    { }

    internal HttpClientDataPortOutgoing(HttpClientDataPortCommunication communication, ILogger<HttpClientDataPortOutgoing> logger, Func<HttpClient> createHttpClient)
    {
        HttpClientDataPortProperties properties = new(communication);
        _baseAddress = new(properties.BaseAddress);
        _httpClient = createHttpClient();
        _logger = logger;
        _treeHandlerOutgoing = new(communication, _httpClient, logger);

        ApplyAuthHeader(communication, properties);

        _treeHandlerOutgoing.Initialize();
    }

    private void ApplyAuthHeader(HttpClientDataPortCommunication communication, HttpClientDataPortProperties properties)
    {
        if (properties.AuthenticationType == AuthenticationType.Bearer)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationType.Bearer.ToString(), communication.BearerToken);
        }
        else if (properties.AuthenticationType == AuthenticationType.Basic)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                AuthenticationType.Basic.ToString(),
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{communication.Username}:{communication.Password}")));
        }
    }

    [SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht")]
    private static HttpClient CreateHttpClient()
        => new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

    public Task ConnectAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public void Dispose()
        => _httpClient.Dispose();

    public async Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values, CancellationToken cancellationToken)
    {
        try
        {
            await _treeHandlerOutgoing.Send(values, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogSendFailed(_baseAddress.OriginalString, ex);
        }
    }
}
