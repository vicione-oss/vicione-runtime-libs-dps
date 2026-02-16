using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentValidation.Results;

namespace ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

/// <summary>
/// Defines a mapper that converts untyped LinkedNodes to typed data point nodes.
/// </summary>
/// <typeparam name="T">The type of data point node this mapper produces.</typeparam>
public interface IDataPointNodeMapper<out T> where T : IDataPointNode
{
    /// <summary>
    /// Gets the LinkedNode type identifier (design ID) that this mapper targets for mapping.
    /// </summary>
    string TargetLinkedNodeTypeId { get; }

    /// <summary>
    /// Gets the CLR type of the data point node produced by this mapper.
    /// </summary>
    Type NodeType => typeof(T);

    /// <summary>
    /// Maps an untyped LinkedNode to a typed data point node.
    /// </summary>
    /// <param name="node">The untyped node to map.</param>
    /// <returns>A typed data point node.</returns>
    T Map(LinkedNode node);

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

    /// <summary>
    /// Gets the properties defined on the node type, excluding base IDataPointNode properties.
    /// </summary>
    List<PropertyInfo> GetPropertiesDefinedInTargetNode() =>
        NodeType.GetProperties()
            .ExceptBy(BasePropertyNames(), p => p.Name)
            .ToList();

    /// <summary>
    /// Gets the names of properties defined on the node type, excluding base IDataPointNode properties.
    /// </summary>
    List<string> PropertyNamesDefinedInTargetNode() =>
        GetPropertiesDefinedInTargetNode()
            .Select(p => p.Name)
            .ToList();

    private static List<string> BasePropertyNames() =>
        typeof(IDataPointNode).GetProperties().Select(p => p.Name).ToList();
}
