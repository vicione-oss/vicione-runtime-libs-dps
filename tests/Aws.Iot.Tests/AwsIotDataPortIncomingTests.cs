using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AwsIotDataPortIncoming_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using var providerFactory = new ConstructorProviderFactory();

        var instance = providerFactory.CreateExternalIncoming(
            new AwsIotDataPortCommunication(),
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<AwsIotDataPortIncoming>();
    }

    [Collection(AwsIotTestEnvironment.Name)]
    public class Interoperability(AwsIotTestSystem system)
    {
        [Theory(Skip = "credentials required")]
        [InlineData(null)]
        [InlineData("myShadow")]
        [Trait("Category", "Interoperability")]
        [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped")]
        public async Task Can_get_desired_value_from_thing_shadow(string? shadowName)
        {
            var thingsNodeId = Guid.NewGuid();
            var thingNodeId = Guid.NewGuid();
            var thingName = thingNodeId.ToString().Replace("-", string.Empty, StringComparison.InvariantCulture);
            var shadowNodeId = Guid.NewGuid();
            var desiredNodeId = Guid.NewGuid();
            var jsonPropertyNodeId = Guid.NewGuid();
            var valueNodeId = Guid.NewGuid();

            Node[] nodes =
            [
                new()
                {
                    Id = thingsNodeId,
                    DesignId = AwsIotNodeDesignId.Things,
                },
                new()
                {
                    Id = thingNodeId,
                    DesignId = AwsIotNodeDesignId.Thing,
                    ParentId = thingsNodeId,
                    Name = thingName,
                },
                new()
                {
                    Id = shadowNodeId,
                    DesignId = shadowName is null ? AwsIotNodeDesignId.ClassicDeviceShadow : AwsIotNodeDesignId.NamedDeviceShadow,
                    ParentId = thingNodeId,
                    Name = shadowName ?? string.Empty,
                    TransferredChannels = [ "value" ],
                },
                new()
                {
                    Id = desiredNodeId,
                    DesignId = AwsIotNodeDesignId.Desired,
                    ParentId = shadowNodeId,
                },
                new()
                {
                    Id = jsonPropertyNodeId,
                    DesignId = AwsIotNodeDesignId.JsonStructure,
                    ParentId = desiredNodeId,
                    Name = "property",
                },
                new()
                {
                    Id = valueNodeId,
                    DesignId = AwsIotNodeDesignId.JsonStructure,
                    ParentId = jsonPropertyNodeId,
                    Name = "value",
                    ValueType = typeof(int),
                    AffectedChannels = [ "value" ],
                },
            ];

            using AwsIotDataPortIncoming incoming = new(system.Communication with { Nodes = nodes, }, Substitute.For<ILoggerFactory>(), AssemblyLoadContext.Default);
            ConcurrentBag<ExternalValue> externalValues = [];
            incoming.Received += v =>
            {
                foreach (var value in v)
                    externalValues.Add(value);
            };
            await incoming.ConnectAsync(TestContext.Current.CancellationToken);
            var start = DateTime.UtcNow;

            try
            {
                await Task.Delay(1_000, TestContext.Current.CancellationToken);
                await system.UpdateShadow(thingName, new()
                {
                    ["desired"] = new JsonObject
                    {
                        ["property"] = new JsonObject
                        {
                            ["value"] = 112,
                        },
                    },
                }, shadowName);
                await Task.Delay(1_000, TestContext.Current.CancellationToken);
            }
            finally
            {
                await incoming.DisconnectAsync(TestContext.Current.CancellationToken);
                await system.DetachPrincipals(thingName);
                await system.DeleteThing(thingName);
            }

            var value = externalValues.Should().ContainSingle().Which;
            value.Channel.Should().Be("value");
            value.Value.Should().Be(112);
            value.Validity.Should().Be(1);
            value.Timestamp.Should().BeAfter(start).And.BeBefore(DateTime.UtcNow);
        }
    }
}
