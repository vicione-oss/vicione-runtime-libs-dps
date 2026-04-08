using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class HttpClientDataPortOutgoing_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance()
    {
        await using var providerFactory = new ConstructorProviderFactory();
        HttpClientDataPortCommunication communication = new();
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "http://local.com",
        };

        var instance = providerFactory.CreateExternalOutgoing(
            communication,
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<HttpClientDataPortOutgoing>();
    }

    [Fact]
    public async Task Can_publish_single_value()
    {
        HttpClientDataPortCommunication communication = new()
        {
            Nodes =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "data",
                    AffectedChannels = { "v1", },
                    TransferredChannels = { "v1", },
                    ValueType = typeof(int?),
                    DesignId = HttpClientNodeDesignId.Path,
                    Properties = { { HttpClientNodeProperties.HeadersProperty, new() { Value = new Dictionary<string, string> { { "Test", "test" }, }, } } },
                },
            ],
        };
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "http://local",
            PollInterval = TimeSpan.FromMilliseconds(500),
        };
        TestLogger<HttpClientDataPortOutgoing> logger = new(LogLevel.Warning);
        HttpRequestMessage? requestMessage = null;
        string? payload = null;
        using SubstituteHttpMessageHandler messageHandler = new(request =>
        {
            requestMessage = request;
            payload = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var outgoing = new HttpClientDataPortOutgoing(communication, logger, () => new(messageHandler));

        await outgoing.ConnectAsync(TestContext.Current.CancellationToken);
        await outgoing.SendAsync(0, [new() { Channel = "v1", Value = 23, }], TestContext.Current.CancellationToken);
        await outgoing.DisconnectAsync(TestContext.Current.CancellationToken);

        logger.Calls.Should().Be(0);
        requestMessage.Should().NotBeNull();
        requestMessage.RequestUri.Should().Be("http://local/data");
        requestMessage.Headers.Should().ContainSingle().And.ContainKey("Test").WhoseValue.Should().Contain("test");
        requestMessage.Content.Should().NotBeNull();
        requestMessage.Content.Headers.ContentType.Should().NotBeNull();
        requestMessage.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        payload.Should().NotBeNull();
        payload.Should().Be("""{"Channel":"v1","Timestamp":"0001-01-01T00:00:00","Validity":0,"Value":{"Type":"System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e","Value":"23"}}""");
    }

    [Fact]
    public async Task Can_publish_complex_value()
    {
        Node groupNode = new()
        {
            Id = Guid.NewGuid(),
            Name = "group",
            ParentId = null,
            TransferredChannels = { "v", },
            DesignId = HttpClientNodeDesignId.Path,
        };
        Node valueNode = new()
        {
            Id = Guid.NewGuid(),
            Name = "value",
            ParentId = groupNode.Id,
            AffectedChannels = { "v", },
            DesignId = HttpClientNodeDesignId.Path,
        };
        HttpClientDataPortCommunication communication = new()
        {
            Nodes = [groupNode, valueNode,],
        };
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "http://local",
            PollInterval = TimeSpan.FromMilliseconds(500),
        };
        TestLogger<HttpClientDataPortOutgoing> logger = new(LogLevel.Warning);
        HttpRequestMessage? requestMessage = null;
        string? payload = null;
        using SubstituteHttpMessageHandler messageHandler = new(request =>
        {
            requestMessage = request;
            payload = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var outgoing = new HttpClientDataPortOutgoing(communication, logger, () => new(messageHandler));

        await outgoing.ConnectAsync(TestContext.Current.CancellationToken);
        await outgoing.SendAsync(0, [new() { Channel = "v", Value = 23, }], TestContext.Current.CancellationToken);
        await outgoing.DisconnectAsync(TestContext.Current.CancellationToken);

        logger.Calls.Should().Be(0);
        requestMessage.Should().NotBeNull();
        requestMessage.RequestUri.Should().Be("http://local/group");
        requestMessage.Headers.Should().BeEmpty();
        requestMessage.Content.Should().NotBeNull();
        requestMessage.Content.Headers.ContentType.Should().NotBeNull();
        requestMessage.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        payload.Should().NotBeNull();
        payload.Should().Be("""{"value":23,"$meta":{"value":"System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"}}""");
    }
}
