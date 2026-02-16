using System;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.TypedNodes;

internal sealed record TestDataPointNode : IDataPointNode
{
    public LinkedNode OriginalNode { get; init; } = null!;
    public IConfigurationNode? Parent { get; set; }
    public Channels Channels { get; init; } = new([], []);
    public PollFrequency PollFrequency { get; init; } = new(TimeSpan.FromMilliseconds(100));
}

internal sealed record TestDataPointNodeWithProperties : IDataPointNode
{
    public required LinkedNode OriginalNode { get; init; }
    public IConfigurationNode? Parent { get; set; }
    public string DataValue { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}
