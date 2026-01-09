using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class InfluxDbExtensions_CreateRecordInfos
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

        var fieldInfos = InfluxDbExtensions.CreateRecordInfo(nodes);

        fieldInfos.Should().HaveCount(2).And.SatisfyRespectively(
            fi =>
            {
                fi.Key.Should().Be(tagNode.Id.ToString());
                fi.Value.Name.Should().Be("tag");
                fi.Value.MeasurementInfo.Bucket.Should().Be("bucket");
                fi.Value.MeasurementInfo.Measurement.Should().Be("measurement");
            },
            fi =>
            {
                fi.Key.Should().Be(fieldNode.Id.ToString());
                fi.Value.Name.Should().Be("field");
                fi.Value.MeasurementInfo.Bucket.Should().Be("bucket");
                fi.Value.MeasurementInfo.Measurement.Should().Be("measurement");
            });
    }
}

public class InfluxDbExtensions_IsSupportedType
{
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(float))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(long))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(double))]
    [InlineData(typeof(short))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(char))]
    public void Returns_true_if_value_is_supported(Type type)
    {
        var vaule = InfluxDbExtensions.IsSupportedType(type);

        vaule.Should().BeTrue();
    }

    [Fact]
    public void Retruns_false_if_value_is_not_supported()
    {
        var value = InfluxDbExtensions.IsSupportedType(typeof(List<string>));

        value.Should().BeFalse();
    }
}
