namespace ViciOne.Suite.DataPort.Extensions.TypedNodes;

/// <summary>
/// Represents the root configuration node in the typed node hierarchy, containing the original communication instance
/// that was used create this node and all its children.
/// </summary>
/// <typeparam name="TCommunication">The type of the data port communication configuration.</typeparam>
public interface IRootConfigurationNode<TCommunication> : IConfigurationNode
    where TCommunication : DataPortCommunication
{
    /// <summary>
    /// Gets the original communication configuration that this typed node hierarchy was created from.
    /// This property enforces that all root configuration nodes must provide access to their original communication.
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global - Required for interface contract enforcement
    TCommunication OriginalCommunication { get; init; }
}
