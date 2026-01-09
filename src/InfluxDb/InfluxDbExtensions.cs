using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Suite.DataPort;

internal static class InfluxDbExtensions
{
    private static readonly List<Type> s_supportedTypes =
    [
        typeof(string),
        typeof(int),
        typeof(float),
        typeof(bool),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(double),
        typeof(short),
        typeof(ushort),
        typeof(char)
    ];

    internal static bool IsSupportedType(Type type)
        => s_supportedTypes.Contains(type);

    internal static Dictionary<Guid, RecordInfo> CreateRecordInfo(IReadOnlyCollection<INode> nodes)
    {
        var bucketNodes = nodes.Where(n => n.DesignId == InfluxDbNodeDesignId.BucketNode);
        var nodeParentsCache = nodes.CreateNodeParentsCache();

        Dictionary<Guid, RecordInfo> records = [];

        foreach (var bucket in bucketNodes)
        {
            foreach (var measurement in bucket.GetSuccessorNodes(nodes))
            {
                var measurementInfo = new MeasurementInfo(bucket.Name, measurement.Name);

                foreach (var node in measurement.GetSuccessorNodes(nodes))
                {
                    records.Add(node.Id, node.DesignId switch
                    {
                        InfluxDbNodeDesignId.FieldNode => CreateField(measurementInfo, node),
                        InfluxDbNodeDesignId.TagNode => CreateTag(measurementInfo, node),
                        _ => throw new InvalidOperationException(),
                    });
                }
            }
        }

        return records;

        RecordInfo CreateField(MeasurementInfo measurementInfo, INode node)
            => new(
                measurementInfo,
                node.Name,
                RecordType.Field,
                node.GetSuccessorRoutes(nodeParentsCache).Any(s => s.Any(e => e.DesignId == InfluxDbNodeDesignId.JsonStructureNode)),
                node.ValueType is not null && !IsSupportedType(node.ValueType));

        RecordInfo CreateTag(MeasurementInfo measurementInfo, INode node)
            => new(
                measurementInfo,
                node.Name, RecordType.Tag,
                node.GetSuccessorRoutes(nodeParentsCache).Any(s => s.Any(e => e.DesignId == InfluxDbNodeDesignId.JsonStructureNode)),
                node.ValueType is not null && (node.ValueType != typeof(string)));
    }

    internal static IEnumerable<INode> GetSuccessorNodes(this INode node, IReadOnlyCollection<INode> nodes)
        => nodes.Where(n => n.ParentId == node.Id);
}
