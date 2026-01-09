using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using ViciOne.ManagedEngine.ExternalCommunication;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class InfluxDbDataPortOutgoing_GetExpectedStructure
{
    [Fact]
    public void Acceptance()
    {
        Node bucketNode = new()
        {
            Id = Guid.NewGuid(),
            DesignId = InfluxDbNodeDesignId.BucketNode,
            Name = "bucket",
        };
        Node measurementNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = bucketNode.Id,
            DesignId = InfluxDbNodeDesignId.MeasurementNode,
            Name = "measurement",
        };
        Node tagNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = measurementNode.Id,
            DesignId = InfluxDbNodeDesignId.TagNode,
            Name = "tag",
            ValueType = typeof(string),
        };
        Node fieldNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = measurementNode.Id,
            DesignId = InfluxDbNodeDesignId.FieldNode,
            Name = "field",
            ValueType = typeof(string),
        };

        var nodes = new[] { bucketNode, measurementNode, tagNode, fieldNode };

        var structure = InfluxDbDataPortOutgoing.GetExpectedStructure(nodes);

        var bucket = structure.Should().ContainSingle().Which;
        bucket.Name.Should().Be(bucketNode.Name);
    }
}

public class InfluxDbDataPortOutgoing_PreparePointData
{
    [Fact]
    public void Can_handle_json_structure()
    {
        Node fieldNode = new()
        {
            Id = Guid.NewGuid(),
            Name = "JSON field node"
        };
        Node apartmentNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = fieldNode.Id,
            Name = "apartment node"
        };
        Node kitchenNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = apartmentNode.Id,
            Name = "kitchen node"
        };
        Node livingRoomNode = new()
        {
            Id = Guid.NewGuid(),
            ParentId = apartmentNode.Id,
            Name = "living room node"
        };

        MeasurementInfo measurement = new("default", "default");
        RecordInfo record = new(measurement, "JSON field node", RecordType.Field, true, false);

        Dictionary<string, RecordInfo> channelRecords = new()
        {
            { kitchenNode.Id.ToString(), record },
            { livingRoomNode.Id.ToString(), record },
        };

        Dictionary<string, INode[]> routes = new()
        {
            { kitchenNode.Id.ToString(), [apartmentNode, kitchenNode] },
            { livingRoomNode.Id.ToString(), [apartmentNode, livingRoomNode] },
        };

        List<ExternalValue> values =
        [
            new()
            {
                Channel = kitchenNode.Id.ToString(),
                Value = 1,
            },
            new()
            {
                Channel = livingRoomNode.Id.ToString(),
                Value = 2,
            },
        ];

        var data = InfluxDbDataPortOutgoing.PrepareDataRecords(values, channelRecords, routes);

        data.Should().ContainSingle().Which.Records.Should().ContainSingle().Which.Value.Should().Be("""
        {
          "apartment node": {
            "kitchen node": 1,
            "living room node": 2
          },
          "$meta": {
            "apartment node": {
              "kitchen node": "System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
              "living room node": "System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
            }
          }
        }
        """);
    }

    [Fact]
    public void Can_handle_single_value()
    {
        var expected = 23;

        Node field = new()
        {
            Id = Guid.NewGuid(),
            Name = "record"
        };

        MeasurementInfo measurement = new("default", "default");
        RecordInfo record = new(measurement, "record", RecordType.Field, false, false);

        Dictionary<string, RecordInfo> channelRecords = new()
        {
            { field.Id.ToString(), record },
        };

        List<ExternalValue> values =
        [
            new()
            {
                Channel = field.Id.ToString(),
                Value = 23,
            },
        ];

        var data = InfluxDbDataPortOutgoing.PrepareDataRecords(values, channelRecords, []).ToList();

        data.Should().ContainSingle().Which.Records.Should().ContainSingle().Which.Value.Should().Be(expected);
    }
}

public class InfluxDbDataPortOutgoing_CreatePointData
{
    [Fact]
    public void Acceptance()
    {
        var measurement = new MeasurementInfo("default", "default");
        var record = new DataRecord(measurement, new()
        {
            { new RecordInfo(measurement, "default", RecordType.Field, false, false), 23 },
            { new RecordInfo(measurement, "default", RecordType.Tag, false, false), "test" },
        }, DateTime.Now);

        var points = InfluxDbDataPortOutgoing.CreatePointToSend("bucket", [record]);

        points.Should().ContainSingle().Which.PointData.HasFields().Should().BeTrue();
    }
}
