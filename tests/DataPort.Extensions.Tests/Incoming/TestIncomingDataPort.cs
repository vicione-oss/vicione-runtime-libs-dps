using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extensions.Incoming;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.TypedNodes;

namespace ViciOne.Suite.DataPort.Extension.Tests.Incoming;

/// <summary>
/// Test implementation of IncomingDataPortBase for unit testing.
/// Provides minimal concrete implementations of all abstract methods.
/// </summary>
internal sealed class TestIncomingDataPort : IncomingDataPortBase<TestCommunication, TestDeviceNode, TestDataPointGroup,
    TestDataPointValue, TestClient>
{
    internal TestIncomingDataPort(
        TestDeviceNode deviceNode,
        List<TestDataPointGroup> dataPointGroups,
        ILoggerFactory loggerFactory,
        IConnectionManager<TestClient> connectionManager,
        IDataPointValidator<IDataPoint>? validator = null,
        TimeProvider? timeProvider = null)
        : base(deviceNode, dataPointGroups, loggerFactory.CreateLogger<TestIncomingDataPort>(), loggerFactory,
            connectionManager, validator, timeProvider)
    {
    }

    protected override IReadClient<TestDataPointGroup, TestDataPointValue> CreateReadClient(TestClient client) =>
        client.ReadClient;

    protected override string GetDeviceAddress(TestDeviceNode deviceNode) =>
        deviceNode.DeviceAddress;

    protected override IReadOnlyList<IDataPoint> GetAllDataPoints(IReadOnlyList<TestDataPointGroup> dataPointGroups)
    {
        var capacity = 0;
        for (var i = 0; i < dataPointGroups.Count; i++)
            capacity += dataPointGroups[i].DataPoints.Count;

        var result = new List<IDataPoint>(capacity);
        for (var i = 0; i < dataPointGroups.Count; i++)
            result.AddRange(dataPointGroups[i].DataPoints);

        return result;
    }

    protected override int GetTransferredChannelsCount(TestDataPointValue dataPointValue) =>
        dataPointValue.TransferredChannels.Count;

    protected override void AddExternalValuesForDataPoint(
        List<ExternalValue> result,
        TestDataPointValue dataPointValue,
        DateTime timestamp)
    {
        foreach (var channel in dataPointValue.TransferredChannels)
        {
            result.Add(new ExternalValue { Channel = channel, Value = dataPointValue.Value, Timestamp = timestamp });
        }
    }

    protected override TimeSpan GetPollFrequency(TestDataPointGroup group) =>
        group.PollFrequency;
}

// Test data types

internal record TestCommunication : DataPortCommunication;

internal record TestDeviceNode(string DeviceAddress, TestCommunication OriginalCommunication)
    : IRootConfigurationNode<TestCommunication>
{
    public IConfigurationNode? ParentConfigurationNode
    {
        get => null;
        set => throw new InvalidOperationException("TestDeviceNode cannot have a parent.");
    }

    public List<IConfigurationNode> ConfigurationNodes { get; } = [];
    public List<IDataPointNode> DataPointNodes { get; } = [];

    public bool CanBeAdded(IConfigurationNode configurationNode) => false;
    public bool CanBeAdded(IDataPointNode dataPointNode) => false;
}

internal sealed class TestDataPoint : IDataPoint
{
    public required Channels Channels { get; init; }
    public string AddressString { get; } = string.Empty;
    public string DataTypeString { get; } = string.Empty;
}

#pragma warning disable CA1515 // types are used by NSubstitute and cannot be avoided
public sealed class TestDataPointGroup
{
    public required List<IDataPoint> DataPoints { get; init; }
    public required TimeSpan PollFrequency { get; init; }
}

public sealed class TestDataPointValue
{
    public required List<string> TransferredChannels { get; init; }
    public required object? Value { get; init; }
}

public sealed class TestClient : IDisposable
{
    public required IReadClient<TestDataPointGroup, TestDataPointValue> ReadClient { get; init; }

    public void Dispose()
    {
    }
}
#pragma warning restore CA1515 // types are used by NSubstitute and cannot be avoided
