using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

/// <summary>
/// Represents a branch (intermediate) configuration node in the typed node hierarchy that has an original untyped node.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IBranchConfigurationNode : IConfigurationNode
{
    /// <summary>
    /// Gets the original untyped node that this typed configuration node was created from.
    /// </summary>
    LinkedNode OriginalNode { get; init; }
}
