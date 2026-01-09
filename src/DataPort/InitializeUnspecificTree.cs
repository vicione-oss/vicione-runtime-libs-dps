using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort;

public static class InitializeUnspecificTree
{
    public static (Dictionary<string, INode[]> ChannelJsonNodes, Dictionary<string, INode> ChannelTransferredNodes, IReadOnlyCollection<Guid> GroupNodes) InitializeOutgoing(IReadOnlyCollection<INode> nodes, Action<INode>? handleTransferNode = null)
    {
        Dictionary<string, INode[]> channelJsonNodes = [];
        Dictionary<string, INode> channelNodes = [];
        List<INode> groupNodes = [];

        var nodesParentCache = nodes.CreateNodeParentsCache();

        foreach (var node in nodes.Where(n => n.TransferredChannels.Count != 0))
        {
            handleTransferNode?.Invoke(node);

            if (node.AffectedChannels.Count == 0)
                groupNodes.Add(node);

            foreach (var channel in node.TransferredChannels)
                channelNodes.Add(channel, node);
        }

        foreach (var node in groupNodes)
        {
            var routes = node.GetSuccessorRoutes(nodesParentCache);
            Dictionary<string, INode[]> channelRoutes = [];

            foreach (var route in routes)
            {
                var affectedChannels = route.Last().AffectedChannels;

                foreach (var channel in affectedChannels)
                    channelRoutes.Add(channel, route);
            }

            foreach (var channel in node.TransferredChannels)
                channelJsonNodes.Add(channel, channelRoutes[channel]);
        }

        return (channelJsonNodes, channelNodes, groupNodes.Select(n => n.Id).ToArray());
    }

    public static (Dictionary<Guid, IReadOnlyCollection<INode[]>> TransferredNodeJsonNodes, Dictionary<string, IReadOnlyCollection<INode>> AddressNodes) InitializeIncoming(IReadOnlyCollection<INode> nodes, Func<IReadOnlyCollection<INode>, string> createAddress)
    {
        Dictionary<string, List<INode>> addressNodes = [];
        List<INode> groupNodes = [];

        var idNodes = nodes.ToDictionary(n => n.Id);
        var nodesParentCache = nodes.CreateNodeParentsCache();

        foreach (var node in nodes.Where(n => n.TransferredChannels.Count != 0))
        {
            var address = createAddress(node.GetRoute(idNodes));

            if (addressNodes.TryGetValue(address, out var list))
                list.Add(node);
            else
                addressNodes.Add(address, [node,]);

            if (node.AffectedChannels.Count == 0)
                groupNodes.Add(node);
        }

        return (groupNodes.ToDictionary(n => n.Id, n => n.GetSuccessorRoutes(nodesParentCache)), addressNodes.ToDictionary(_ => _.Key, _ => (IReadOnlyCollection<INode>)_.Value));
    }
}
