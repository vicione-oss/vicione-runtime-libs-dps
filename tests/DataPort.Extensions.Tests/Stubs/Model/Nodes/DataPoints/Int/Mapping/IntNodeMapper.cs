using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.Int.Mapping;

internal sealed class IntNodeMapper : DataPointNodeMapperBase<IntNode>
{
    public override string TargetLinkedNodeTypeId => "Int";

    protected override IntNode CreateNode(LinkedNode node, PollFrequency pollFrequency)
    {
        var value = node.GetRequiredPropertyValue<string>(IntNode.IntNodePropertyId);
        return new IntNode(node, pollFrequency, new IntNodeProperty(value));
    }
}
