using System.Collections.Generic;

namespace ViciOne.Suite.DataPort;

public interface IDataPortCommunication
{
    string Name { get; }
    string DesignId { get; }
    IReadOnlyCollection<Node> Nodes { get; }
}
