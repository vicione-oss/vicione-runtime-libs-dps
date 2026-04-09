using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class HttpServerDataPortOutgoing_
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

        var instance = providerFactory.CreateExternalOutgoing(
            communication,
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<HttpServerDataPortOutgoing>();
    }

    [Fact]
    public async Task Can_create_endpoint()
    {
        TestLogger<HttpServerDataPortOutgoing> logger = new();
        var part = Guid.NewGuid();
        var endopoint = Guid.NewGuid();
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
                Id = endopoint,
                ParentId = part,
                Name = "get",
                DesignId = HttpServerNodeDesignId.HttpGetEndpoint,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParentId = endopoint,
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

        var outgoing = new HttpServerDataPortOutgoing(communication, logger, manager);

        await outgoing.ConnectAsync(TestContext.Current.CancellationToken);

        manager.GetEndpoints.Should().ContainSingle().Which.Should().Be("data");
    }

    [Fact]
    public async Task Can_create_endpoint_with_complex_structure()
    {
        TestLogger<HttpServerDataPortOutgoing> logger = new();
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
                Name = "get",
                DesignId = HttpServerNodeDesignId.HttpGetEndpoint,
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

        var incoming = new HttpServerDataPortOutgoing(communication, logger, manager);

        await incoming.ConnectAsync(TestContext.Current.CancellationToken);

        manager.GetEndpoints.Should().ContainSingle().Which.Should().Be("endpoint");
    }
}
