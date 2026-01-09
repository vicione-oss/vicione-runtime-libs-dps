using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using AwesomeAssertions;
using ViciOne.ManagedEngine.ExternalCommunication;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class NodeValueFactory_CreateJsonStructure
{
    [Fact]
    public void Acceptance()
    {
        Dictionary<string, INode[]> channelNodeRoutes = new()
        {
            { "A898103C-E586-4FF7-A3DD-408B1FB60D6B", new Node[]
                {
                    new() { Name = "House", },
                    new() { Name = "Temperature", },
                }
            },
            { "31C432F1-AA6F-4575-8B67-08CCB56613DD", new Node[]
                {
                    new() { Name = "House", },
                    new() { Name = "Kitchen", },
                    new() { Name = "Temperature", },
                }
            },
        };
        var values = new ExternalValue[]
        {
            new()
            {
                Channel = "A898103C-E586-4FF7-A3DD-408B1FB60D6B",
                Value = 10,
            },
            new()
            {
                Channel = "31C432F1-AA6F-4575-8B67-08CCB56613DD",
                Value = 33,
            },
        };

        var (jsonObject, meta) = NodeValueFactory.CreateJsonStructure(channelNodeRoutes, values);

        jsonObject.ToJsonString(new() { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver(), }).Should().Be($$"""
        {
          "House": {
            "Temperature": 10,
            "Kitchen": {
              "Temperature": 33
            }
          }
        }
        """);
        meta.ToJsonString(new() { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver(), }).Should().Be($$"""
        {
          "House": {
            "Temperature": "{{typeof(int).AssemblyQualifiedName}}",
            "Kitchen": {
              "Temperature": "{{typeof(int).AssemblyQualifiedName}}"
            }
          }
        }
        """);
    }

    [Fact]
    public void Can_handle_complex_type()
    {
        Dictionary<string, INode[]> channelNodeRoutes = new()
        {
            { "A898103C-E586-4FF7-A3DD-408B1FB60D6B", new Node[]
                {
                    new() { Name = "House", },
                    new() { Name = "Temperature", },
                }
            },
        };
        var values = new ExternalValue[]
        {
            new()
            {
                Channel = "A898103C-E586-4FF7-A3DD-408B1FB60D6B",
                Value = new MyValue { MyValueProperty = 112, },
            },
        };

        var (jsonObject, meta) = NodeValueFactory.CreateJsonStructure(channelNodeRoutes, values);

        jsonObject.ToJsonString(new() { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver(), }).Should().Be($$"""
        {
          "House": {
            "Temperature": {
              "MyValueProperty": 112
            }
          }
        }
        """);
        meta.ToJsonString(new() { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver(), }).Should().Be($$"""
        {
          "House": {
            "Temperature": "{{JavaScriptEncoder.Default.Encode(typeof(MyValue).AssemblyQualifiedName!)}}"
          }
        }
        """);
    }

    internal sealed class MyValue
    {
        public int MyValueProperty { get; set; }
    }
}

public class NodeValueFactory_IncludeMeta
{
    [Fact]
    public void Acceptance()
    {
        Dictionary<string, INode[]> channelNodeRoutes = new()
        {
            { "A898103C-E586-4FF7-A3DD-408B1FB60D6B", new Node[]
                {
                    new() { Name = "House", },
                    new() { Name = "Temperature", },
                }
            },
        };
        var values = new ExternalValue[]
        {
            new()
            {
                Channel = "A898103C-E586-4FF7-A3DD-408B1FB60D6B",
                Value = 112,
            },
        };

        var jsonObject = NodeValueFactory.CreateJsonStructure(channelNodeRoutes, values).IncludeMeta();

        jsonObject.ToJsonString(new() { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver(), }).Should().Be($$"""
        {
          "House": {
            "Temperature": 112
          },
          "$meta": {
            "House": {
              "Temperature": "{{typeof(int).AssemblyQualifiedName}}"
            }
          }
        }
        """);
    }
}

public class NodeValueFactory_GetValueJsonNode
{
    [Fact]
    public void Returns_value_json_node()
    {
        Node rootNode = new() { Id = Guid.NewGuid(), Name = "root", };
        Node groupNode = new() { Id = Guid.NewGuid(), Name = "group", ParentId = rootNode.Id, };
        Node valueNode = new() { Id = Guid.NewGuid(), Name = "value", ParentId = groupNode.Id, };
        var jObject = JsonNode.Parse("""
        {
            "group":
            {
                "value": 23
            }
        }
        """);

        var node = NodeValueFactory.GetValueJsonNode([groupNode, valueNode,], jObject);

        node.Should().NotBeNull();
        node!.GetValue<int>().Should().Be(23);
        node!.GetPath().Should().Be("$.group.value");
    }

    [Fact]
    public void Can_handle_null()
    {
        var node = NodeValueFactory.GetValueJsonNode([], null);

        node.Should().BeNull();
    }

    [Fact]
    public void Returns_value_node_without_structure()
    {
        var jValue = JsonNode.Parse("23");

        var node = NodeValueFactory.GetValueJsonNode([], jValue);

        node.Should().Be(jValue);
    }
}
