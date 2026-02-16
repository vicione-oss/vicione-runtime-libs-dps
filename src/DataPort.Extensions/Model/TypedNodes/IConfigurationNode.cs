using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Suite.DataPort.Extensions.Exceptions;

namespace ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

/// <summary>
/// Represents a configuration node in the typed node hierarchy that can contain child configuration nodes and data points.
/// <para>
/// <strong>Mutability Note:</strong> Although implementing types are typically records, they contain mutable state
/// for establishing bidirectional parent-child relationships in the node tree. This mutability is intentional and
/// restricted to the construction phase. After construction, nodes should be treated as read-only.
/// </para>
/// </summary>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IConfigurationNode : ITypedNode
{
    /// <summary>
    /// Gets or sets the parent configuration node. Null for root nodes.
    /// <para>
    /// This property is mutable to support bidirectional tree traversal (e.g., building full symbolic paths).
    /// It is set during node construction and should not be modified afterward.
    /// </para>
    /// </summary>
    IConfigurationNode? ParentConfigurationNode { get; set; }

    /// <summary>
    /// Gets the list of child configuration nodes.
    /// <para>
    /// This collection is mutable to allow adding children during tree construction.
    /// After construction, it should be treated as read-only.
    /// </para>
    /// </summary>
    List<IConfigurationNode> ConfigurationNodes { get; }

    /// <summary>
    /// Gets the list of data point nodes directly attached to this configuration node.
    /// <para>
    /// This collection is mutable to allow adding data points during tree construction.
    /// After construction, it should be treated as read-only.
    /// </para>
    /// </summary>
    List<IDataPointNode> DataPointNodes { get; }

    /// <summary>
    /// Adds a data point node to this configuration node.
    /// </summary>
    /// <param name="dataPointNode">The data point node to add.</param>
    /// <exception cref="InvalidConfigurationException">Thrown when the data point cannot be added to this configuration node.</exception>
    void AddDataPoint(IDataPointNode dataPointNode)
    {
        if (CanBeAdded(dataPointNode))
        {
            DataPointNodes.Add(dataPointNode);
            dataPointNode.Parent = this;
        }
        else
        {
            throw new InvalidConfigurationException();
        }
    }

    /// <summary>
    /// Removes a data point node from this configuration node.
    /// </summary>
    /// <param name="dataPointNode">The data point node to remove.</param>
    void RemoveDataPoint(IDataPointNode dataPointNode)
    {
        DataPointNodes.Remove(dataPointNode);
        dataPointNode.Parent = null;
    }

    /// <summary>
    /// Adds a child configuration node to this configuration node.
    /// </summary>
    /// <param name="configurationNode">The configuration node to add.</param>
    /// <exception cref="InvalidConfigurationException">Thrown when the configuration node cannot be added to this configuration node.</exception>
    void AddConfiguration(IConfigurationNode configurationNode)
    {
        if (CanBeAdded(configurationNode))
        {
            ConfigurationNodes.Add(configurationNode);
            configurationNode.ParentConfigurationNode = this;
        }
        else
        {
            throw new InvalidConfigurationException();
        }
    }

    /// <summary>
    /// Removes a child configuration node from this configuration node.
    /// </summary>
    /// <param name="configurationNode">The configuration node to remove.</param>
    void RemoveConfiguration(IConfigurationNode configurationNode)
    {
        configurationNode.ParentConfigurationNode = null;
        ConfigurationNodes.Remove(configurationNode);
    }

    /// <summary>
    /// Determines whether the specified configuration node can be added as a child to this configuration node.
    /// </summary>
    /// <param name="configurationNode">The configuration node to check.</param>
    /// <returns>True if the configuration node can be added; otherwise, false.</returns>
    bool CanBeAdded(IConfigurationNode configurationNode);

    /// <summary>
    /// Determines whether the specified data point node can be added to this configuration node.
    /// </summary>
    /// <param name="dataPointNode">The data point node to check.</param>
    /// <returns>True if the data point node can be added; otherwise, false.</returns>
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    bool CanBeAdded(IDataPointNode dataPointNode);
}
