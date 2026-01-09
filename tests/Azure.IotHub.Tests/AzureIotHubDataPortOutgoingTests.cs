using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AzureIotHubDataPortOutgoing_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using var providerFactory = new ConstructorProviderFactory();

        var instance = providerFactory.CreateExternalOutgoing(
            new AzureIotHubDataPortCommunication(),
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<AzureIotHubDataPortOutgoing>();
    }

    [Collection(AzureIotHubTestEnvironment.Name)]
    public class Interoperability(AzureIotHubTestSystem system)
    {
        [Fact(Skip = "credentials required")]
        [Trait("Category", "Interoperability")]
        [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped")]
        public async Task Can_update_reported_value_in_device_twin()
        {
            var deviceNodeId = Guid.NewGuid();
            var deviceName = deviceNodeId.ToString().Replace("-", string.Empty, StringComparison.InvariantCulture);
            var propertiesNodeId = Guid.NewGuid();
            var reportedNodeId = Guid.NewGuid();
            var jsonPropertyNodeId = Guid.NewGuid();
            var valueNodeId = Guid.NewGuid();

            Node[] nodes =
            [
                new()
                {
                    Id = deviceNodeId,
                    DesignId = AzureIotHubNodeDesignId.Device,
                    Name = deviceName,
                },
                new()
                {
                    Id = propertiesNodeId,
                    DesignId = AzureIotHubNodeDesignId.Properties,
                    ParentId = deviceNodeId,
                    Name = "Properties",
                },
                new()
                {
                    Id = reportedNodeId,
                    DesignId = AzureIotHubNodeDesignId.Reported,
                    ParentId = propertiesNodeId,
                    Name = "Reported",
                    TransferredChannels = [ "value" ],
                },
                new()
                {
                    Id = jsonPropertyNodeId,
                    DesignId = AzureIotHubNodeDesignId.JsonStructure,
                    ParentId = reportedNodeId,
                    Name = "property",
                },
                new()
                {
                    Id = valueNodeId,
                    DesignId = AzureIotHubNodeDesignId.JsonStructure,
                    ParentId = jsonPropertyNodeId,
                    Name = "value",
                    ValueType = typeof(int),
                    AffectedChannels = [ "value" ],
                },
            ];

            await using AzureIotHubDataPortOutgoing outgoing = new(system.Communication with { Nodes = nodes, }, Substitute.For<ILogger<AzureIotHubDataPortOutgoing>>());

            await outgoing.ConnectAsync(CancellationToken.None);

            try
            {
                (await system.GetDeviceTwin(deviceName))
                    ["properties"]!
                    ["reported"]!
                    ["property"]!
                    ["value"]
                    .Should().BeNull();

                await outgoing.SendAsync(1, [
                        new()
                        {
                            Channel = "value",
                            Value = 112,
                        },
                    ], CancellationToken.None);

                (await system.GetDeviceTwin(deviceName))
                    ["properties"]!
                    ["reported"]!
                    ["property"]!
                    ["value"]!
                    .GetValue<int>()
                    .Should().Be(112);
            }
            finally
            {
                await outgoing.DisconnectAsync(CancellationToken.None);
                await system.DeleteDevice(deviceName);
            }
        }

        [Fact(Skip = "credentials required")]
        [Trait("Category", "Interoperability")]
        [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped")]
        public async Task Can_send_device_telemetry()
        {
            var deviceNodeId = Guid.NewGuid();
            var deviceName = deviceNodeId.ToString().Replace("-", string.Empty, StringComparison.InvariantCulture);
            var telemetryNodeId = Guid.NewGuid();
            var jsonPropertyNodeId = Guid.NewGuid();
            var valueNodeId = Guid.NewGuid();

            Node[] nodes =
            [
                new()
                {
                    Id = deviceNodeId,
                    DesignId = AzureIotHubNodeDesignId.Device,
                    Name = deviceName,
                },
                new()
                {
                    Id = telemetryNodeId,
                    DesignId = AzureIotHubNodeDesignId.Telemetry,
                    ParentId = deviceNodeId,
                    Name = "Telemetry",
                    TransferredChannels = [ "value" ],
                },
                new()
                {
                    Id = jsonPropertyNodeId,
                    DesignId = AzureIotHubNodeDesignId.JsonStructure,
                    ParentId = telemetryNodeId,
                    Name = "property",
                },
                new()
                {
                    Id = valueNodeId,
                    DesignId = AzureIotHubNodeDesignId.JsonStructure,
                    ParentId = jsonPropertyNodeId,
                    Name = "value",
                    ValueType = typeof(int),
                    AffectedChannels = [ "value" ],
                },
            ];

            await using AzureIotHubDataPortOutgoing outgoing = new(system.Communication with { Nodes = nodes, }, Substitute.For<ILogger<AzureIotHubDataPortOutgoing>>());

            await using EventHubConsumerClient client = new(EventHubConsumerClient.DefaultConsumerGroupName, system.EventHubConnectionString);
            ConcurrentBag<PartitionEvent> values = [];
            using CancellationTokenSource readCancellationTokenSource = new();
            var start = DateTimeOffset.UtcNow;
            var readTask = Task.Run(async () =>
            {
                await foreach (var @event in client.ReadEventsAsync(readCancellationTokenSource.Token))
                {
                    if (@event.Data.EnqueuedTime > start)
                        values.Add(@event);
                }
            });
            DateTime timestamp = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await outgoing.ConnectAsync(CancellationToken.None);

            try
            {
                await Task.Delay(1_000);
                await outgoing.SendAsync(1, [
                        new()
                        {
                            Channel = "value",
                            Value = 112,
                            Validity = 11,
                            Timestamp = timestamp,
                        },
                    ], CancellationToken.None);
                await Task.Delay(1_000);
            }
            finally
            {
                await outgoing.DisconnectAsync(CancellationToken.None);
                await system.DeleteDevice(deviceName);
                await readCancellationTokenSource.CancelAsync();
                try
                {
                    await readTask;
                }
                catch (OperationCanceledException)
                { }
            }

            var value = values.Should().ContainSingle().Which;
            var data = await JsonSerializer.DeserializeAsync<JsonObject>(value.Data.EventBody.ToStream());
            data!["property"]!["value"]!.GetValue<int>().Should().Be(112);
            value.Data.Properties["type"].Should().Be("{\"property\":{\"value\":\"System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"}}");
            value.Data.Properties["timestamp"].Should().Be(timestamp.ToString("O"));
            value.Data.Properties["validity"].Should().Be("11");
        }
    }
}
