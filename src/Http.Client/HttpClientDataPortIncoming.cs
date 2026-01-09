using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.ManagedEngine.TypeResolution;

namespace ViciOne.Suite.DataPort;

public sealed class HttpClientDataPortIncoming : IExternalIncomingCommunication<HttpClientDataPortCommunication>, IDisposable
{
    private readonly TimeSpan _pollInterval;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<HttpClientDataPortIncoming> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<Guid, IReadOnlyCollection<INode[]>> _transferredNodeJsonNodes;
    private readonly List<Func<CancellationToken, Task>> _tasks = [];
    private readonly HttpClient _httpClient;

    private CancellationTokenSource? _cancellationTokenSource;
    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private readonly Uri _baseAddress;

    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    public HttpClientDataPortIncoming(HttpClientDataPortCommunication communication, ILogger<HttpClientDataPortIncoming> logger, AssemblyLoadContext loadContext) : this(communication, logger, loadContext, CreateHttpClient, TimeProvider.System)
    { }

    internal HttpClientDataPortIncoming(HttpClientDataPortCommunication communication, ILogger<HttpClientDataPortIncoming> logger, AssemblyLoadContext loadContext, Func<HttpClient> createHttpClient, TimeProvider timeProvider)
    {
        HttpClientDataPortProperties properties = new(communication);
        _pollInterval = properties.PollInterval ?? throw new InvalidOperationException("Poll interval cannot be null.");
        _baseAddress = new(properties.BaseAddress);
        _httpClient = createHttpClient();
        _logger = logger;
        _timeProvider = timeProvider;
        _jsonOptions = JsonSetup.CreatePreserveTypeOptions(loadContext);

        ApplyAuthHeader(communication, properties);

        Dictionary<string, IReadOnlyCollection<INode>> addressNodes;

        (_transferredNodeJsonNodes, addressNodes) = InitializeUnspecificTree.InitializeIncoming(communication.Nodes, static n => Url.Combine(n, n => n.Name));

        foreach (var (address, transferredNodes) in addressNodes)
        {
            foreach (var node in transferredNodes)
                _tasks.Add(c => ReceiveAsync(new(_baseAddress, address), node.GetHeaders(), node.GetHttpMethod(), node, c));
        }
    }

    private void ApplyAuthHeader(HttpClientDataPortCommunication communication, HttpClientDataPortProperties properties)
    {
        if (properties.AuthenticationType == AuthenticationType.Bearer)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", communication.BearerToken);
        }
        else if (properties.AuthenticationType == AuthenticationType.Basic)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
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

    private async Task ProcessTimerAsync()
    {
        if (_cancellationTokenSource is null)
            throw new InvalidOperationException("Cancellation token source must be initalized.");
        if (_timer is null)
            throw new InvalidOperationException("Timer must be initalized.");

        while (await _timer.WaitForNextTickAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
            await Task.WhenAll(_tasks.Select(t => t.Invoke(_cancellationTokenSource.Token))).ConfigureAwait(false);
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new();
        _timer = new(_pollInterval, _timeProvider);
        _timerTask = Task.Run(ProcessTimerAsync, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is not null)
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _timer?.Dispose();

        if (_timerTask is not null)
        {
            try
            {
                await _timerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        _cancellationTokenSource?.Dispose();

        _timer = null;
        _timerTask = null;
        _cancellationTokenSource = null;
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _timer?.Dispose();
        _httpClient.Dispose();
    }

    private async Task ReceiveAsync(Uri address, Dictionary<string, string> headers, HttpMethod httpMethod, INode node, CancellationToken cancellationToken)
    {
        try
        {
            var jsonMessage = await GetDataAsync().ConfigureAwait(false);
            Process(node, jsonMessage);
        }
        catch (Exception ex)
        {
            _logger.LogSendFailed(address.OriginalString, ex);
        }

        async Task<JsonNode?> GetDataAsync()
        {
            using var request = new HttpRequestMessage(httpMethod, address).AddHeaders(headers);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        }

        void Process(INode node, JsonNode? jsonMessage)
        {
            List<ExternalValue> values = [];

            if (_transferredNodeJsonNodes.TryGetValue(node.Id, out var routes))
                values.AddRange(ProcessJsonNode(jsonMessage, node, routes, _jsonOptions));
            else
                values.Add(ProcessSimpleValue(node, jsonMessage, node.ValueType ?? throw new InvalidOperationException("Value type cannot be null."), node.AffectedChannels, _jsonOptions));

            Received?.Invoke(values);
        }
    }

    internal static IEnumerable<ExternalValue> ProcessJsonNode(JsonNode? jsonMessage, INode transferredNode, IReadOnlyCollection<INode[]> routes, JsonSerializerOptions jsonOptions)
    {
        foreach (var route in routes)
        {
            var node = NodeValueFactory.GetValueJsonNode(route, jsonMessage);
            var typeName = node?["Type"]?.GetValue<string>() ?? throw new InvalidOperationException($"Cannot find type of value ('{string.Join('.', route.Select(e => e.Name))}').");
            yield return ProcessSimpleValue(transferredNode, node?["Value"], TypeResolver.GetType(typeName), route[^1].AffectedChannels, jsonOptions);
        }
    }

    private static ExternalValue ProcessSimpleValue(INode transferredNode, JsonNode? node, Type nodeType, IReadOnlyCollection<string> affectedChannels, JsonSerializerOptions jsonOptions)
    {
        var value = node?.Deserialize(nodeType, jsonOptions);
        var channel = affectedChannels.First(transferredNode.TransferredChannels.Contains);

        return new()
        {
            Channel = channel,
            Value = value,
            Timestamp = DateTime.UtcNow,
            Validity = 1,
        };
    }
}
