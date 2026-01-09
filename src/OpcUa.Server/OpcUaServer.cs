using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace ViciOne.Suite.DataPort;

internal sealed class OpcUaServer(OpcUaServerDataPortCommunication communication, ILogger<IOpcUaServer> logger) : StandardServer, IOpcUaServer
{
    private readonly OpcUaServerDataPortProperties _properties = new(communication);
    private readonly Dictionary<Guid, Node> _nodes = [];
    private DataPortNodeManager? _nodeManager;

    public event Action<string, DateTime, object> ReceiveValue
    {
        add
        {
            if (_nodeManager is null)
                throw new InvalidOperationException("Node manager is not initialized.");

            _nodeManager.ReceiveValue += value;
        }

        remove
        {
            if (_nodeManager is null)
                throw new InvalidOperationException("Node manager is not initialized.");

            _nodeManager.ReceiveValue -= value;
        }
    }

    public void AddNodes(IReadOnlyCollection<Node> nodes)
    {
        if (_nodeManager is not null)
            throw new InvalidOperationException("Node manager is already initialized.");

        foreach (var node in nodes)
        {
            if (_nodes.TryGetValue(node.Id, out var existingNode))
            {
                existingNode.TransferredChannels.AddRange(node.TransferredChannels);
                existingNode.AffectedChannels.AddRange(node.AffectedChannels);
                continue;
            }
            _nodes.Add(node.Id, node);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configuration = OpcUaSetup.CreateConfiguration(_properties);

        Utils.SetLogger(logger);
        await ValidateConfigAsync(configuration, cancellationToken);
        Start(configuration);

        if (_nodeManager is null)
            throw new InvalidOperationException("Server did not initialize correctly.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Utils.Logger == logger)
            Utils.SetLogger(new TraceEventLogger());

        Stop();

        return Task.CompletedTask;
    }

    public async Task PublishValueAsync(string channel, object? value, DateTime timestamp, CancellationToken cancellationToken)
    {
        if (_nodeManager is null)
            throw new InvalidOperationException("Node manager is not initialized.");

        var node = _nodeManager.GetNodeState(channel);

        if (!await _nodeManager.WriteVariableValueAsync(node, value, timestamp, false))
            throw new InvalidOperationException($"Failed to send '{value}' to node with id '{node.NodeId.Identifier}'.");
    }

    public async Task SetNodeStatusAsync(string channel, object? value, CancellationToken cancellationToken)
    {
        if (_nodeManager is null)
            throw new InvalidOperationException("Node manager is not initialized.");

        var node = _nodeManager.GetNodeState(channel);

        if (!await _nodeManager.UpdateVariableStateAsync(node, OpcUaStatusCodes.ConvertToStatusCode(value, logger)))
            throw new InvalidOperationException($"Failed to set status '{value}' to node with id '{node.NodeId.Identifier}'.");
    }

    protected override SessionManager CreateSessionManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var manager = base.CreateSessionManager(server, configuration);

        if (string.IsNullOrEmpty(_properties.User) && string.IsNullOrEmpty(_properties.Password))
            return manager;

        manager.ImpersonateUser += ImpersonateUser;

        return manager;
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        _nodeManager = new(server, configuration, _nodes.Values.GetRoutes(), _properties.Namespace);
        return new MasterNodeManager(server, configuration, null, _nodeManager);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _nodeManager?.Dispose();
            _nodeManager = null;
        }
    }

    /// <summary>
    /// Parts of the method are copied from the reference implementation of the OPC UA server. <see cref="https://github.com/OPCFoundation/UA-.NETStandard/blob/master/Applications/Quickstarts.Servers/ReferenceServer/ReferenceServer.cs"/>
    /// </summary>
    private void ImpersonateUser(Session session, ImpersonateEventArgs args)
        => args.Identity = args.NewIdentity switch
        {
            UserNameIdentityToken userNameToken => VerifyPassword(userNameToken),
            AnonymousIdentityToken => new RoleBasedIdentity(new UserIdentity(), [Role.Anonymous]),
            _ => throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid, "Not supported user token type: {0}.", args.NewIdentity),
        };

    /// <summary>
    /// Parts of the method are copied from the reference implementation of the OPC UA server. <see cref="https://github.com/OPCFoundation/UA-.NETStandard/blob/master/Applications/Quickstarts.Servers/ReferenceServer/ReferenceServer.cs"/>
    /// </summary>
    private RoleBasedIdentity VerifyPassword(UserNameIdentityToken userNameToken)
    {
        var username = userNameToken.UserName;

        if (_properties.User != username || _properties.Password != userNameToken.DecryptedPassword)
        {
            // construct translation object with default text.
            var info = new TranslationInfo(
                "InvalidPassword",
                "en-US",
                "Invalid username or password.",
                username);

            // create an exception with a vendor defined sub-code.
            throw new ServiceResultException(new ServiceResult(
                StatusCodes.BadUserAccessDenied,
                "InvalidPassword",
                LoadServerProperties().ProductUri,
                new LocalizedText(info)));
        }

        return new RoleBasedIdentity(new UserIdentity(userNameToken),
               [Role.AuthenticatedUser]);
    }

    private static async Task ValidateConfigAsync(ApplicationConfiguration configuration, CancellationToken cancellationToken)
    {
        await configuration.Validate(ApplicationType.Server).ConfigureAwait(false);

        ApplicationInstance applicationInstance = new(configuration);

        if (!await applicationInstance.CheckApplicationInstanceCertificates(false, CertificateFactory.DefaultLifeTime, cancellationToken))
            throw new InvalidOperationException("Server certificate is not valid.");
    }
}
