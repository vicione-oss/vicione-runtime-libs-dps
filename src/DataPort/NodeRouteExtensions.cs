using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort;

public static class NodeRouteExtensions
{
    private static readonly ArrayPool<INode> s_arrayPoolNode = ArrayPool<INode>.Create();

    public static IReadOnlyCollection<INode> GetRoute(this INode node, Dictionary<Guid, INode> nodes)
    {
        List<INode> route = [];
        GetRoute(node, null, nodes, route);
        route.Reverse();
        return route;
    }

    public static IReadOnlyCollection<INode> GetRoute(this INode node, INode parent, Dictionary<Guid, INode> nodes)
    {
        List<INode> route = [];
        GetRoute(node, parent, nodes, route);
        route.Reverse();
        return route;
    }

    private static void GetRoute(INode node, INode? parent, Dictionary<Guid, INode> nodes, List<INode> route)
    {
        if (node == parent)
            return;

        route.Add(node);

        if (node.ParentId is not null)
            GetRoute(nodes[node.ParentId.Value], parent, nodes, route);
    }

    public static IReadOnlyCollection<IReadOnlyCollection<INode>> GetRoutes(this IReadOnlyCollection<INode> nodes)
    {
        List<IReadOnlyCollection<INode>> routes = [];
        var optimizedNodes = nodes.CreateNodeParentsCache();

        foreach (var node in nodes.Where(n => n.ParentId is null))
        {
            var currentRoute = s_arrayPoolNode.Rent(1);

            try
            {
                currentRoute[0] = node;
                routes.AddRange(GetSuccessorRoutes(node, optimizedNodes, currentRoute, 1));
            }
            finally
            {
                s_arrayPoolNode.Return(currentRoute);
            }
        }

        return routes;
    }

    public static Dictionary<Guid, INode[]> CreateNodeParentsCache(this IReadOnlyCollection<INode> nodes)
        => nodes.GroupBy(n => n.ParentId).Where(g => g.Key is not null).ToDictionary(g => g.Key!.Value, g => g.ToArray());

    public static IReadOnlyCollection<INode[]> GetSuccessorRoutes(this INode node, Dictionary<Guid, INode[]> nodeParentsCache)
        => GetSuccessorRoutes(node, nodeParentsCache, [], 0);

    private static IReadOnlyCollection<INode[]> GetSuccessorRoutes(this INode node, Dictionary<Guid, INode[]> nodes, INode[] currentRoute, int currentRouteLength)
    {
        if (!nodes.TryGetValue(node.Id, out var successors))
            return currentRouteLength == 0 ? [] : new INode[][] { [.. currentRoute[0..currentRouteLength]] };

        List<INode[]> routes = [];

        foreach (var successor in successors)
        {
            var route = s_arrayPoolNode.Rent(currentRouteLength + 1);

            try
            {
                currentRoute.CopyTo(route, 0);
                route[currentRouteLength] = successor;
                routes.AddRange(GetSuccessorRoutes(successor, nodes, route, currentRouteLength + 1));
            }
            finally
            {
                s_arrayPoolNode.Return(route);
            }
        }

        return routes;
    }
}
