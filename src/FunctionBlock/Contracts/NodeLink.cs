using System;

namespace ViciOne.Suite.System.Contracts;

public record NodeLink
{
    public required Guid InputConnectorId { get; init; }
    public required Guid OutputConnectorId { get; init; }
    public string? PropertyPath { get; init; }
    public NodeLinkModification? Modification { get; init; }
}

public enum NodeLinkModification
{
    Extract,
    Compose
}
