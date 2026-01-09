using System.Collections.Generic;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.Suite.DataPort;

public abstract record class DataPortCommunication : ICommunication, IDataPortCommunication
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<Node> Nodes { get; init; } = [];
}
