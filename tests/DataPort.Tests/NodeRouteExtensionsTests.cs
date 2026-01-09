using System;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class NodeRouteExtensions_GetRoute
{
    [Fact]
    public void Returns_route()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), Name = "root", };
        Node groupNode = new() { Id = Guid.NewGuid(), Name = "group", ParentId = rootNode.Id, };
        Node valueNode = new() { Id = Guid.NewGuid(), Name = "value", ParentId = groupNode.Id, };
        var nodes = new[] { rootNode, groupNode, valueNode, }.ToDictionary<Node, Guid, INode>(n => n.Id, n => n);

        var route = valueNode.GetRoute(nodes);

        string.Join("/", route.Select(n => n.Name)).Should().Be("root/group/value");
    }
}

public class NodeRouteExtensions_GetRoute_Node
{
    [Fact]
    public void Returns_route_until_node()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), Name = "root", };
        Node groupNode = new() { Id = Guid.NewGuid(), Name = "group", ParentId = rootNode.Id, };
        Node subgroupNode = new() { Id = Guid.NewGuid(), Name = "subgroup", ParentId = groupNode.Id, };
        Node valueNode = new() { Id = Guid.NewGuid(), Name = "value", ParentId = subgroupNode.Id, };
        var nodes = new[] { rootNode, groupNode, subgroupNode, valueNode, }.ToDictionary<Node, Guid, INode>(n => n.Id, n => n);

        var route = valueNode.GetRoute(groupNode, nodes);

        string.Join("/", route.Select(n => n.Name)).Should().Be("subgroup/value");
    }
}

public class NodeRouteExtensions_GetSuccessorRoutes
{
    [Fact]
    public void Returns_routes()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), Name = "root", };
        Node groupNode = new() { Id = Guid.NewGuid(), Name = "group", ParentId = rootNode.Id, };
        Node value1Node = new() { Id = Guid.NewGuid(), Name = "value1", ParentId = groupNode.Id, };
        Node subgroupNode = new() { Id = Guid.NewGuid(), Name = "subgroup", ParentId = groupNode.Id, };
        Node value2Node = new() { Id = Guid.NewGuid(), Name = "value2", ParentId = subgroupNode.Id, };
        Node value3Node = new() { Id = Guid.NewGuid(), Name = "value3", ParentId = subgroupNode.Id, };
        var nodes = new[] { rootNode, groupNode, subgroupNode, value1Node, value2Node, value3Node, };

        var routes = rootNode.GetSuccessorRoutes(nodes.CreateNodeParentsCache());

        routes.Select(r => string.Join("/", r.Select(n => n.Name)))
            .Should().HaveCount(3)
            .And.Contain("group/value1")
            .And.Contain("group/subgroup/value2")
            .And.Contain("group/subgroup/value3");
    }

    [Fact]
    public void Returns_an_empty_list_if_no_successors()
    {
        Node valueNode = new() { Name = "value", };

        var routes = valueNode.GetSuccessorRoutes([]);

        routes.Should().BeEmpty();
    }
}
