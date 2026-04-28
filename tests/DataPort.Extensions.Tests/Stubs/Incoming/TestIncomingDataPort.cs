using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Client;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Mapping;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;
using ViciOne.Suite.DataPort.Extensions.Client;
using ViciOne.Suite.DataPort.Extensions.Incoming;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Verification;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Incoming;

internal class TestIncomingDataPort(
    TestCommunication communication,
    ILoggerFactory loggerFactory,
    TypedNodeMapper<TestCommunication, TestDeviceNode> typedNodeMapper,
    TestDataPointGroupsMapper dataPointGroupsMapper,
    IClientLifecycleManager<TestClient, TestClientInformation> connectionManager,
    TimeProvider? timeProvider = null,
    IDataPointConfigurationVerifier<TestDataPoint>? configurationVerifier = null,
    TestClientInformation? clientInformation = null)
    : IncomingDataPortBase<TestCommunication, TestDeviceNode, TestDataPoint, TestDataPointGroup, TestDataPointValue,
        TestClient, TestClientInformation>(communication, loggerFactory, typedNodeMapper, dataPointGroupsMapper, timeProvider)
{
    protected override IDataPointConfigurationVerifier<TestDataPoint>? CreateConfigurationVerifier(TestClient client) =>
        configurationVerifier;

    protected override IClientLifecycleManager<TestClient, TestClientInformation> ClientLifecycleManager { get; } =
        connectionManager;

    public override TestClientInformation ClientInformation { get; } =
        clientInformation ?? new TestClientInformation();

    protected override IReadOnlyList<ExternalValue> ToExternalValues(
        IReadOnlyList<TestDataPointValue> values, DateTime timestamp) =>
    [
        .. from dpv in values
        from channel in dpv.TransferredChannels
        select new ExternalValue
        {
            Channel = channel,
            Value = dpv.Value,
            Timestamp = timestamp
        }
    ];
}
