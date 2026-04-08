using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

internal sealed class HttpTreeHandlerOutgoing(HttpClientDataPortCommunication communication, HttpClient httpClient, ILogger<HttpClientDataPortOutgoing> logger) : UnspecificTreeHandlerOutgoing<HttpRequestMessage>
{
    private readonly HttpClientDataPortCommunication _communication = communication;
    private readonly ILogger<HttpClientDataPortOutgoing> _logger = logger;
    private readonly Uri _baseAddress = new(new HttpClientDataPortProperties(communication).BaseAddress);
    private readonly HttpClient _httpClient = httpClient;
    private readonly Dictionary<Guid, (Uri Address, HttpMethod Method, Dictionary<string, string> Headers)> _addressConfiguration = [];

    public override void Initialize()
    {
        var idNodes = _communication.Nodes.ToDictionary(n => n.Id, n => (INode)n);
        Initialize(_communication.Nodes, node =>
        {
            var nodes = node.GetRoute(idNodes);
            _addressConfiguration.Add(node.Id, (new(_baseAddress, Url.Combine(nodes, n => n.Name)), node.GetHttpMethod(), node.GetHeaders()));
        });
    }

    protected override HttpRequestMessage HandleGroupNode(INode node, JsonObject data, JsonObject meta, IReadOnlyCollection<ExternalValue> values)
    {
        var json = JsonSerializer.Serialize((data, meta).IncludeMeta());
        return CreateRequest(node, json);
    }

    protected override HttpRequestMessage HandleSingleNode(INode node, ExternalValue value)
    {
        var json = JsonSerializer.Serialize(value, JsonSetup.PreserveTypeOptions);
        return CreateRequest(node, json);
    }

    [SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht")]
    private HttpRequestMessage CreateRequest(INode node, string json)
    {
        var (address, httpMethod, headers) = _addressConfiguration[node.Id];
        HttpRequestMessage request = new(httpMethod, address);
        StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        request.Content = content;
        request.AddHeaders(headers);
        return request;
    }

    protected override async Task SendValue(HttpRequestMessage value, CancellationToken cancellationToken)
    {
        _logger.LogTraceSendingData(value.RequestUri, value.Method);

        var response = await _httpClient.SendAsync(value, cancellationToken).ConfigureAwait(false);

        _logger.LogResponse(value.RequestUri, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to send data to '{value.RequestUri}'.");
    }
}
