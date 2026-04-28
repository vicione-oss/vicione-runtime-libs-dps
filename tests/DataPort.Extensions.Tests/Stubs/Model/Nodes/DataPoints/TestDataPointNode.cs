using System;
using System.Linq;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints;

internal sealed record TestDataPointNode(LinkedNode OriginalNode) : IDataPointNode
{
    public IConfigurationNode? Parent { get; set; }
    public PollFrequency PollFrequency { get; init; } = new(TimeSpan.FromMilliseconds(100));

    public Channels Channels { get; } = new(
        [.. OriginalNode.AffectedChannels.Select(static ac => new AffectedChannel(ac))],
        [.. OriginalNode.TransferredChannels.Select(static ac => new TransferredChannel(ac))]
    );
}
