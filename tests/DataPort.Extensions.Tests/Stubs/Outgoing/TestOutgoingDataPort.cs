using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Client;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Client;
using ViciOne.Suite.DataPort.Extensions.Model.Mapping;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Outgoing;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry;
using ViciOne.Suite.DataPort.Extensions.Verification;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Outgoing;

/// <summary>
/// Minimal test implementation of OutgoingDataPortBase.
/// Used for direct base class testing.
/// </summary>
/// <remarks>
/// Minimal test implementation of OutgoingDataPortBase.
/// Used for direct base class testing.
/// </remarks>
internal class TestOutgoingDataPort(
    TestCommunication communication,
    TypedNodeMapper<TestCommunication, TestDeviceNode> typedNodeMapper,
    IDataPointsMapper<TestDeviceNode, TestCommunication, TestDataPoint> mapper,
    QueueConfiguration queueConfiguration,
    IClientLifecycleManager<TestClient, TestClientInformation> connectionManager,
    ILoggerFactory loggerFactory,
    IDelayProvider? delayProvider = null,
    IRetryPolicy? retryPolicy = null,
    IDataPointConfigurationVerifier<TestDataPoint>? configurationVerifier = null,
    TestClientInformation? clientInformation = null)
    : OutgoingDataPortBase<TestDataPoint, TestDataPointValue, TestClient, TestDeviceNode, TestCommunication,
        TestClientInformation>
    (communication, loggerFactory, typedNodeMapper, mapper, queueConfiguration,
        delayProvider ?? new DefaultDelayProvider())
{
    // Convenience method for tests: routes external values through the base class pipeline
    public Task EnqueueAsync(IReadOnlyList<ExternalValue> externalValues)
        => base.SendAsync(0, externalValues, CancellationToken.None);

    protected override IDataPointConfigurationVerifier<TestDataPoint>? CreateConfigurationVerifier(TestClient client) =>
        configurationVerifier;

    protected override IClientLifecycleManager<TestClient, TestClientInformation> ClientLifecycleManager { get; } =
        connectionManager;

    public override TestClientInformation ClientInformation { get; } =
        clientInformation ?? new TestClientInformation();

    protected override IRetryPolicy RetryPolicy { get; } = retryPolicy ?? new InfiniteRetryPolicy();

    protected override TestDataPointValue ConvertValue(ExternalValue externalValue, TestDataPoint dataPoint)
        => new()
        {
            Value = externalValue.Value,
            DataPoint = dataPoint
        };
}
