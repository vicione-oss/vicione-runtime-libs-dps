#pragma warning disable CA1515 // Public for YamlConsistencyTest type parameters

using ViciOne.ManagedEngine.Communication;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;

/// <summary>
/// Minimal test-only communication configuration supporting both incoming and outgoing data ports.
/// </summary>
[Communication("TestDataPort")]
public record TestCommunication : DataPortCommunication
{
    public string IpAddress { get; init; } = "192.168.1.100";
    public int Port { get; init; } = 102;
    public QueueConfiguration QueueConfiguration { get; init; } = new(QueueStrategy.DropOldest, new QueueSize(10));
    public string ConnectionString => $"{IpAddress}:{Port}";
}
