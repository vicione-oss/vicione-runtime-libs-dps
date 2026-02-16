using System.Linq;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Outgoing.ChannelResolution;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

public class ChannelResolverTests
{
    [Fact]
    public void SingleChannelDataPoint_ResolvesCorrectly()
    {
        var resolver = new ChannelResolver<TestDataPoint>([new TestDataPoint("DP1", "CH1")]);

        var resolution = resolver.Resolve([new ExternalValue { Channel = "CH1" }]);

        resolution.ResolvedChannels.Should().ContainSingle()
            .Which.DataPoint.Identifier.Value.Should().Be("DP1");
    }

    [Fact]
    public void MultiChannelDataPoint_AllChannelsAreIndexed()
    {
        var resolver = new ChannelResolver<TestDataPoint>([new TestDataPoint("DP1", "CH1", "CH2")]);

        var resolution = resolver.Resolve(
        [
            new ExternalValue { Channel = "CH1" },
            new ExternalValue { Channel = "CH2" }
        ]);

        resolution.ResolvedChannels.Should().HaveCount(2)
            .And.AllSatisfy(r => r.DataPoint.Identifier.Value.Should().Be("DP1"));
    }

    [Fact]
    public void MultipleDataPoints_ResolveIndependently()
    {
        var resolver = new ChannelResolver<TestDataPoint>(
        [
            new TestDataPoint("DP1", "CH1", "CH2"),
            new TestDataPoint("DP2", "CH3")
        ]);

        var resolution = resolver.Resolve(
        [
            new ExternalValue { Channel = "CH1" },
            new ExternalValue { Channel = "CH2" },
            new ExternalValue { Channel = "CH3" }
        ]);

        resolution.ResolvedChannels.Should().HaveCount(3);
        resolution.ResolvedChannels[0].DataPoint.Identifier.Value.Should().Be("DP1");
        resolution.ResolvedChannels[1].DataPoint.Identifier.Value.Should().Be("DP1");
        resolution.ResolvedChannels[2].DataPoint.Identifier.Value.Should().Be("DP2");
    }

    [Fact]
    public void DuplicateChannel_LastDataPointWins()
    {
        var resolver = new ChannelResolver<TestDataPoint>(
        [
            new TestDataPoint("DP1", "CH1"),
            new TestDataPoint("DP2", "CH1") // Duplicate
        ]);

        var resolution = resolver.Resolve([new ExternalValue { Channel = "CH1" }]);

        resolution.ResolvedChannels.Should().ContainSingle()
            .Which.DataPoint.Identifier.Value.Should().Be("DP2", "last data point overwrites");
    }

    [Fact]
    public void NoDataPoints_ChannelIsUnresolved()
    {
        var resolver = new ChannelResolver<TestDataPoint>([]);

        var resolution = resolver.Resolve([new ExternalValue { Channel = "CH1" }]);

        resolution.ResolvedChannels.Should().BeEmpty();
        resolution.UnresolvedChannels.Should().ContainSingle();
    }

    [Fact]
    public void DataPointWithNoChannels_ChannelIsUnresolved()
    {
        var resolver = new ChannelResolver<TestDataPoint>([new TestDataPoint("DP1")]);

        var resolution = resolver.Resolve([new ExternalValue { Channel = "CH1" }]);

        resolution.ResolvedChannels.Should().BeEmpty();
        resolution.UnresolvedChannels.Should().ContainSingle();
    }

    private class TestDataPoint(string id, params string[] channels) : IDataPoint
    {
        public Channels Channels { get; init; } = new(
            [],
            [..channels.Select(c => new TransferredChannel(c))]);
        public DataPointIdentifier Identifier { get; } = new(id);
        public DataTypeName DataTypeName { get; } = new(string.Empty);
    }
}
