using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

/// <summary>
/// Extension methods for node mappers to support YAML configuration consistency tests.
/// </summary>
internal static class NodeMapperExtensions
{
    extension<T>(IBranchConfigurationNodeMapper<T> mapper) where T : IBranchConfigurationNode
    {
        /// <summary>
        /// Gets the properties defined on the branch configuration node type,
        /// excluding base IConfigurationNode and IBranchConfigurationNode properties.
        /// </summary>
        internal IReadOnlyList<PropertyInfo> GetPropertiesDefinedInTargetNode()
            => [.. mapper.NodeType.GetProperties()
                .ExceptBy(
                    typeof(IConfigurationNode).GetProperties()
                        .Concat(typeof(IBranchConfigurationNode).GetProperties())
                        .Select(static p => p.Name),
                    static p => p.Name)];

        /// <summary>
        /// Gets the property names defined on the branch configuration node type,
        /// excluding base IConfigurationNode and IBranchConfigurationNode properties.
        /// </summary>
        internal IReadOnlyList<string> PropertyNamesDefinedInTargetNode()
            => [.. mapper.GetPropertiesDefinedInTargetNode().Select(static p => p.Name)];
    }

    extension<T>(IDataPointNodeMapper<T> mapper) where T : IDataPointNode
    {
        /// <summary>
        /// Gets the properties defined on the data point node type,
        /// excluding base IDataPointNode properties.
        /// </summary>
        internal IReadOnlyList<PropertyInfo> GetPropertiesDefinedInTargetNode()
            => [.. mapper.NodeType.GetProperties()
                .ExceptBy(
                    typeof(IDataPointNode).GetProperties().Select(static p => p.Name),
                    static p => p.Name)];

        /// <summary>
        /// Gets the property names defined on the data point node type,
        /// excluding base IDataPointNode properties.
        /// </summary>
        internal IReadOnlyList<string> PropertyNamesDefinedInTargetNode()
            => [.. mapper.GetPropertiesDefinedInTargetNode().Select(static p => p.Name)];
    }
}
