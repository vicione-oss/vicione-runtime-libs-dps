using FluentValidation.Results;

namespace ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;

/// <summary>
/// Defines a mapper that creates the root typed configuration node from a data port communication configuration.
/// </summary>
/// <typeparam name="TRootConfigurationNode">The type of root configuration node this mapper produces.</typeparam>
/// <typeparam name="TCommunication">The type of data port communication configuration.</typeparam>
public interface IRootConfigurationNodeMapper<out TRootConfigurationNode, in TCommunication>
    where TRootConfigurationNode : IRootConfigurationNode<TCommunication>
    where TCommunication : DataPortCommunication
{
    /// <summary>
    /// Creates a root typed configuration node from the specified communication configuration.
    /// </summary>
    /// <param name="communication">The communication configuration to convert.</param>
    /// <returns>A typed root configuration node.</returns>
    TRootConfigurationNode CreateRootNode(TCommunication communication);

    /// <summary>
    /// Validates the specified communication configuration against the mapping rules.
    /// </summary>
    /// <param name="communication">The communication configuration to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(TCommunication communication);
}
