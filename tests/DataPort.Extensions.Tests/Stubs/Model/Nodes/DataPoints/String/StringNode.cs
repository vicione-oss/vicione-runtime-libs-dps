using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.Int;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.String;

/// <summary>
/// Configuration node for a string-valued data point. The counterpart to <see cref="IntNode"/>,
/// it is mapped to a <c>StringDataPoint</c> by <c>TestDataPointGroupsMapper</c>.
/// </summary>
internal sealed record StringNode(LinkedNode OriginalNode, PollFrequency PollFrequency, StringProperty StringProperty)
    : DataPointNodeBase(OriginalNode, PollFrequency)
{
    public const string StringNodePropertyId = nameof(StringProperty);
}

internal record struct StringProperty(string Value);
