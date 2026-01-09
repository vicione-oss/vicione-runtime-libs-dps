
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class InitializeUnspecificTree_InitializeOutgoing
{
    [Fact]
    public void Handles_single_node_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId,
                    ParentId = pathPartOneId,
                    Name = "data",
                    AffectedChannels = { "v1", },
                    TransferredChannels = { "v1", },
                },
            ];

        var (jsonChannelRoutes, channelTransferredNodes, groupNodes) = InitializeUnspecificTree.InitializeOutgoing(nodes);

        jsonChannelRoutes.Should().BeEmpty();
        channelTransferredNodes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be("v1");
            e.Value.Id.Should().Be(endpointId);
        });
        groupNodes.Should().BeEmpty();
    }

    [Fact]
    public void Handles_JSON_node_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var jsonNode1 = Guid.NewGuid();
        var jsonNode2 = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId,
                    ParentId = pathPartOneId,
                    Name = "data",
                    TransferredChannels = { "v1", },
                },
                new()
                {
                    Id = jsonNode1,
                    ParentId = endpointId,
                    Name = "Kitchen",
                },
                new()
                {
                    Id = jsonNode2,
                    ParentId = jsonNode1,
                    Name = "Temperature",
                    AffectedChannels = { "v1", },
                },
            ];

        var (jsonChannelRoutes, channelTransferredNodes, groupNodes) = InitializeUnspecificTree.InitializeOutgoing(nodes);

        jsonChannelRoutes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be("v1");
            e.Value.Should().HaveCount(2).And.SatisfyRespectively(
                n => n.Id.Should().Be(jsonNode1),
                n => n.Id.Should().Be(jsonNode2));
        });
        channelTransferredNodes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be("v1");
            e.Value.Id.Should().Be(endpointId);
        });
        groupNodes.Should().ContainSingle().Which.Should().Be(endpointId);
    }

    [Fact]
    public void Handles_JSON_and_single_value_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var jsonNode1 = Guid.NewGuid();
        var jsonNode2 = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId,
                    ParentId = pathPartOneId,
                    Name = "data",
                    TransferredChannels = { "gv1", },
                },
                new()
                {
                    Id = jsonNode1,
                    ParentId = endpointId,
                    Name = "Kitchen",
                },
                new()
                {
                    Id = jsonNode2,
                    ParentId = jsonNode1,
                    Name = "Temperature",
                    AffectedChannels = { "gv1", "v1", },
                    TransferredChannels = { "v1", },
                },
            ];

        var (jsonChannelRoutes, channelTransferredNodes, groupNodes) = InitializeUnspecificTree.InitializeOutgoing(nodes);

        jsonChannelRoutes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be("gv1");
            e.Value.Should().HaveCount(2).And.SatisfyRespectively(
                n => n.Id.Should().Be(jsonNode1),
                n => n.Id.Should().Be(jsonNode2));
        });
        channelTransferredNodes.Should().SatisfyRespectively(
            e =>
            {
                e.Key.Should().Be("gv1");
                e.Value.Id.Should().Be(endpointId);
            },
            e =>
            {
                e.Key.Should().Be("v1");
                e.Value.Id.Should().Be(jsonNode2);
            });
        groupNodes.Should().ContainSingle().Which.Should().Be(endpointId);
    }

    [Fact]
    public void Handles_same_address_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var pathPartTwoId = Guid.NewGuid();
        var endpointId1 = Guid.NewGuid();
        var endpointId2 = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId1,
                    ParentId = pathPartOneId,
                    Name = "data",
                    AffectedChannels = { "v1", },
                    TransferredChannels = { "v1", },
                },
                new()
                {
                    Id = endpointId2,
                    ParentId = pathPartOneId,
                    Name = "data",
                    TransferredChannels = { "gv1", },
                },
                new()
                {
                    Id = pathPartTwoId,
                    ParentId = endpointId2,
                    Name = "Sensor",
                    AffectedChannels = { "gv1", },
                },
            ];

        var (jsonChannelRoutes, channelTransferredNodes, groupNodes) = InitializeUnspecificTree.InitializeOutgoing(nodes);

        jsonChannelRoutes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be("gv1");
            e.Value.Should().ContainSingle().Which.Id.Should().Be(pathPartTwoId);
        });
        channelTransferredNodes.Should().HaveCount(2).And.SatisfyRespectively(
            e =>
            {
                e.Key.Should().Be("v1");
                e.Value.Id.Should().Be(endpointId1);
            },
            e =>
            {
                e.Key.Should().Be("gv1");
                e.Value.Id.Should().Be(endpointId2);
            });
        groupNodes.Should().ContainSingle().Which.Should().Be(endpointId2);
    }
}

