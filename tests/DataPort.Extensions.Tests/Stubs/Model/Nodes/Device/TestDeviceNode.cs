#pragma warning disable CA1515 // Public for YamlConsistencyTest type parameters

using System;
using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;

/// <summary>
/// Minimal test-only device node for unit testing, supporting both incoming and outgoing data ports.
/// </summary>
public sealed class TestDeviceNode : IRootConfigurationNode<TestCommunication>
{
    public TestCommunication OriginalCommunication { get; init; }
    public DeviceIdentifier DeviceIdentifier { get; init; }

    // Constructor for incoming tests
    public TestDeviceNode(string deviceIdentifier, TestCommunication originalCommunication)
    {
        DeviceIdentifier = new DeviceIdentifier(deviceIdentifier);
        OriginalCommunication = originalCommunication;
    }

    // Parameterless constructor for outgoing tests (property initializer syntax)
    public TestDeviceNode()
    {
        OriginalCommunication = new TestCommunication();
        DeviceIdentifier = new DeviceIdentifier("192.168.1.100");
    }

    public IConfigurationNode? ParentConfigurationNode
    {
        get => null;
        set => throw new InvalidOperationException("TestDeviceNode cannot have a parent.");
    }

    public List<IConfigurationNode> ConfigurationNodes { get; } = [];
    public List<IDataPointNode> DataPointNodes { get; } = [];

    public bool CanBeAdded(IConfigurationNode configurationNode) => true;
    public bool CanBeAdded(IDataPointNode dataPointNode) => true;
}
