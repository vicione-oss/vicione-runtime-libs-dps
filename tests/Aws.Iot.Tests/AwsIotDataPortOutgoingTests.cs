using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.ManagedEngine.Runtime;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AwsIotDataPortOutgoing_
{
    [Fact]
    public async Task DependencyInjectionProviderFactory_can_create_instance_Async()
    {
        await using var providerFactory = new ConstructorProviderFactory();

        var instance = providerFactory.CreateExternalOutgoing(
            new AwsIotDataPortCommunication(),
            new(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<INameResolver>(),
            AssemblyLoadContext.Default);

        instance.Should().NotBeNull().And.BeOfType<AwsIotDataPortOutgoing>();
    }

    [Collection(AwsIotTestEnvironment.Name)]
    public class Interoperability(AwsIotTestSystem system)
    {
        [Theory(Skip = "credentials required")]
        [InlineData(null)]
        [InlineData("myShadow")]
        [Trait("Category", "Interoperability")]
        [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped")]
        public async Task Can_update_reported_value_in_shadow(string? shadowName)
        {
            var thingsNodeId = Guid.NewGuid();
            var thingNodeId = Guid.NewGuid();
            var thingName = thingNodeId.ToString().Replace("-", string.Empty, StringComparison.InvariantCulture);
            var shadowNodeId = Guid.NewGuid();
            var reportedNodeId = Guid.NewGuid();
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
                    Id = reportedNodeId,
                    DesignId = AwsIotNodeDesignId.Reported,
                    ParentId = shadowNodeId,
                },
                new()
                {
                    Id = jsonPropertyNodeId,
                    DesignId = AwsIotNodeDesignId.JsonStructure,
                    ParentId = reportedNodeId,
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

            using AwsIotDataPortOutgoing outgoing = new(system.Communication with { Nodes = nodes, }, Substitute.For<ILoggerFactory>());

            await outgoing.ConnectAsync(CancellationToken.None);

            try
            {
                await outgoing.SendAsync(1, [
                        new()
                        {
                            Channel = "value",
                            Value = 112,
                        },
                    ], CancellationToken.None);
                await Task.Delay(1_000);
                (await system.GetShadow(thingName, shadowName))
                    ["state"]!
                    ["reported"]!
                    ["property"]!
                    ["value"]!
                    .GetValue<int>()
                    .Should().Be(112);
            }
            finally
            {
                await outgoing.DisconnectAsync(CancellationToken.None);
                await system.DetachPrincipals(thingName);
                await system.DeleteThing(thingName);
            }
        }
    }
}
