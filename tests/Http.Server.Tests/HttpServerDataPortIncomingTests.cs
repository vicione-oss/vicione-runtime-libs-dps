using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class HttpServerDataPortIncoming_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using var providerFactory = new ConstructorProviderFactory();
        HttpServerDataPortCommunication communication = new();
        _ = new HttpServerDataPortProperties(communication)
        {
            BaseAddress = "http://localhost:5050",
        };

        var instance = providerFactory.CreateExternalIncoming(
            communication,
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<HttpServerDataPortIncoming>();
    }

    [Fact]
    public async Task Can_create_endpoint()
    {
        TestLogger<HttpServerDataPortIncoming> logger = new();
        var part = Guid.NewGuid();
        var endpoint = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
            {
                Id = part,
                Name = "data",
                DesignId = HttpServerNodeDesignId.PathPart,
            },
            new()
            {
                Id = endpoint,
                ParentId = part,
                Name = "post",
                DesignId = HttpServerNodeDesignId.HttpPostEndpoint,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParentId = endpoint,
                Name = "data",
                AffectedChannels = { "v1", },
                TransferredChannels = { "v1", },
                ValueType = typeof(int?),
                DesignId = HttpServerNodeDesignId.Path,
            },
        ];

        HttpServerDataPortCommunication communication = new()
        {
            Nodes = nodes,
        };

        _ = new HttpServerDataPortProperties(communication)
        {
            BaseAddress = "http://local",
        };

        var manager = new FakeHttpListenerManager();

        var incoming = new HttpServerDataPortIncoming(communication, logger, AssemblyLoadContext.Default, manager);

        await incoming.ConnectAsync(CancellationToken.None);

        manager.PostEndpoints.Should().ContainSingle().Which.Should().Be("data");
    }

    [Fact]
    public async Task Can_create_endpoint_with_complex_structure()
    {
        TestLogger<HttpServerDataPortIncoming> logger = new();
        var part = Guid.NewGuid();
        var endpoint = Guid.NewGuid();
        var json = Guid.NewGuid();
        List<Node> nodes =
        [
            new()
            {
                Id = part,
                Name = "endpoint",
                DesignId = HttpServerNodeDesignId.PathPart,
            },
            new()
            {
                Id = endpoint,
                ParentId = part,
                Name = "post",
                DesignId = HttpServerNodeDesignId.HttpPostEndpoint,
            },
            new()
            {
                Id = json,
                ParentId = endpoint,
                Name = "json",
                AffectedChannels = { "v1", },
                TransferredChannels = { "v1", },
                DesignId = HttpServerNodeDesignId.JsonStructure,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParentId = json,
                Name = "data2",
                ValueType = typeof(int?),
                DesignId = HttpServerNodeDesignId.Path,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParentId = json,
                Name = "data1",
                ValueType = typeof(int?),
                DesignId = HttpServerNodeDesignId.Path,
            },
        ];

        HttpServerDataPortCommunication communication = new()
        {
            Nodes = nodes,
        };

        _ = new HttpServerDataPortProperties(communication)
        {
            BaseAddress = "http://local",
        };

        var manager = new FakeHttpListenerManager();

        var incoming = new HttpServerDataPortIncoming(communication, logger, AssemblyLoadContext.Default, manager);

        await incoming.ConnectAsync(CancellationToken.None);

        manager.PostEndpoints.Should().ContainSingle().Which.Should().Be("endpoint");
    }

    public class HttpServerDataPortIncoming_ProcessJsonNode
    {
        [Fact]
        public void Acceptance()
        {
            var message = JsonNode.Parse("""
            {
                "Test": {
                    "Type": "System.Int32",
                    "Value": 23
                }
            }
            """);
            Node transferredNode = new()
            {
                TransferredChannels =
                {
                    "test",
                },
            };
            var route = new[]
            {
                new Node[]
                {
                    new()
                    {
                        Name = "Test",
                        AffectedChannels =
                        {
                            "test",
                        },
                    },
                },
            };

            var values = HttpServerDataPortIncoming.ProcessJsonNode(message, transferredNode, route, new JsonSerializerOptions());

            var value = values.Should().ContainSingle().Which;
            value.Channel.Should().Be("test");
            value.Value.Should().Be(23);
        }
    }
}
