using System;
using FluentValidation.Results;

namespace ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

/// <summary>
/// Defines a mapper that converts untyped LinkedNodes to typed branch configuration nodes.
/// </summary>
/// <typeparam name="T">The type of branch configuration node this mapper produces.</typeparam>
public interface IBranchConfigurationNodeMapper<out T> where T : IBranchConfigurationNode
{
    /// <summary>
    /// Gets the LinkedNode type identifier (design ID) that this mapper targets for mapping.
    /// </summary>
    string TargetLinkedNodeTypeId { get; }

    /// <summary>
    /// Maps an untyped LinkedNode to a typed branch configuration node.
    /// </summary>
    /// <param name="node">The untyped node to map.</param>
    /// <returns>A typed branch configuration node.</returns>
    T Map(LinkedNode node);

    /// <summary>
    /// Gets the CLR type of the configuration node produced by this mapper.
    /// </summary>
    Type NodeType => typeof(T);

    /// <summary>
    /// Validates the specified node against the mapping rules.
    /// </summary>
    /// <param name="linkedNode">The node to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(LinkedNode linkedNode);

    /// <summary>
    /// Determines whether this mapper is the target mapper for the specified node based on its design ID.
    /// </summary>
    /// <param name="linkedNode">The node to check.</param>
    /// <returns>True if this mapper targets the node's design ID; otherwise, false.</returns>
    bool IsTargetMapperFor(LinkedNode linkedNode) => linkedNode.DesignId == TargetLinkedNodeTypeId;
}
