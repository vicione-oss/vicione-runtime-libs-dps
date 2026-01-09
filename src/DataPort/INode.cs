using System;
using System.Collections.Generic;

namespace ViciOne.Suite.DataPort;

public interface INode
{
    Guid Id { get; }
    Guid? ParentId { get; }
    string DesignId { get; }

    Dictionary<string, Property> Properties { get; }

    string Name { get; }
    List<string> AffectedChannels { get; }
    List<string> TransferredChannels { get; }
    Type? ValueType { get; }
}
