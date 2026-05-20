using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints.String.Mapping;

internal sealed class StringNodeMapper : DataPointNodeMapperBase<StringNode>
{
    public override string TargetLinkedNodeTypeId => "String";

    protected override StringNode CreateNode(LinkedNode node, PollFrequency pollFrequency)
    {
        var value = node.GetRequiredPropertyValue<string>(StringNode.StringNodePropertyId);
        return new StringNode(node, pollFrequency, new StringProperty(value));
    }
}
