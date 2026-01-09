using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace ViciOne.Suite.DataPort;

public static class JsonNodeExtensions
{
    public static JsonObject GetParentJsonNode(IReadOnlyCollection<INode> structure, JsonObject node)
    {
        if (structure.Count <= 1)
        {
            return node;
        }
        else
        {
            var part = structure.First();
            node.TryAdd(part.Name, new JsonObject());
            return GetParentJsonNode([.. structure.Skip(1)], (JsonObject)(node[part.Name] ?? throw new InvalidOperationException("Node cannot be null.")));
        }
    }
}
