using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

internal sealed class HttpServerTreeHandlerOutgoing : UnspecificTreeHandlerOutgoing<ToSendData>
{
    private readonly HttpServerDataPortCommunication _communication;
    private readonly ILogger<HttpServerDataPortOutgoing> _logger;
    private readonly Dictionary<Guid, HttpCacheMessage> _addressConfiguration;
    private readonly IHttpListenerManager _manager;

    public HttpServerTreeHandlerOutgoing(HttpServerDataPortCommunication communication, IHttpListenerManager manager, ILogger<HttpServerDataPortOutgoing> logger)
    {
        _communication = communication;
        _manager = manager;
        _logger = logger;
        _addressConfiguration = [];

        Initialize();
    }

    public override void Initialize()
    {
        var idNodes = _communication.Nodes.ToDictionary(n => n.Id, n => (INode)n);

        Initialize(_communication.Nodes, handleTransferNode: node =>
        {
            var route = node.GetRoute(idNodes);
            var endpoint = Url.Combine(route.Where(n => n.DesignId == HttpServerNodeDesignId.PathPart), n => n.Name);
            var endpointNode = route.First(n => n.DesignId == HttpServerNodeDesignId.HttpGetEndpoint);

            if (endpointNode.GetPropertyValueOrDefault("SendLastValue", true))
                _addressConfiguration.Add(node.Id, new HttpCacheMessage(1));
            else
                _addressConfiguration.Add(node.Id, new HttpCacheMessage(endpointNode.GetPropertyValueOrDefault("QueueSize", 1)));

            _manager.MapGet(_communication, endpoint, async context =>
            {
                try
                {
                    var dataToSend = _addressConfiguration[node.Id].GetData();

                    string json;

                    if (dataToSend.Count == 1)
                        json = JsonSerializer.Serialize(dataToSend.First(), JsonSetup.PreserveTypeOptions);
                    else
                        json = JsonSerializer.Serialize(dataToSend, JsonSetup.PreserveTypeOptions);

                    var data = Encoding.UTF8.GetBytes(json);

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    context.Response.ContentEncoding = Encoding.UTF8;
                    await context.Response.OutputStream.WriteAsync(data);
                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogSendFailed(ex);

                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Close();
                }
            });
        });
    }

    protected override ToSendData HandleGroupNode(INode node, JsonObject data, JsonObject meta, IReadOnlyCollection<ExternalValue> values)
    {
        var json = JsonSerializer.Serialize((data, meta).IncludeMeta());
        return CreateMessage(node, json);
    }

    protected override ToSendData HandleSingleNode(INode node, ExternalValue value)
    {
        var json = JsonSerializer.Serialize(value, JsonSetup.PreserveTypeOptions);
        return CreateMessage(node, json);
    }

    private static ToSendData CreateMessage(INode node, string json) => new(node.Id, json);

    protected override Task SendValue(ToSendData value, CancellationToken cancellationToken)
    {
        try
        {
            _addressConfiguration[value.NodeId].AddData(value.Value);
        }
        catch (Exception ex)
        {
            _logger.LogSendFailed(ex);
        }

        return Task.CompletedTask;
    }
}
