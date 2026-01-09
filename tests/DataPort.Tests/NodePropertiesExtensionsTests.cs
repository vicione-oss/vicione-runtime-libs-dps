using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class NodePropertiesExtensions_GetPropertyValueOrDefault
{
    [Fact]
    public void Returns_value()
    {
        Node node = new()
        {
            Properties = new()
            {
                { "Count", new() { Value = 23 }}
            },
        };

        node.GetPropertyValueOrDefault<int>("Count", default).Should().Be(23);
    }

    [Fact]
    public void Returns_default_if_property_does_not_exist()
        => new Node().GetPropertyValueOrDefault<int>("Count", default).Should().Be(0);
}

public class NodePropertiesExtensions_GetProperty
{
    [Fact]
    public void Returns_first_level_property()
    {
        Node node = new()
        {
            Properties = new()
            {
                { "Count", new() { Value = 23 }}
            },
        };

        var property = node.GetProperty("Count");

        property!.Value.Should().Be(23);
    }

    [Fact]
    public void Returns_second_level_property()
    {
        Node node = new()
        {
            Properties = new()
            {
                {
                    "House", new()
                    {
                        Value = new Dictionary<string, Property>
                        {
                            { "Kitchen", new() { Value = 23 } },
                        },
                    }
                },
            },
        };

        var property = node.GetProperty("House.Kitchen");

        property!.Value.Should().Be(23);
    }

    [Fact]
    public void Throws_if_property_not_exists()
    {
        var act = () => new Node().GetProperty("Count");

        act.Should().Throw<InvalidOperationException>().WithMessage("*prop*Count*not*exist*");
    }

    [Fact]
    public void Throws_if_next_level_not_exists()
    {
        Node node = new()
        {
            Properties = new()
            {
                { "House", new() { Value = 0, } },
            },
        };

        var act = () => node.GetProperty("House.Kitchen");

        act.Should().Throw<InvalidOperationException>().WithMessage("*prop*House*not*level*Kitchen*");
    }
}

public class NodePropertiesExtensions_TryGetProperty
{
    [Fact]
    public void Returns_first_level_property()
    {
        Node node = new()
        {
            Properties = new()
            {
                { "Count", new() { Value = 23 }}
            },
        };

        node.TryGetProperty("Count", out var property).Should().BeTrue();

        property!.Value.Should().Be(23);
    }

    [Fact]
    public void Returns_second_level_property()
    {
        Node node = new()
        {
            Properties = new()
            {
                {
                    "House", new()
                    {
                        Value = new Dictionary<string, Property>
                        {
                            { "Kitchen", new() { Value = 23 } },
                        },
                    }
                },
            },
        };

        node.TryGetProperty("House.Kitchen", out var property).Should().BeTrue();

        property!.Value.Should().Be(23);
    }

    [Fact]
    public void Returns_false_if_property_not_exists()
    {
        var result = new Node().TryGetProperty("Count", out var _);

        result.Should().BeFalse();
    }

    [Fact]
    public void Returns_false_if_next_level_not_exists()
    {
        Node node = new()
        {
            Properties = new()
            {
                { "House", new() { Value = new Dictionary<string, Property>(), } },
            },
        };

        var result = node.TryGetProperty("House.Kitchen", out var _);

        result.Should().BeFalse();
    }
}

public class NodePropertiesExtensions_GetAllPropertyChannels
{
    [Fact]
    public void Returns_all_channels()
    {
        List<Node> nodes = [
            new()
                {
                    Properties =
                    {
                        { "Count", new() { Value = 0, Channels = { "property", }, } },
                        { "PropertyWithSubProperties", new() { Value = new Dictionary<string, Property>()
                            {
                                { "SubProperty", new() { Value = 0, Channels = { "subProperty1", "subProperty2" }, } },
                            },
                        } },
                    },
                },
            ];

        var channels = nodes.GetAllPropertyChannels().ToArray();

        channels.Should()
            .HaveCount(3)
            .And.Contain("property")
            .And.Contain("subProperty1")
            .And.Contain("subProperty2");
    }
}
