using FluentValidation.Results;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Folders.Mapping;

internal sealed class TestFolderNodeMapper
    : IBranchConfigurationNodeMapper<TestBranchNodeWithProperties>
{
    public string TargetLinkedNodeTypeId => "Folder";

    public bool IsTargetMapperFor(LinkedNode linkedNode)
        => linkedNode.DesignId == TargetLinkedNodeTypeId && !linkedNode.HasProperty("GroupName");

    public TestBranchNodeWithProperties Map(LinkedNode node) => new()
    {
        OriginalNode = node,
        CustomProperty1 = node.GetRequiredPropertyValue<string>("CustomProperty1"),
        CustomProperty2 = node.GetRequiredPropertyValue<int>("CustomProperty2")
    };

    public ValidationResult Validate(LinkedNode linkedNode) => new();
}
