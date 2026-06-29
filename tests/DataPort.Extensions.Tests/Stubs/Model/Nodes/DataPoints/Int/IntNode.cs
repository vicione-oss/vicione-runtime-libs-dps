using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.Int;

internal sealed record IntNode(LinkedNode OriginalNode, PollFrequency PollFrequency, IntNodeProperty IntNodeProperty)
    : DataPointNodeBase(OriginalNode, PollFrequency)
{
    public const string IntNodePropertyId = nameof(IntNodeProperty);
}

internal record struct IntNodeProperty(string Value);
