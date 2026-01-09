using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort;

public static class LinkedNodeFactory
{
    public static List<LinkedNode> Create(IReadOnlyCollection<Node> nodes)
        => [.. Create(nodes, null)];

    private static IEnumerable<LinkedNode> Create(IReadOnlyCollection<Node> nodes, LinkedNode? parent)
    {
        var roots = nodes.Where(n => n.ParentId == parent?.Id);

        foreach (var root in roots)
        {
            LinkedNode linkedNode = new(root) { Parent = parent };
            linkedNode.Children.AddRange(Create(nodes, linkedNode));

            yield return linkedNode;
        }
    }

    public static IEnumerable<LinkedNode> GetAllChildren(this LinkedNode node, Dictionary<int, Func<INode, bool>>? conditions = null, int level = 0)
    {
        if (conditions is not null && conditions.TryGetValue(level, out var condition))
        {
            if (condition(node))
                yield return node;
        }
        else
        {
            yield return node;
        }

        foreach (var child in node.Children)
        {
            foreach (var c in GetAllChildren(child, conditions, level + 1))
                yield return c;
        }
    }
}
