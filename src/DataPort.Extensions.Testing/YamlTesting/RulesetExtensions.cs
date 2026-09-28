using System.Collections.Generic;
using System.Linq;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Tree.Builder.Rules;

namespace ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

/// <summary>
/// Provides extension methods for querying and analyzing YAML rulesets.
/// </summary>
public static class RulesetExtensions
{
    /// <summary>
    /// Gets the root identifier from the ruleset.
    /// </summary>
    public static string GetRootId(this Ruleset ruleset) => ruleset.Root?.Id ?? string.Empty;

    /// <summary>
    /// Gets all root-level data port nodes.
    /// </summary>
    public static List<DataPortNodeType> GetRootNodes(this Ruleset ruleset) =>
        [.. ruleset.NodeTypes.OfType<DataPortNodeType>()];

    /// <summary>
    /// Gets all configuration nodes (DataPortTreeNodes that have child nodes).
    /// </summary>
    public static List<DataPortTreeNodeType> GetConfigurationNodes(this Ruleset ruleset) =>
    [
        .. ruleset.NodeTypes
            .OfType<DataPortTreeNodeType>()
            .Where(x => x.ChildNodes.Length > 0)
    ];

    /// <summary>
    /// Gets all data point nodes (leaf nodes without children).
    /// </summary>
    public static List<DataPortTreeNodeType> GetDataPointNodes(this Ruleset ruleset) =>
    [
        .. ruleset.NodeTypes
            .OfType<DataPortTreeNodeType>()
            .Where(x => x.ChildNodes.Length == 0)
    ];

    /// <summary>
    /// Gets all property IDs defined in a node's property categories.
    /// </summary>
    public static List<string> GetPropertyIds(this NodeType node) =>
        [.. node.PropertyCategories.SelectMany(cat => cat.Properties.Select(prop => prop.Id))];

    /// <summary>
    /// Gets the mapping IDs of all configuration nodes.
    /// </summary>
    public static List<string> GetConfigurationNodesMappingIds(this Ruleset ruleset) =>
    [
        .. ruleset.GetConfigurationNodes()
            .Select(x => x.MappingId)
    ];

    /// <summary>
    /// Gets the mapping IDs of all data point nodes.
    /// </summary>
    public static List<string> GetDataPointNodesMappingIds(this Ruleset ruleset) =>
    [
        .. ruleset.GetDataPointNodes()
            .Select(x => x.MappingId)
    ];

    /// <summary>
    /// Finds a configuration node by its mapping ID.
    /// </summary>
    /// <returns>The matching node, or <c>null</c> if not found.</returns>
    public static DataPortTreeNodeType? GetConfigurationNodeByMappingId(this Ruleset ruleset, string mappingId) =>
        ruleset.GetConfigurationNodes().FirstOrDefault(node => node.MappingId == mappingId);

    /// <summary>
    /// Finds a data point node by its mapping ID.
    /// </summary>
    /// <returns>The matching node, or <c>null</c> if not found.</returns>
    public static DataPortTreeNodeType? GetDataPointNodeByMappingId(this Ruleset ruleset, string mappingId) =>
        ruleset.GetDataPointNodes().FirstOrDefault(node => node.MappingId == mappingId);
}
