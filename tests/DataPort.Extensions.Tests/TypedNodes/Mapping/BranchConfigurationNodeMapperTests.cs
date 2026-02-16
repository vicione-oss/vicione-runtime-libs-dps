using System.Linq;
using System.Reflection;
using FluentValidation.Results;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.TypedNodes.Mapping;

public class BranchConfigurationNodeMapperTests
{
    [Fact]
    public void GetPropertiesDefinedInTargetNode_ExcludesBaseProperties()
    {
        // Arrange
        IBranchConfigurationNodeMapper<ITestBranchNode> mapper = new TestBranchMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(ITestBranchNode.CustomProperty1));
        propertyNames.Should().Contain(nameof(ITestBranchNode.CustomProperty2));
    }

    [Fact]
    public void PropertyNamesDefinedInTargetNode_ReturnsPropertyNames()
    {
        // Arrange
        IBranchConfigurationNodeMapper<ITestBranchNode> mapper = new TestBranchMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().HaveCount(2);
        propertyNames.Should().Contain(nameof(ITestBranchNode.CustomProperty1));
        propertyNames.Should().Contain(nameof(ITestBranchNode.CustomProperty2));
    }

    [Fact]
    public void WithMinimalNode_ReturnsEmptyList()
    {
        // Arrange - IMinimalBranchNode has no custom properties beyond the interface
        IBranchConfigurationNodeMapper<IMinimalBranchNode> mapper = new MinimalBranchMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert - Should be empty since no properties beyond base interfaces
        propertyNames.Should().BeEmpty();
    }

    [Fact]
    public void GetPropertiesDefinedInTargetNode_ReturnsPropertyInfo()
    {
        // Arrange
        IBranchConfigurationNodeMapper<ITestBranchNode> mapper = new TestBranchMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        properties.Should().AllBeAssignableTo<PropertyInfo>();
        properties.Should().OnlyContain(p => p.DeclaringType == typeof(ITestBranchNode));
    }

    // Test node implementations

    private interface ITestBranchNode : IBranchConfigurationNode
    {
        string CustomProperty1 { get; set; }
        int CustomProperty2 { get; set; }
    }

    private interface IMinimalBranchNode : IBranchConfigurationNode
    {
        // No custom properties
    }

    // Test mappers

    private sealed class TestBranchMapper : IBranchConfigurationNodeMapper<ITestBranchNode>
    {
        public string TargetLinkedNodeTypeId => "test-branch";
        public ITestBranchNode Map(LinkedNode node) => Substitute.For<ITestBranchNode>();
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class MinimalBranchMapper : IBranchConfigurationNodeMapper<IMinimalBranchNode>
    {
        public string TargetLinkedNodeTypeId => "minimal-branch";
        public IMinimalBranchNode Map(LinkedNode node) => Substitute.For<IMinimalBranchNode>();
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }
}
