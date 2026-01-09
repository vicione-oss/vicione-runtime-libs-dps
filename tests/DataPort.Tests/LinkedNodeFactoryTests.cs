using System;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class LinkedNodeFactory_Create
{
    [Fact]
    public void Should_create_linked_nodes()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), ParentId = null, };
        Node groupNode = new() { Id = Guid.NewGuid(), ParentId = rootNode.Id, };
        Node valueOneNode = new() { Id = Guid.NewGuid(), ParentId = groupNode.Id, };
        Node valueTwoNode = new() { Id = Guid.NewGuid(), ParentId = groupNode.Id, };
        Node valueThreeNode = new() { Id = Guid.NewGuid(), ParentId = rootNode.Id, };
        Node[] nodes = [rootNode, groupNode, valueOneNode, valueTwoNode, valueThreeNode];

        var result = LinkedNodeFactory.Create(nodes);

        var rootLinkedNode = result.Should().ContainSingle().Which;
        var groupLinkedNode = rootLinkedNode.Children.Should().Contain(n => n.Id == groupNode.Id).Which;
        var valueThreeLinkedNode = rootLinkedNode.Children.Should().Contain(n => n.Id == valueThreeNode.Id).Which;
        var valueOneLinkedNode = groupLinkedNode.Children.Should().Contain(n => n.Id == valueOneNode.Id).Which;
        var valueTwoLinkedNode = groupLinkedNode.Children.Should().Contain(n => n.Id == valueTwoNode.Id).Which;

        rootLinkedNode.Parent.Should().BeNull();
        rootLinkedNode.Children.Should()
                .HaveCount(2)
                .And.Contain(groupLinkedNode)
                .And.Contain(valueThreeLinkedNode);
        groupLinkedNode.Parent.Should().Be(rootLinkedNode);
        groupLinkedNode.Children.Should()
                .HaveCount(2)
                .And.Contain(valueOneLinkedNode)
                .And.Contain(valueTwoLinkedNode);
        valueOneLinkedNode.Parent.Should().Be(groupLinkedNode);
        valueOneLinkedNode.Children.Should().BeEmpty();
        valueTwoLinkedNode.Parent.Should().Be(groupLinkedNode);
        valueTwoLinkedNode.Children.Should().BeEmpty();
        valueThreeLinkedNode.Parent.Should().Be(rootLinkedNode);
        valueThreeLinkedNode.Children.Should().BeEmpty();
    }
}

public class LinkedNodeFactory_GetAllChildren
{
    [Fact]
    public void Should_return_all_children()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), ParentId = null, };
        Node groupNode = new() { Id = Guid.NewGuid(), ParentId = rootNode.Id, };
        Node valueOneNode = new() { Id = Guid.NewGuid(), ParentId = groupNode.Id, };
        Node valueTwoNode = new() { Id = Guid.NewGuid(), ParentId = groupNode.Id, };
        Node valueThreeNode = new() { Id = Guid.NewGuid(), ParentId = rootNode.Id, };
        Node[] nodes = [rootNode, groupNode, valueOneNode, valueTwoNode, valueThreeNode];
        var linkedNodes = LinkedNodeFactory.Create(nodes);

        var result = linkedNodes.First().GetAllChildren().ToArray();

        result.Should().HaveCount(5);
        result.Select(n => n.Id).Should().ContainInOrder(rootNode.Id, groupNode.Id, valueOneNode.Id, valueTwoNode.Id, valueThreeNode.Id);
    }

    [Fact]
    public void Should_return_all_children_matching_condition()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), ParentId = null, };
        Node groupNode = new() { Id = Guid.NewGuid(), ParentId = rootNode.Id, DesignId = "group" };
        Node valueOneNode = new() { Id = Guid.NewGuid(), ParentId = groupNode.Id, DesignId = "value" };
        Node valueTwoNode = new() { Id = Guid.NewGuid(), ParentId = groupNode.Id, };
        Node valueThreeNode = new() { Id = Guid.NewGuid(), ParentId = rootNode.Id, };
        Node[] nodes = [rootNode, groupNode, valueOneNode, valueTwoNode, valueThreeNode];
        var linkedNodes = LinkedNodeFactory.Create(nodes);

        var result = linkedNodes.First().GetAllChildren(new()
        {
            { 1, n => n.DesignId == "group" },
            { 2, n => n.DesignId == "value" },
        }).ToList();

        result.Should().HaveCount(3);
        result.Select(n => n.Id).Should().ContainInOrder(rootNode.Id, groupNode.Id, valueOneNode.Id);
    }
}
