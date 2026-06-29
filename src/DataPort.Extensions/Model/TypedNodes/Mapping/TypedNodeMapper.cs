using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Suite.DataPort.Extensions.Exceptions;

namespace ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

/// <summary>
/// Orchestrates the mapping of flat YAML configuration nodes into a strongly-typed hierarchical node structure.
/// Processes nodes recursively, validating each node and establishing bidirectional parent-child relationships.
/// Supports both configuration nodes (branches that can contain children) and data point nodes (leaves).
/// </summary>
/// <typeparam name="TCommunication">The communication configuration type containing the node definitions.</typeparam>
/// <typeparam name="TRootConfigurationNode">The root node type for the hierarchy.</typeparam>
public class TypedNodeMapper<TCommunication, TRootConfigurationNode>(
    IRootConfigurationNodeMapper<TRootConfigurationNode, TCommunication> rootMapper,
    IReadOnlyList<IBranchConfigurationNodeMapper<IBranchConfigurationNode>> configurationNodeMappers,
    IReadOnlyList<IDataPointNodeMapper<IDataPointNode>> dataPointNodeMappers)
    where TCommunication : DataPortCommunication
    where TRootConfigurationNode : IRootConfigurationNode<TCommunication>
{
    /// <summary>
    /// Maps the communication configuration to a hierarchical structure of typed nodes.
    /// This is the entry point for building the entire node hierarchy from YAML configuration.
    /// </summary>
    /// <param name="communication">The data port communication configuration containing the node definitions.</param>
    /// <returns>The root configuration node with all child nodes attached.</returns>
    /// <exception cref="InvalidConfigurationException">Thrown when validation fails for any node in the hierarchy.</exception>
    public TRootConfigurationNode MapToTypedNodes(TCommunication communication)
    {
        var validationResult = rootMapper.Validate(communication);

        if (!validationResult.IsValid)
        {
            throw new InvalidConfigurationException(communication, validationResult.Errors);
        }

        var deviceNode = rootMapper.CreateRootNode(communication);
        var typedNodes = ConvertNodesToTypedNodes([.. communication.Nodes]);
        AddTypedNodes(deviceNode, typedNodes);
        return deviceNode;
    }

    /// <summary>
    /// Adds top-level typed nodes to the root configuration node.
    /// Separates configuration nodes (branches) from data point nodes (leaves) and adds them accordingly.
    /// </summary>
    private static void AddTypedNodes(IConfigurationNode rootConfiguration, IReadOnlyList<ITypedNode> typedNodes)
    {
        foreach (var configNode in typedNodes.OfType<IConfigurationNode>())
        {
            rootConfiguration.AddConfiguration(configNode);
        }

        foreach (var dataPointNode in typedNodes.OfType<IDataPointNode>())
        {
            rootConfiguration.AddDataPoint(dataPointNode);
        }
    }

    /// <summary>
    /// Converts a flat list of YAML nodes to a list of typed nodes with parent-child relationships.
    /// First creates LinkedNodes to establish parent-child structure, then recursively maps each to its typed representation.
    /// </summary>
    private IReadOnlyList<ITypedNode> ConvertNodesToTypedNodes(List<Node> nodes)
    {
        var linkedNodes = LinkedNodeFactory.Create(nodes);
        return [.. linkedNodes.Select(MapToTypedNode)];
    }

    /// <summary>
    /// Recursively maps a linked node to its corresponding typed node representation.
    /// For configuration nodes (branches), recursively processes all children.
    /// For data point nodes (leaves), directly maps without children.
    /// </summary>
    /// <exception cref="InvalidConfigurationException">Thrown when node validation fails.</exception>
    /// <exception cref="NotSupportedException">Thrown when no suitable mapper is found for the node.</exception>
    private ITypedNode MapToTypedNode(LinkedNode linkedNode)
    {
        var configurationNodeMapper = FindConfigurationNodeMapper(linkedNode);
        var dataPointNodeMapper = FindDataPointNodeMapper(linkedNode);

        if (configurationNodeMapper != null)
        {
            var validationResult = configurationNodeMapper.Validate(linkedNode);
            if (!validationResult.IsValid)
            {
                throw new InvalidConfigurationException(linkedNode, validationResult.Errors);
            }

            return MapToConfigurationNode(linkedNode, configurationNodeMapper);
        }

        if (dataPointNodeMapper != null)
        {
            var validationResult = dataPointNodeMapper.Validate(linkedNode);
            if (!validationResult.IsValid)
            {
                throw new InvalidConfigurationException(linkedNode, validationResult.Errors);
            }

            return dataPointNodeMapper.Map(linkedNode);
        }

        throw new NotSupportedException($"No mapper found for linked node: {linkedNode}.");
    }

    /// <summary>
    /// Maps a configuration node and recursively processes all its children.
    /// This method is the core of the recursive hierarchy building:
    /// 1. Maps the parent configuration node
    /// 2. Recursively maps all children (which may have their own children)
    /// 3. Establishes bidirectional parent-child relationships
    /// </summary>
    private IConfigurationNode MapToConfigurationNode(LinkedNode linkedNode,
        IBranchConfigurationNodeMapper<IBranchConfigurationNode> branchConfigurationNodeMapper)
    {
        var configurationNode = branchConfigurationNodeMapper.Map(linkedNode);
        var childNodes = linkedNode.Children.Select(MapToTypedNode).ToList();

        AttachChildNodesToParent(configurationNode, childNodes);

        return configurationNode;
    }

    /// <summary>
    /// Establishes bidirectional parent-child relationships in the node hierarchy.
    /// For configuration nodes: sets parent reference and adds to parent's configuration children.
    /// For data point nodes: adds to parent's data point children (no parent reference needed for leaves).
    /// </summary>
    private static void AttachChildNodesToParent(IConfigurationNode parentNode, IReadOnlyList<ITypedNode> childNodes)
    {
        foreach (var childConfigNode in childNodes.OfType<IConfigurationNode>())
        {
            childConfigNode.ParentConfigurationNode = parentNode;
            parentNode.AddConfiguration(childConfigNode);
        }

        foreach (var dataPointNode in childNodes.OfType<IDataPointNode>())
        {
            parentNode.AddDataPoint(dataPointNode);
        }
    }

    private IDataPointNodeMapper<IDataPointNode>? FindDataPointNodeMapper(LinkedNode linkedNode)
        => dataPointNodeMappers.FirstOrDefault(mapper => mapper.IsTargetMapperFor(linkedNode));

    private IBranchConfigurationNodeMapper<IBranchConfigurationNode>? FindConfigurationNodeMapper(LinkedNode linkedNode)
        => configurationNodeMappers.FirstOrDefault(mapper => mapper.IsTargetMapperFor(linkedNode));

    /// <summary>
    /// Gets the collection of branch configuration node mappers used for mapping configuration (branch) nodes.
    /// </summary>
    public IReadOnlyList<IBranchConfigurationNodeMapper<IBranchConfigurationNode>> ConfigurationNodeMappers
        => configurationNodeMappers;

    /// <summary>
    /// Gets the collection of data point node mappers used for mapping data point (leaf) nodes.
    /// </summary>
    public IReadOnlyList<IDataPointNodeMapper<IDataPointNode>> DataPointNodeMappers => dataPointNodeMappers;
}
