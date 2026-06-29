#pragma warning disable CA1812 // Avoid uninstantiated internal classes - Used via generic type parameters

using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

namespace ViciOne.Suite.DataPort.Extensions.Testing.Tests.YamlTesting;

public class NodeMapperExtensionsTests
{
    [Fact]
    public void BranchMapper_GetPropertiesDefinedInTargetNode_ExcludesBaseProperties()
    {
        // Arrange
        IBranchConfigurationNodeMapper<TestBranchNode> mapper = new TestBranchMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().Contain(nameof(TestBranchNode.CustomProperty));
        propertyNames.Should().NotContain(nameof(IConfigurationNode.ParentConfigurationNode));
        propertyNames.Should().NotContain(nameof(IBranchConfigurationNode.OriginalNode));
    }

    [Fact]
    public void BranchMapper_PropertyNamesDefinedInTargetNode_ReturnsOnlyCustomProperties()
    {
        // Arrange
        IBranchConfigurationNodeMapper<TestBranchNode> mapper = new TestBranchMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().HaveCount(1);
        propertyNames.Should().Contain(nameof(TestBranchNode.CustomProperty));
    }

    [Fact]
    public void BranchMapper_PropertyNamesDefinedInTargetNode_ReturnsEmpty_WhenNoCustomProperties()
    {
        // Arrange
        IBranchConfigurationNodeMapper<MinimalBranchNode> mapper = new MinimalBranchMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().BeEmpty();
    }

    [Fact]
    public void DataPointMapper_GetPropertiesDefinedInTargetNode_ExcludesBaseProperties()
    {
        // Arrange
        IDataPointNodeMapper<TestDataPointNode> mapper = new TestDataPointMapper();

        // Act
        var properties = mapper.GetPropertiesDefinedInTargetNode();

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().Contain(nameof(TestDataPointNode.CustomValue));
        propertyNames.Should().NotContain(nameof(IDataPointNode.Channels));
        propertyNames.Should().NotContain(nameof(IDataPointNode.OriginalNode));
    }

    [Fact]
    public void DataPointMapper_PropertyNamesDefinedInTargetNode_ReturnsOnlyCustomProperties()
    {
        // Arrange
        IDataPointNodeMapper<TestDataPointNode> mapper = new TestDataPointMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().HaveCount(1);
        propertyNames.Should().Contain(nameof(TestDataPointNode.CustomValue));
    }

    [Fact]
    public void DataPointMapper_PropertyNamesDefinedInTargetNode_ReturnsEmpty_WhenNoCustomProperties()
    {
        // Arrange
        IDataPointNodeMapper<MinimalDataPointNode> mapper = new MinimalDataPointMapper();

        // Act
        var propertyNames = mapper.PropertyNamesDefinedInTargetNode();

        // Assert
        propertyNames.Should().BeEmpty();
    }

    // Stubs

    private sealed record TestBranchNode : IBranchConfigurationNode
    {
        public required LinkedNode OriginalNode { get; init; }
        public IConfigurationNode? ParentConfigurationNode { get; set; }
        public List<IConfigurationNode> ConfigurationNodes { get; } = [];
        public List<IDataPointNode> DataPointNodes { get; } = [];
        public string CustomProperty { get; set; } = string.Empty;
        public bool CanBeAdded(IConfigurationNode configurationNode) => true;
        public bool CanBeAdded(IDataPointNode dataPointNode) => true;
    }

    private sealed record MinimalBranchNode : IBranchConfigurationNode
    {
        public required LinkedNode OriginalNode { get; init; }
        public IConfigurationNode? ParentConfigurationNode { get; set; }
        public List<IConfigurationNode> ConfigurationNodes { get; } = [];
        public List<IDataPointNode> DataPointNodes { get; } = [];
        public bool CanBeAdded(IConfigurationNode configurationNode) => true;
        public bool CanBeAdded(IDataPointNode dataPointNode) => true;
    }

    private sealed record TestDataPointNode(LinkedNode OriginalNode) : IDataPointNode
    {
        public IConfigurationNode? Parent { get; set; }
        public string CustomValue { get; set; } = string.Empty;
        public Channels Channels { get; } = new([], []);
    }

    private sealed record MinimalDataPointNode(LinkedNode OriginalNode) : IDataPointNode
    {
        public IConfigurationNode? Parent { get; set; }
        public Channels Channels { get; } = new([], []);
    }

    private sealed class TestBranchMapper : IBranchConfigurationNodeMapper<TestBranchNode>
    {
        public string TargetLinkedNodeTypeId => "test-branch";
        public TestBranchNode Map(LinkedNode node) => new() { OriginalNode = node };
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class MinimalBranchMapper : IBranchConfigurationNodeMapper<MinimalBranchNode>
    {
        public string TargetLinkedNodeTypeId => "minimal-branch";
        public MinimalBranchNode Map(LinkedNode node) => new() { OriginalNode = node };
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class TestDataPointMapper : IDataPointNodeMapper<TestDataPointNode>
    {
        public string TargetLinkedNodeTypeId => "test-datapoint";
        public TestDataPointNode Map(LinkedNode node) => new(node);
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }

    private sealed class MinimalDataPointMapper : IDataPointNodeMapper<MinimalDataPointNode>
    {
        public string TargetLinkedNodeTypeId => "minimal-datapoint";
        public MinimalDataPointNode Map(LinkedNode node) => new(node);
        public ValidationResult Validate(LinkedNode linkedNode) => new();
    }
}
