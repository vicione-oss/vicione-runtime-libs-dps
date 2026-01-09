using System;
using System.Collections.Generic;

namespace ViciOne.Suite.DataPort;

public sealed class LinkedNode(INode node) : INode
{
    public LinkedNode? Parent { get; init; }
    public List<LinkedNode> Children { get; init; } = [];

    public Guid Id => node.Id;
    public Guid? ParentId => node.ParentId;
    public string DesignId => node.DesignId;
    public Dictionary<string, Property> Properties => node.Properties;
    public string Name => node.Name;
    public List<string> AffectedChannels => node.AffectedChannels;
    public List<string> TransferredChannels => node.TransferredChannels;
    public Type? ValueType => node.ValueType;
}
