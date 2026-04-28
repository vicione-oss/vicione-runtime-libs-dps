using System.Linq;
using System.Reflection;
using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.TypedNodes.Mapping;

public class BranchConfigurationNodeMapperTests
{
    [Fact]
    public void GetPropertiesDefinedInTargetNode_ExcludesBaseProperties()
    {
        // Arrange
        IBranchConfigurationNodeMapper<TestBranchNodeWithProperties> mapper = new TestBranchMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty1));
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty2));
    }

    [Fact]
    public void PropertyNamesDefinedInTargetNode_ReturnsPropertyNames()
    {
        // Arrange
        IBranchConfigurationNodeMapper<TestBranchNodeWithProperties> mapper = new TestBranchMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty1));
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty2));
    }

    [Fact]
    public void WithMinimalNode_ReturnsEmptyList()
    {
        // Arrange - TestBranchNode has no custom properties beyond the interface
        IBranchConfigurationNodeMapper<TestBranchNode> mapper = new MinimalBranchMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().BeEmpty();
    }

    [Fact]
    public void GetPropertiesDefinedInTargetNode_ReturnsPropertyInfo()
    {
        // Arrange
        IBranchConfigurationNodeMapper<TestBranchNodeWithProperties> mapper = new TestBranchMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        properties.Should().AllBeAssignableTo<PropertyInfo>();
        properties.Should().OnlyContain(p => p.DeclaringType == typeof(TestBranchNodeWithProperties));
    }

    [Fact]
    public void GetPropertiesDefinedInTargetNode_ExcludesPropertiesMarkedWithExcludeFromMapping()
    {
        // Arrange
        IBranchConfigurationNodeMapper<TestBranchNodeWithExcludedProperty> mapper = new ExcludedPropertyBranchMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().HaveCount(1);
        propertyNames.Should().Contain(nameof(TestBranchNodeWithExcludedProperty.MappedProperty));
        propertyNames.Should().NotContain(nameof(TestBranchNodeWithExcludedProperty.ComputedProperty));
    }

    // Test mappers

    private sealed class TestBranchMapper : IBranchConfigurationNodeMapper<TestBranchNodeWithProperties>
    {
        public string TargetLinkedNodeTypeId => "test-branch";
        public TestBranchNodeWithProperties Map(LinkedNode node) => new TestBranchNodeWithProperties
        {
            OriginalNode = node
        };
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class MinimalBranchMapper : IBranchConfigurationNodeMapper<TestBranchNode>
    {
        public string TargetLinkedNodeTypeId => "minimal-branch";
        public TestBranchNode Map(LinkedNode node) => new TestBranchNode
        {
            OriginalNode = node
        };
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class ExcludedPropertyBranchMapper : IBranchConfigurationNodeMapper<TestBranchNodeWithExcludedProperty>
    {
        public string TargetLinkedNodeTypeId => "excluded-property-branch";
        public TestBranchNodeWithExcludedProperty Map(LinkedNode node) => new TestBranchNodeWithExcludedProperty
        {
            OriginalNode = node
        };
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }
}
