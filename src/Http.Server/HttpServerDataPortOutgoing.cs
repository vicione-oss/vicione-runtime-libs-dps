using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public sealed class HttpServerDataPortOutgoing : IExternalOutgoingCommunication<HttpServerDataPortCommunication>
{
    private readonly ILogger<HttpServerDataPortOutgoing> _logger;
    private readonly HttpServerTreeHandlerOutgoing _handler;
    private readonly HttpServerDataPortCommunication _communication;
    private readonly IHttpListenerManager _manager;

    public HttpServerDataPortOutgoing(HttpServerDataPortCommunication communication, ILogger<HttpServerDataPortOutgoing> logger)
        : this(communication, logger, WebServerManager.Instance)
    { }

    internal HttpServerDataPortOutgoing(HttpServerDataPortCommunication communication, ILogger<HttpServerDataPortOutgoing> logger, IHttpListenerManager manager)
    {
        _manager = manager;
        _logger = logger;
        _communication = communication;

        _manager.RegisterOrCreateWebApp(communication);

        _handler = new(communication, _manager, _logger);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
        => await _manager.StartApp(_communication);

    public async Task DisconnectAsync(CancellationToken cancellationToken)
        => await _manager.CloseConnection(_communication);

    public async Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values, CancellationToken cancellationToken)
        => await _handler.Send(values, cancellationToken);
}
