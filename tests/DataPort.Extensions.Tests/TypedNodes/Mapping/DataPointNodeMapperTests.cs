using System.Linq;
using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.TypedNodes.Mapping;

public class DataPointNodeMapperTests
{
    [Fact]
    public void GetPropertiesDefinedInTargetNode_ExcludesBaseProperties()
    {
        // Arrange
        IDataPointNodeMapper<TestDataPointNodeWithProperties> mapper = new TestDataPointMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(TestDataPointNodeWithProperties.DataValue));
        propertyNames.Should().Contain(nameof(TestDataPointNodeWithProperties.Timestamp));
    }

    [Fact]
    public void PropertyNamesDefinedInTargetNode_ReturnsPropertyNames()
    {
        // Arrange
        IDataPointNodeMapper<TestDataPointNodeWithProperties> mapper = new TestDataPointMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(TestDataPointNodeWithProperties.DataValue));
        propertyNames.Should().Contain(nameof(TestDataPointNodeWithProperties.Timestamp));
    }

    [Fact]
    public void WithMinimalNode_ReturnsEmptyList()
    {
        // Arrange - IDataPointNode has no custom properties beyond the base
        IDataPointNodeMapper<IDataPointNode> mapper = new MinimalDataPointMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().BeEmpty();
    }

    // Test mappers

    private sealed class TestDataPointMapper : IDataPointNodeMapper<TestDataPointNodeWithProperties>
    {
        public string TargetLinkedNodeTypeId => "test-datapoint";
        public TestDataPointNodeWithProperties Map(LinkedNode node) => new TestDataPointNodeWithProperties
        {
            OriginalNode = node
        };
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class MinimalDataPointMapper : IDataPointNodeMapper<IDataPointNode>
    {
        public string TargetLinkedNodeTypeId => "minimal-datapoint";
        public IDataPointNode Map(LinkedNode node) => new TestDataPointNode { OriginalNode = node };
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }
}
