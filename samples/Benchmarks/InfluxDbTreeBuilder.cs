using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort;

namespace Benchmarks;

internal static class InfluxDbTreeBuilder
{
    internal static List<List<ExternalValue>> CreateTestData(IReadOnlyCollection<Node> nodes, int count)
    {
        var records = nodes.GetRecords();

        var data = new List<List<ExternalValue>>();

        for (var i = 0; i < count; i++)
        {
            data.Add([.. records.Select(f => new ExternalValue
            {
                Channel = f,
                Value = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Validity = 0,
            })]);
        }

        return data;
    }

    private static string[] GetRecords(this IEnumerable<Node> nodes)
        => [.. nodes
            .Where(n => n.DesignId is InfluxDbNodeDesignId.FieldNode or InfluxDbNodeDesignId.TagNode)
            .Select(n => n.AffectedChannels.Single())];

    internal static List<Node> CreateTree(IInfluxDbThroughput influx)
        => CreateTree(influx.Buckets, influx.Measurements, influx.Fields, influx.Tags);

    internal static List<Node> CreateTree(int buckets, int measurements, int fields, int tags)
    {
        List<Node> nodes = [];

        for (var b = 0; b < buckets; b++)
        {
            var bucketId = Guid.NewGuid();
            nodes.Add(CreateNode(bucketId, null, InfluxDbNodeDesignId.BucketNode));

            for (var m = 0; m < measurements; m++)
            {
                var measurementId = Guid.NewGuid();
                nodes.Add(CreateNode(measurementId, bucketId, InfluxDbNodeDesignId.MeasurementNode));

                for (var f = 0; f < fields; f++)
                    nodes.Add(CreateValueNode(Guid.NewGuid(), measurementId, InfluxDbNodeDesignId.FieldNode, typeof(string)));

                for (var t = 0; t < tags; t++)
                    nodes.Add(CreateValueNode(Guid.NewGuid(), measurementId, InfluxDbNodeDesignId.TagNode, typeof(string)));
            }
        }

        return nodes;
    }

    private static Node CreateNode(Guid id, Guid? parent, string designId)
        => new()
        {
            Id = id,
            DesignId = designId,
            ParentId = parent,
            Name = id.ToString(),
        };

    private static Node CreateValueNode(Guid id, Guid? parent, string designId, Type type)
        => new()
        {
            Id = id,
            DesignId = designId,
            ParentId = parent,
            Name = id.ToString(),
            AffectedChannels = { id.ToString(), },
            ValueType = type,
        };
}
