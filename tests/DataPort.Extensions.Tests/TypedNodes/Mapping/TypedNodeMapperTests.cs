using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NSubstitute;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.TypedNodes.Mapping;

public class TypedNodeMapperTests
{
    private readonly IRootConfigurationNodeMapper<ITestRootNode, TestCommunication> _rootMapper;
    private readonly IBranchConfigurationNodeMapper<IBranchConfigurationNode> _configMapper;
    private readonly IDataPointNodeMapper<IDataPointNode> _dataPointMapper;
    private readonly TypedNodeMapper<TestCommunication, ITestRootNode> _sut;

    public TypedNodeMapperTests()
    {
        _rootMapper = Substitute.For<IRootConfigurationNodeMapper<ITestRootNode, TestCommunication>>();
        _configMapper = Substitute.For<IBranchConfigurationNodeMapper<IBranchConfigurationNode>>();
        _dataPointMapper = Substitute.For<IDataPointNodeMapper<IDataPointNode>>();

        _sut = new TypedNodeMapper<TestCommunication, ITestRootNode>(
            _rootMapper,
            [_configMapper],
            [_dataPointMapper]);
    }

    [Fact]
    public void MapToTypedNodes_WithValidCommunication_ShouldCallRootMapperValidate()
    {
        // Arrange
        var communication = new TestCommunication();
        var rootNode = Substitute.For<ITestRootNode>();

        _rootMapper.Validate(communication).Returns(new ValidationResult());
        _rootMapper.CreateRootNode(communication).Returns(rootNode);

        // Act
        _sut.MapToTypedNodes(communication);

        // Assert
        _rootMapper.Received(1).Validate(communication);
    }

    [Fact]
    public void MapToTypedNodes_WithInvalidCommunication_ShouldThrowInvalidConfigurationException()
    {
        // Arrange
        var communication = new TestCommunication();
        var validationFailures = new List<ValidationFailure> { new("Host", "Host is required") };
        var validationResult = new ValidationResult(validationFailures);

        _rootMapper.Validate(communication).Returns(validationResult);

        // Act
        var act = () => _sut.MapToTypedNodes(communication);

        // Assert
        act.Should().ThrowExactly<InvalidConfigurationException>()
            .WithMessage("*Host*")
            .WithMessage("*TestCommunication*");
    }

    [Fact]
    public void MapToTypedNodes_WithInvalidConfigurationNode_ShouldThrowInvalidConfigurationException()
    {
        // Arrange
        var configNode = new Node
        {
            Id = Guid.NewGuid(),
            DesignId = "TestConfig",
            Name = "TestConfigNode",
            Properties = []
        };

        var communication = new TestCommunication { Nodes = [configNode] };
        var rootNode = Substitute.For<ITestRootNode>();

        _rootMapper.Validate(communication).Returns(new ValidationResult());
        _rootMapper.CreateRootNode(communication).Returns(rootNode);
        _configMapper.TargetLinkedNodeTypeId.Returns("TestConfig");
        _configMapper.IsTargetMapperFor(Arg.Any<LinkedNode>()).Returns(true);

        var validationFailures = new List<ValidationFailure> { new("SomeProperty", "Some property is invalid") };
        _configMapper.Validate(Arg.Any<LinkedNode>()).Returns(new ValidationResult(validationFailures));

        // Act
        var act = () => _sut.MapToTypedNodes(communication);

        // Assert
        act.Should().ThrowExactly<InvalidConfigurationException>()
            .WithMessage("*TestConfigNode*")
            .WithMessage("*TestConfig*")
            .WithMessage("*SomeProperty*");
        _configMapper.Received(1).Validate(Arg.Any<LinkedNode>());
    }

