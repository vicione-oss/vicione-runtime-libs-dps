using System;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class JsonNodeExtensions_GetParentJsonNode
{
    [Fact]
    public void Returns_parent_json_node()
    {
        Node groupNode = new()
        {
            Id = Guid.NewGuid(),
            Name = "group",
            ParentId = null,
        };
        Node valueNode = new()
        {
            Id = Guid.NewGuid(),
            Name = "value",
            ParentId = groupNode.Id,
        };
        JsonObject jObject = [];

        var parent = JsonNodeExtensions.GetParentJsonNode([groupNode, valueNode,], jObject);

        parent.GetPath().Should().Be("$.group");
        jObject.Should().ContainSingle();
    }

    [Fact]
    public void Returns_original_json_object()
    {
        Node valueNode = new()
        {
            Name = "value",
        };
        JsonObject jObject = [];

        var obj = JsonNodeExtensions.GetParentJsonNode([valueNode,], jObject);

        ((object)obj).Should().Be(jObject);
    }
}
