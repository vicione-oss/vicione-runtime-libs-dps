using System;
using System.Collections.Generic;

namespace ViciOne.Suite.DataPort;

public class Node : INode
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string DesignId { get; init; } = string.Empty;

    public Dictionary<string, Property> Properties { get; init; } = [];

    public string Name { get; init; } = string.Empty;
    public List<string> AffectedChannels { get; init; } = [];
    public List<string> TransferredChannels { get; init; } = [];
    public Type? ValueType { get; init; }
}
