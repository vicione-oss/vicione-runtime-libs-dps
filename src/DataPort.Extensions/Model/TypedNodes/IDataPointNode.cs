using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

/// <summary>
/// Represents a data point node in the typed node hierarchy, which is a leaf node containing actual data point information.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IDataPointNode : ITypedNode
{
    /// <summary>
    /// Gets or sets the parent configuration node that this data point belongs to.
    /// </summary>
    IConfigurationNode? Parent { get; set; }

    /// <summary>
    /// Gets the original untyped node that this typed data point node was created from.
    /// </summary>
    LinkedNode OriginalNode { get; }
}
