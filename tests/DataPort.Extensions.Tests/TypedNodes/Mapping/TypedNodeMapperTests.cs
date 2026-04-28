using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NSubstitute;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Folders;
using ViciOne.Suite.DataPort.Extensions.Exceptions;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.TypedNodes.Mapping;

public class TypedNodeMapperTests
{
    private readonly IRootConfigurationNodeMapper<TestDeviceNode, TestCommunication> _rootMapper;
    private readonly IBranchConfigurationNodeMapper<IBranchConfigurationNode> _configMapper;
    private readonly IDataPointNodeMapper<IDataPointNode> _dataPointMapper;
    private readonly TypedNodeMapper<TestCommunication, TestDeviceNode> _sut;

    public TypedNodeMapperTests()
    {
        _rootMapper = Substitute.For<IRootConfigurationNodeMapper<TestDeviceNode, TestCommunication>>();
        _configMapper = Substitute.For<IBranchConfigurationNodeMapper<IBranchConfigurationNode>>();
        _dataPointMapper = Substitute.For<IDataPointNodeMapper<IDataPointNode>>();

        _sut = new TypedNodeMapper<TestCommunication, TestDeviceNode>(
            _rootMapper,
            [_configMapper],
            [_dataPointMapper]);
    }

    [Fact]
    public void MapToTypedNodes_WithValidCommunication_ShouldCallRootMapperValidate()
    {
        // Arrange
        var communication = new TestCommunication();
        var rootNode = new TestDeviceNode
        {
            OriginalCommunication = communication
        };

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
        var validationFailures = new List<ValidationFailure>
        {
            new("Host", "Host is required")
        };
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

        var communication = new TestCommunication
        {
            Nodes = [configNode]
        };
        var rootNode = new TestDeviceNode
        {
            OriginalCommunication = communication
        };

        _rootMapper.Validate(communication).Returns(new ValidationResult());
        _rootMapper.CreateRootNode(communication).Returns(rootNode);
        _configMapper.TargetLinkedNodeTypeId.Returns("TestConfig");
        _configMapper.IsTargetMapperFor(Arg.Any<LinkedNode>()).Returns(true);

        var validationFailures = new List<ValidationFailure>
        {
            new("SomeProperty", "Some property is invalid")
        };
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

        var communication = new TestCommunication
        {
            Nodes = [dataPointNode]
        };
        var rootNode = new TestDeviceNode
        {
            OriginalCommunication = communication
        };

        _rootMapper.Validate(communication).Returns(new ValidationResult());
        _rootMapper.CreateRootNode(communication).Returns(rootNode);
        _dataPointMapper.TargetLinkedNodeTypeId.Returns("TestDataPoint");
        _dataPointMapper.IsTargetMapperFor(Arg.Any<LinkedNode>()).Returns(true);

        var validationFailures = new List<ValidationFailure>
        {
            new("ByteNumber", "ByteNumber must be non-negative")
        };
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

        var communication = new TestCommunication
        {
            Nodes = [configNode, dataPointNode]
        };
        var rootNode = new TestDeviceNode
        {
            OriginalCommunication = communication
        };

        _rootMapper.Validate(communication).Returns(new ValidationResult());
        _rootMapper.CreateRootNode(communication).Returns(rootNode);

        _configMapper.TargetLinkedNodeTypeId.Returns("TestConfig");
        _configMapper.IsTargetMapperFor(Arg.Is<LinkedNode>(static n => n.DesignId == "TestConfig")).Returns(true);
        _configMapper.Validate(Arg.Any<LinkedNode>()).Returns(new ValidationResult());
        _configMapper.Map(Arg.Any<LinkedNode>()).Returns(callInfo =>
        {
            var node = callInfo.Arg<LinkedNode>();
            return new TestBranchNode
            {
                OriginalNode = node
            };
        });

        _dataPointMapper.TargetLinkedNodeTypeId.Returns("TestDataPoint");
        _dataPointMapper.IsTargetMapperFor(Arg.Is<LinkedNode>(static n => n.DesignId == "TestDataPoint")).Returns(true);
        _dataPointMapper.Validate(Arg.Any<LinkedNode>()).Returns(new ValidationResult());
        _dataPointMapper.Map(Arg.Any<LinkedNode>()).Returns(callInfo =>
        {
            var node = callInfo.Arg<LinkedNode>();
            return new TestDataPointNode(node);
        });

        // Act
        _sut.MapToTypedNodes(communication);

        // Assert
        _rootMapper.Received(1).Validate(communication);
        _configMapper.Received(1).Validate(Arg.Is<LinkedNode>(static n => n.DesignId == "TestConfig"));
        _dataPointMapper.Received(1).Validate(Arg.Is<LinkedNode>(static n => n.DesignId == "TestDataPoint"));
    }
}
