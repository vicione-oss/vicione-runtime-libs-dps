
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class HttpClientDataPortIncoming_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using var providerFactory = new ConstructorProviderFactory();
        HttpClientDataPortCommunication communication = new();
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "http://local.com",
            PollInterval = TimeSpan.FromSeconds(2),
        };

        var instance = providerFactory.CreateExternalIncoming(
            communication,
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<HttpClientDataPortIncoming>();
    }

    [Fact]
    public async Task Polls_data_Async()
    {
        TestLogger<HttpClientDataPortIncoming> logger = new();
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
        var count = 0;
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
        using SubstituteHttpMessageHandler messageHandler = new(
            request =>
            {
                request.RequestUri!.ToString().Should().Be("http://local/data");
                request.Headers.Should().ContainSingle().And.ContainSingle(e => e.Key == "Test").Which.Value.Should().Contain("test");

                count++;
                semaphore.Release();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null"), };
            });
        FakeTimeProvider timeProvider = new();

        using var incoming = new HttpClientDataPortIncoming(communication, logger, AssemblyLoadContext.Default, () => new(messageHandler), timeProvider);

        await incoming.ConnectAsync(TestContext.Current.CancellationToken);
        timeProvider.Advance(500.Milliseconds());
        await semaphore.WaitAsync(100, TestContext.Current.CancellationToken);
        timeProvider.Advance(500.Milliseconds());
        await semaphore.WaitAsync(100, TestContext.Current.CancellationToken);
        await incoming.DisconnectAsync(TestContext.Current.CancellationToken);

        count.Should().Be(2);
        logger.Calls.Should().Be(0);
    }

    public class ProcessJsonNode
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

            var values = HttpClientDataPortIncoming.ProcessJsonNode(message, transferredNode, route, new JsonSerializerOptions());

            var value = values.Should().ContainSingle().Which;
            value.Channel.Should().Be("test");
            value.Value.Should().Be(23);
        }
    }
}
