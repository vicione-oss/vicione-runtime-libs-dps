using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

namespace ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

/// <summary>
/// Configuration interface for YAML testing infrastructure.
/// </summary>
/// <typeparam name="TCommunication">The DataPortCommunication type.</typeparam>
/// <typeparam name="TRootConfigurationNode">The root configuration node type.</typeparam>
public interface IYamlTestingConfig<TCommunication, TRootConfigurationNode>
    where TCommunication : DataPortCommunication
    where TRootConfigurationNode : IRootConfigurationNode<TCommunication>
{
    /// <summary>
    /// The YAML file name to load (e.g., "s7-absolute.yaml" or "s7-symbolic.yaml").
    /// </summary>
    static abstract string YamlFileName { get; }

    /// <summary>
    /// The typed node mapper for the YAML configuration.
    /// </summary>
    static abstract TypedNodeMapper<TCommunication, TRootConfigurationNode> Mapper { get; }

    /// <summary>
    /// C# properties on Communication class that don't map to YAML (internal computed properties).
    /// Default: empty set.
    /// </summary>
    static abstract HashSet<string> ExcludedCommunicationProperties { get; }
}
