using System.Linq;
using FluentValidation.Results;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.TypedNodes.Mapping;

public class DataPointNodeMapperTests
{
    [Fact]
    public void GetPropertiesDefinedInTargetNode_ExcludesBaseProperties()
    {
        // Arrange
        IDataPointNodeMapper<ITestDataPoint> mapper = new TestDataPointMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(ITestDataPoint.DataValue));
        propertyNames.Should().Contain(nameof(ITestDataPoint.Timestamp));
    }

    [Fact]
    public void PropertyNamesDefinedInTargetNode_ReturnsPropertyNames()
    {
        // Arrange
        IDataPointNodeMapper<ITestDataPoint> mapper = new TestDataPointMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(ITestDataPoint.DataValue));
        propertyNames.Should().Contain(nameof(ITestDataPoint.Timestamp));
    }

    [Fact]
    public void WithMinimalNode_ReturnsEmptyList()
    {
        // Arrange - IMinimalDataPoint has no custom properties beyond the interface
        IDataPointNodeMapper<IMinimalDataPoint> mapper = new MinimalDataPointMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert - Should be empty since no properties beyond base interface
        propertyNames.Should().BeEmpty();
    }

    // Test node implementations

    private interface ITestDataPoint : IDataPointNode
    {
        string DataValue { get; set; }
        long Timestamp { get; set; }
    }

    private interface IMinimalDataPoint : IDataPointNode
    {
        // No custom properties
    }

    // Test mappers

    private sealed class TestDataPointMapper : IDataPointNodeMapper<ITestDataPoint>
    {
        public string TargetLinkedNodeTypeId => "test-datapoint";
        public ITestDataPoint Map(LinkedNode node) => Substitute.For<ITestDataPoint>();
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class MinimalDataPointMapper : IDataPointNodeMapper<IMinimalDataPoint>
    {
        public string TargetLinkedNodeTypeId => "minimal-datapoint";
        public IMinimalDataPoint Map(LinkedNode node) => Substitute.For<IMinimalDataPoint>();
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }
}
