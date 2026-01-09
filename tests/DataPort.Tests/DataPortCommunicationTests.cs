using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using ViciOne.ManagedEngine.Communication;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class DataPortCommunication_
{
    [Fact]
    public void Can_be_serialized_and_deserialized()
    {
        MyCommunication communication = new()
        {
            Nodes =
            [
                new Node()
                {
                    AffectedChannels = ["a"],
                    DesignId = "id",
                    Id = Guid.NewGuid(),
                    Name = "name",
                    ParentId = Guid.NewGuid(),
                    Properties = new()
                    {
                        { "NodeProperty", new() { Value = true, Channels = ["a"], } },
                    },
                    TransferredChannels = ["a"],
                    ValueType = typeof(int),
                }
            ],
        };

        JsonSerializerOptions settings = new()
        {
            Converters = { new TypeJsonConverter(), },
        };
        var json = JsonSerializer.Serialize(new ContainerToSerialize(communication), settings);
        var deserializedCommunication = JsonSerializer.Deserialize<ContainerToDeserialize>(json, settings);

        deserializedCommunication!.Communication.Should().BeEquivalentTo(communication);
    }

    internal sealed record class MyCommunication : DataPortCommunication;

    internal sealed class ContainerToSerialize(object? communication)
    {
        [JsonConverter(typeof(TypeNameHandlingConverter))]
        public object? Communication { get; } = communication;
    }

    [SuppressMessage("Performance", "CA1812:Nicht instanziierte interne Klassen vermeiden.")]
    internal sealed class ContainerToDeserialize
    {
        [JsonConverter(typeof(TypeNameHandlingConverter))]
        public ICommunication? Communication { get; set; }
    }
}
