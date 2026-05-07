using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.ManagedEngine.TypeResolution;

namespace ViciOne.Suite.DataPort;

public sealed class HttpServerDataPortIncoming : IExternalIncomingCommunication<HttpServerDataPortCommunication>
{
    private readonly ILogger<HttpServerDataPortIncoming> _logger;
    private readonly IHttpListenerManager _manager;
    private readonly HttpServerDataPortCommunication _communication;
    private readonly Dictionary<Guid, IReadOnlyCollection<INode[]>> _transferredNodeJsonNodes;
    private readonly JsonSerializerOptions _jsonOptions;

    public event Action<IReadOnlyCollection<ExternalValue>>? Received;

    public HttpServerDataPortIncoming(HttpServerDataPortCommunication communication, ILogger<HttpServerDataPortIncoming> logger, AssemblyLoadContext loadContext)
        : this(communication, logger, loadContext, WebServerManager.Instance)
    { }

    internal HttpServerDataPortIncoming(HttpServerDataPortCommunication communication, ILogger<HttpServerDataPortIncoming> logger, AssemblyLoadContext loadContext, IHttpListenerManager manager)
    {
        _manager = manager;
        _logger = logger;
        _jsonOptions = JsonSetup.CreatePreserveTypeOptions(loadContext);
        _communication = communication;

        _manager.RegisterOrCreateWebApp(communication);

        Dictionary<string, IReadOnlyCollection<INode>> addressNodes;

        (_transferredNodeJsonNodes, addressNodes) = InitializeUnspecificTree.InitializeIncoming(communication.Nodes, n => Url.Combine(n.Where(n => n.DesignId == HttpServerNodeDesignId.PathPart), n => n.Name));

        MapNodes(communication, addressNodes);
    }

    private void MapNodes(HttpServerDataPortCommunication communication, Dictionary<string, IReadOnlyCollection<INode>> addressNodes)
    {
        foreach (var (address, transferredNodes) in addressNodes)
        {
            foreach (var node in transferredNodes)
            {
                _manager.MapPost(communication, address, async context =>
                {
                    try
                    {
                        using var reader = new StreamReader(context.Request.InputStream);
                        var jsonNode = JsonNode.Parse(await reader.ReadToEndAsync());

                        List<ExternalValue> values = [];

                        if (_transferredNodeJsonNodes.TryGetValue(node.Id, out var routs))
                            values.AddRange(ProcessJsonNode(jsonNode, node, routs, _jsonOptions));
                        else
                            values.Add(ProcessSimpleValue(node, jsonNode, node.ValueType ?? throw new InvalidOperationException("Value type cannot be null."), node.AffectedChannels, _jsonOptions));

                        Received?.Invoke(values);

                        await Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogReceiveFailed(ex);
                        context.Response.StatusCode = 500;
                    }
                });
            }
        }
    }

    internal static IEnumerable<ExternalValue> ProcessJsonNode(JsonNode? jsonMessage, INode transferredNode, IReadOnlyCollection<INode[]> routes, JsonSerializerOptions jsonOptions)
    {
        foreach (var route in routes)
        {
            var node = NodeValueFactory.GetValueJsonNode(route.Where(n => n.DesignId != HttpServerNodeDesignId.Path).ToArray(), jsonMessage);
            var typeName = node?["Type"]?.GetValue<string>();
            Type? type;

            if (typeName == null)
                type = route[^1].ValueType;
            else
                type = TypeResolver.GetType(typeName);

            yield return ProcessSimpleValue(transferredNode, node?["Value"] ?? node?[route[^1].Name], type ?? throw new InvalidOperationException($"Cannot find type of value ('{string.Join('.', route.Select(e => e.Name))}')."), route[^1].AffectedChannels, jsonOptions);
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

    public async Task ConnectAsync(CancellationToken cancellationToken)
        => await _manager.StartApp(_communication);

    public async Task DisconnectAsync(CancellationToken cancellationToken)
        => await _manager.CloseConnection(_communication);
}
