using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Folders.Mapping;

internal sealed class TestGroupNodeMapper : IBranchConfigurationNodeMapper<TestGroupNode>
{
    public string TargetLinkedNodeTypeId => "Folder";

    public bool IsTargetMapperFor(LinkedNode linkedNode) =>
        linkedNode.DesignId == TargetLinkedNodeTypeId && linkedNode.HasProperty("GroupName");

    public TestGroupNode Map(LinkedNode node) => new()
    {
        OriginalNode = node,
        GroupName = node.GetRequiredPropertyValue<string>("GroupName"),
        CustomProperty1 = node.GetRequiredPropertyValue<string>("CustomProperty1"),
        CustomProperty2 = node.GetRequiredPropertyValue<int>("CustomProperty2")
    };

    public ValidationResult Validate(LinkedNode linkedNode) => new();
}