    [Fact]
    public void MapToTypedNodes_WithInvalidDataPointNode_ShouldThrowInvalidConfigurationException()
    {
        // Arrange
        var dataPointNode = new Node
        {
            Id = Guid.NewGuid(),
            DesignId = "TestDataPoint",
            Name = "TestDataPointNode",
            Properties = []
        };

        var communication = new TestCommunication { Nodes = [dataPointNode] };
        var rootNode = Substitute.For<ITestRootNode>();

        _rootMapper.Validate(communication).Returns(new ValidationResult());
        _rootMapper.CreateRootNode(communication).Returns(rootNode);
        _dataPointMapper.TargetLinkedNodeTypeId.Returns("TestDataPoint");
        _dataPointMapper.IsTargetMapperFor(Arg.Any<LinkedNode>()).Returns(true);

        var validationFailures = new List<ValidationFailure> { new("ByteNumber", "ByteNumber must be non-negative") };
        _dataPointMapper.Validate(Arg.Any<LinkedNode>()).Returns(new ValidationResult(validationFailures));

        // Act
        var act = () => _sut.MapToTypedNodes(communication);

        // Assert
        act.Should().ThrowExactly<InvalidConfigurationException>()
            .WithMessage("*TestDataPointNode*")
            .WithMessage("*TestDataPoint*")
            .WithMessage("*ByteNumber*");
        _dataPointMapper.Received(1).Validate(Arg.Any<LinkedNode>());
    }

    [Fact]
    public void MapToTypedNodes_WithValidNodes_ShouldCallValidateOnce()
    {
        // Arrange
        var configNode = new Node
        {
            Id = Guid.NewGuid(),
            DesignId = "TestConfig",
            Name = "TestConfigNode",
            Properties = []
        };

        var dataPointNode = new Node
        {
            Id = Guid.NewGuid(),
            ParentId = configNode.Id,
            DesignId = "TestDataPoint",
            Name = "TestDataPointNode",
            Properties = []
        };

        var communication = new TestCommunication { Nodes = [configNode, dataPointNode] };
        var rootNode = Substitute.For<ITestRootNode>();
        var mockConfigNode = Substitute.For<IBranchConfigurationNode>();
        var mockDataPointNode = Substitute.For<IDataPointNode>();

        _rootMapper.Validate(communication).Returns(new ValidationResult());
        _rootMapper.CreateRootNode(communication).Returns(rootNode);

        _configMapper.TargetLinkedNodeTypeId.Returns("TestConfig");
        _configMapper.IsTargetMapperFor(Arg.Is<LinkedNode>(static n => n.DesignId == "TestConfig")).Returns(true);
        _configMapper.Validate(Arg.Any<LinkedNode>()).Returns(new ValidationResult());
        _configMapper.Map(Arg.Any<LinkedNode>()).Returns(mockConfigNode);

        _dataPointMapper.TargetLinkedNodeTypeId.Returns("TestDataPoint");
        _dataPointMapper.IsTargetMapperFor(Arg.Is<LinkedNode>(static n => n.DesignId == "TestDataPoint")).Returns(true);
        _dataPointMapper.Validate(Arg.Any<LinkedNode>()).Returns(new ValidationResult());
        _dataPointMapper.Map(Arg.Any<LinkedNode>()).Returns(mockDataPointNode);

        // Act
        _sut.MapToTypedNodes(communication);

        // Assert
        _rootMapper.Received(1).Validate(communication);
        _configMapper.Received(1).Validate(Arg.Is<LinkedNode>(static n => n.DesignId == "TestConfig"));
        _dataPointMapper.Received(1).Validate(Arg.Is<LinkedNode>(static n => n.DesignId == "TestDataPoint"));
    }

    // Test helper classes
#pragma warning disable CA1515 // types are used by NSubstitute and cannot be avoided
    public record TestCommunication : DataPortCommunication;

    // ReSharper disable once MemberCanBePrivate.Global
    public interface ITestRootNode : IRootConfigurationNode<TestCommunication>;
#pragma warning restore CA1515 // types are used by NSubstitute and cannot be avoided
}
