using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Loader;
using System.Threading;
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
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
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
    public async Task Can_publish_Async()
    {
        TestLogger<HttpClientDataPortOutgoing> logger = new(LogLevel.Warning);
        List<Node> nodes =
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
        ];
        using SemaphoreSlim semaphore = new(0, 1);
        HttpClientDataPortCommunication communication = new()
        {
            Nodes = nodes,
        };
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "http://local",
            PollInterval = TimeSpan.FromMilliseconds(500),
        };
        HttpRequestMessage? requestMessage = null;
        using SubstituteHttpMessageHandler messageHandler = new(request =>
        {
            requestMessage = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var outgoing = new HttpClientDataPortOutgoing(communication, logger, () => new(messageHandler));

        await outgoing.ConnectAsync(TestContext.Current.CancellationToken);
        await outgoing.SendAsync(0, [new() { Channel = "v1", Value = 23, }], TestContext.Current.CancellationToken);
        await outgoing.DisconnectAsync(TestContext.Current.CancellationToken);

        logger.Calls.Should().Be(0);
        requestMessage!.RequestUri.Should().Be("http://local/data");
        requestMessage!.Headers.Should().ContainSingle().And.ContainSingle(e => e.Key == "Test").Which.Value.Should().Contain("test");
    }
}
