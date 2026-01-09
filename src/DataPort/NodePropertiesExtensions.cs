using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ViciOne.Suite.DataPort;

public static class NodePropertiesExtensions
{
    public static T GetPropertyValueOrDefault<T>(this INode node, string name, T defaultValue) where T : notnull
        => node.TryGetProperty(name, out var property) ? property.GetValue<T>() : defaultValue;

    public static T GetValue<T>(this Property property) where T : notnull
        => (T)property.Value;

    public static Property GetProperty(this INode node, string name)
        => GetProperty(node.Properties, name.Split('.'), 0);

    private static Property GetProperty(IDictionary<string, Property> properties, string[] levels, int index)
    {
        if (properties.TryGetValue(levels[index], out var property))
        {
            if ((levels.Length - 1) > index)
            {
                if (property.Value is IDictionary<string, Property> nextProperties)
                    return GetProperty(nextProperties, levels, index + 1);
                throw new InvalidOperationException($"The property '{levels[index]}' does not contain a next level '{levels[index + 1]}'.");
            }
            return property;
        }

        throw new InvalidOperationException($"Property '{levels[index]}' does not exist.");
    }

    public static bool TryGetProperty(this INode node, string name, [NotNullWhen(true)] out Property? value)
        => TryGetProperty(node.Properties, name.Split('.'), 0, out value);

    private static bool TryGetProperty(IDictionary<string, Property> properties, string[] levels, int index, [NotNullWhen(true)] out Property? value)
    {
        if (properties.TryGetValue(levels[index], out var property))
        {
            if ((levels.Length - 1) > index)
            {
                if (property.Value is IDictionary<string, Property> nextProperties)
                    return TryGetProperty(nextProperties, levels, index + 1, out value);
                value = null;
                return false;
            }

            value = property;
            return true;
        }
        value = null;
        return false;
    }

    public static IEnumerable<string> GetAllPropertyChannels(this IEnumerable<INode> nodes)
        => GetPropertyChannels(nodes.SelectMany(n => n.Properties));

    private static IEnumerable<string> GetPropertyChannels(IEnumerable<KeyValuePair<string, Property>> properties)
    {
        foreach (var (_, property) in properties)
        {
            IEnumerable<string> channels;

            if (property.Value is IDictionary<string, Property> subProperties)
                channels = GetPropertyChannels(subProperties);
            else
                channels = property.Channels;

            foreach (var channel in channels)
                yield return channel;
        }
    }
}
