using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Folders;

internal sealed record TestBranchNodeWithProperties : IBranchConfigurationNode, IHasFullPath
{
    public required LinkedNode OriginalNode { get; init; }
    public IConfigurationNode? ParentConfigurationNode { get; set; }
    public List<IConfigurationNode> ConfigurationNodes { get; } = [];
    public List<IDataPointNode> DataPointNodes { get; } = [];
    public string CustomProperty1 { get; set; } = string.Empty;
    public int CustomProperty2 { get; set; }
    public string FullPath => $"{ParentConfigurationNode}/{CustomProperty1}";

    public bool CanBeAdded(IConfigurationNode configurationNode) => true;
    public bool CanBeAdded(IDataPointNode dataPointNode) => true;
}