public class InitializeUnspecificTree_InitializeIncoming
{
    [Fact]
    public void Handles_single_node_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId,
                    ParentId = pathPartOneId,
                    Name = "data",
                    AffectedChannels = { "v1", },
                    TransferredChannels = { "v1", },
                },
            ];

        var (transferredNodeJsonNodes, addressNodes) = InitializeUnspecificTree.InitializeIncoming(nodes, InitializeUnspecificTreeTests.ComposeAddress);

        transferredNodeJsonNodes.Should().BeEmpty();
        addressNodes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be("http://local/receive/data");
            e.Value.Should().ContainSingle().Which.Id.Should().Be(endpointId);
        });
    }

    [Fact]
    public void Handles_JSON_node_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var jsonNode1 = Guid.NewGuid();
        var jsonNode2 = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId,
                    ParentId = pathPartOneId,
                    Name = "data",
                    TransferredChannels = { "v1", },
                },
                new()
                {
                    Id = jsonNode1,
                    ParentId = endpointId,
                    Name = "Kitchen",
                },
                new()
                {
                    Id = jsonNode2,
                    ParentId = jsonNode1,
                    Name = "Temperature",
                    AffectedChannels = { "v1", },
                },
            ];

        var (transferredNodeJsonNodes, addressNodes) = InitializeUnspecificTree.InitializeIncoming(nodes, InitializeUnspecificTreeTests.ComposeAddress);

        transferredNodeJsonNodes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be(endpointId);
            e.Value.Should().ContainSingle().Which.Select(_ => _.Id).Should().ContainInOrder(jsonNode1, jsonNode2);
        });
        addressNodes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be("http://local/receive/data");
            e.Value.Should().ContainSingle().Which.Id.Should().Be(endpointId);
        });
    }

    [Fact]
    public void Handles_JSON_and_single_value_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var jsonNode1 = Guid.NewGuid();
        var jsonNode2 = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId,
                    ParentId = pathPartOneId,
                    Name = "data",
                    TransferredChannels = { "gv1", },
                },
                new()
                {
                    Id = jsonNode1,
                    ParentId = endpointId,
                    Name = "Kitchen",
                },
                new()
                {
                    Id = jsonNode2,
                    ParentId = jsonNode1,
                    Name = "Temperature",
                    AffectedChannels = { "gv1", "v1", },
                    TransferredChannels = { "v1", },
                },
            ];

        var (transferredNodeJsonNodes, addressNodes) = InitializeUnspecificTree.InitializeIncoming(nodes, InitializeUnspecificTreeTests.ComposeAddress);

        transferredNodeJsonNodes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be(endpointId);
            e.Value.Should().ContainSingle().Which.Select(_ => _.Id).Should().ContainInOrder(jsonNode1, jsonNode2);
        });
        addressNodes.Should().HaveCount(2).And.SatisfyRespectively(
            e =>
            {
                e.Key.Should().Be("http://local/receive/data");
                e.Value.Should().ContainSingle().Which.Id.Should().Be(endpointId);
            },
            e =>
            {
                e.Key.Should().Be("http://local/receive/data/Kitchen/Temperature");
                e.Value.Should().ContainSingle().Which.Id.Should().Be(jsonNode2);
            });
    }

    [Fact]
    public void Handles_same_address_correctly()
    {
        var pathPartOneId = Guid.NewGuid();
        var pathPartTwoId = Guid.NewGuid();
        var endpointId1 = Guid.NewGuid();
        var endpointId2 = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
                {
                    Id = pathPartOneId,
                    Name = "receive",
                },
                new()
                {
                    Id = endpointId1,
                    ParentId = pathPartOneId,
                    Name = "data",
                    AffectedChannels = { "v1", },
                    TransferredChannels = { "v1", },
                },
                new()
                {
                    Id = endpointId2,
                    ParentId = pathPartOneId,
                    Name = "data",
                    TransferredChannels = { "gv1", },
                },
                new()
                {
                    Id = pathPartTwoId,
                    ParentId = endpointId2,
                    Name = "Sensor",
                    AffectedChannels = { "gv1", },
                },
            ];

        var (transferredNodeJsonNodes, addressNodes) = InitializeUnspecificTree.InitializeIncoming(nodes, InitializeUnspecificTreeTests.ComposeAddress);

        transferredNodeJsonNodes.Should().ContainSingle().And.SatisfyRespectively(e =>
        {
            e.Key.Should().Be(endpointId2);
            e.Value.Should().ContainSingle().Which.Should().ContainSingle().Which.Id.Should().Be(pathPartTwoId);
        });
        addressNodes.Should().ContainSingle().And.SatisfyRespectively(
            e =>
            {
                e.Key.Should().Be("http://local/receive/data");
                e.Value.Should().HaveCount(2).And.Subject.Select(_ => _.Id).Should().Contain(endpointId1).And.Contain(endpointId2);
            });
    }

}

internal static class InitializeUnspecificTreeTests
{
    internal static string ComposeAddress(IReadOnlyCollection<INode> nodes)
        => "http://local/" + string.Join("/", nodes.Select(e => e.Name));
}
