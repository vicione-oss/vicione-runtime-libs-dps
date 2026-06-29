using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints;

/// <summary>
/// Shared base for data point configuration node stubs. Holds the members common to every
/// <see cref="IDataPointNode"/>, leaving subclasses to declare only their type-specific property.
/// </summary>
internal abstract record DataPointNodeBase(LinkedNode OriginalNode, PollFrequency PollFrequency) : IDataPointNode
{
    public const string PollFrequencyPropertyId = nameof(PollFrequency);

    public IConfigurationNode? Parent { get; set; }

    public Channels Channels { get; } = new(
        Channels.AffectedChannels([.. OriginalNode.AffectedChannels]),
        Channels.TransferredChannels([.. OriginalNode.TransferredChannels]));
}
