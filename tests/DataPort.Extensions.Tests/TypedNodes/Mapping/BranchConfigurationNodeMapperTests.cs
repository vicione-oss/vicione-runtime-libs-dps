using System.Linq;
using System.Reflection;
using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Folders;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

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
        propertyNames.Should().HaveCount(3);
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty1));
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty2));
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.FullPath));
    }

    [Fact]
    public void PropertyNamesDefinedInTargetNode_ReturnsPropertyNames()
    {
        // Arrange
        IBranchConfigurationNodeMapper<TestBranchNodeWithProperties> mapper = new TestBranchMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().HaveCount(3);
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty1));
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.CustomProperty2));
        propertyNames.Should().Contain(nameof(TestBranchNodeWithProperties.FullPath));
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

}
