using System;
using System.Linq;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.TypedNodes;

internal sealed record TestDataPointNode(LinkedNode OriginalNode) : IDataPointNode
{
    public IConfigurationNode? Parent { get; set; }
    public PollFrequency PollFrequency { get; init; } = new(TimeSpan.FromMilliseconds(100));

    public Channels Channels { get; } = new(
        [.. OriginalNode.AffectedChannels.Select(static ac => new AffectedChannel(ac))],
        [.. OriginalNode.TransferredChannels.Select(static ac => new TransferredChannel(ac))]
    );
}

internal sealed record TestDataPointNodeWithProperties(LinkedNode OriginalNode) : IDataPointNode
{
    public IConfigurationNode? Parent { get; set; }
    public string DataValue { get; set; } = string.Empty;
    public long Timestamp { get; set; }

    public Channels Channels { get; } = new(
        [.. OriginalNode.AffectedChannels.Select(static ac => new AffectedChannel(ac))],
        [.. OriginalNode.TransferredChannels.Select(static ac => new TransferredChannel(ac))]
    );
}
