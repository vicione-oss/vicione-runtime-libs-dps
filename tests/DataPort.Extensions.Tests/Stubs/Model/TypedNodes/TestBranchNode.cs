using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.TypedNodes;

internal sealed record TestBranchNode : IBranchConfigurationNode
{
    public required LinkedNode OriginalNode { get; init; }
    public IConfigurationNode? ParentConfigurationNode { get; set; }
    public List<IConfigurationNode> ConfigurationNodes { get; } = [];
    public List<IDataPointNode> DataPointNodes { get; } = [];

    public bool CanBeAdded(IConfigurationNode configurationNode) => true;
    public bool CanBeAdded(IDataPointNode dataPointNode) => true;
}

internal sealed record TestBranchNodeWithProperties : IBranchConfigurationNode
{
    public required LinkedNode OriginalNode { get; init; }
    public IConfigurationNode? ParentConfigurationNode { get; set; }
    public List<IConfigurationNode> ConfigurationNodes { get; } = [];
    public List<IDataPointNode> DataPointNodes { get; } = [];
    public string CustomProperty1 { get; set; } = string.Empty;
    public int CustomProperty2 { get; set; }

    public bool CanBeAdded(IConfigurationNode configurationNode) => true;
    public bool CanBeAdded(IDataPointNode dataPointNode) => true;
}
