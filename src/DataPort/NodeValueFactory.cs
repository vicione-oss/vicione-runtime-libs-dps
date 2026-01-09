using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public static class NodeValueFactory
{
    public static (JsonObject Data, JsonObject Meta) CreateJsonStructure(IDictionary<string, INode[]> channelNodeRoutes, IEnumerable<ExternalValue> values, Func<ExternalValue, JsonNode?>? getMetaValue = null)
    {
        getMetaValue ??= GetTypeName;
        var data = new JsonObject();
        var meta = new JsonObject();

        foreach (var value in values)
        {
            var route = channelNodeRoutes[value.Channel];
            var jObject = JsonNodeExtensions.GetParentJsonNode(route, data);
            var metaObject = JsonNodeExtensions.GetParentJsonNode(route, meta);
            var rawValue = value.Value;
            JsonNode? nodeValue;

            if (rawValue is JsonNode v)
                nodeValue = v;
            else
                nodeValue = JsonValue.Create(rawValue);

            jObject.Add(route.Last().Name, nodeValue);
            metaObject.Add(route.Last().Name, getMetaValue(value));
        }

        return (data, meta);
    }

    private static JsonNode? GetTypeName(ExternalValue value)
        => value.Value?.GetType().AssemblyQualifiedName ?? typeof(object).AssemblyQualifiedName;

    public static JsonObject IncludeMeta(this (JsonObject Data, JsonObject Meta) obj)
    {
        obj.Data["$meta"] = obj.Meta;
        return obj.Data;
    }

    public static JsonNode? GetValueJsonNode(ReadOnlySpan<INode> structure, JsonNode? node, int index = 0)
    {
        if (!((structure.Length - index) > 0) || node is null)
        {
            return node;
        }
        else
        {
            var part = structure[index];
            return GetValueJsonNode(structure, node[part.Name], index + 1);
        }
    }
}
