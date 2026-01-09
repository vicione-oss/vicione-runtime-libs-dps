using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public abstract class UnspecificTreeHandlerOutgoing<TValue>
{
    private static readonly PropertyEqualityComparer<INode, Guid> s_nodeComparer = new(n => n.Id);

    protected Dictionary<string, INode[]> ChannelJsonNodeRoutes { get; private set; } = [];
    protected Dictionary<string, INode> ChannelTransferredNodes { get; private set; } = [];
    protected IReadOnlyCollection<Guid> GroupNodes { get; private set; } = [];

    protected void Initialize(IReadOnlyCollection<INode> nodes, Action<INode>? handleTransferNode = null)
        => (ChannelJsonNodeRoutes, ChannelTransferredNodes, GroupNodes) = InitializeUnspecificTree.InitializeOutgoing(nodes, handleTransferNode);

    public async Task Send(IEnumerable<ExternalValue> values, CancellationToken cancellationToken)
    {
        foreach (var groupValues in values.GroupBy(v => ChannelTransferredNodes[v.Channel], s_nodeComparer))
        {
            var node = groupValues.Key;
            var groupedValues = groupValues.ToArray();
            TValue value;

            if (GroupNodes.Contains(node.Id))
            {
                var (data, meta) = NodeValueFactory.CreateJsonStructure(ChannelJsonNodeRoutes, groupedValues);
                value = HandleGroupNode(node, data, meta, groupedValues);
            }
            else
            {
                var externalValue = groupValues.Single();
                value = HandleSingleNode(node, externalValue);
            }

            await SendValue(value, cancellationToken);

            if (value is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public abstract void Initialize();
    protected abstract TValue HandleGroupNode(INode node, JsonObject data, JsonObject meta, IReadOnlyCollection<ExternalValue> values);
    protected abstract TValue HandleSingleNode(INode node, ExternalValue value);
    protected abstract Task SendValue(TValue value, CancellationToken cancellationToken);
}
