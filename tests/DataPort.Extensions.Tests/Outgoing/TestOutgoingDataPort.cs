using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Outgoing;
using ViciOne.Suite.DataPort.Extensions.Resilience;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Minimal test implementation of OutgoingDataPortBase.
/// Used for direct base class testing (~100 lines).
/// </summary>
#pragma warning disable CA1515 // types are used by NSubstitute and cannot be avoided
public class TestOutgoingDataPort
    : OutgoingDataPortBase<TestDataPoint, TestDataPointValue, TestClient>
#pragma warning restore CA1515 // types are used by NSubstitute and cannot be avoided
{
    /// <summary>
    /// Gets the test client for test assertions.
    /// </summary>
    public TestClient TestClient { get; }

    public TestOutgoingDataPort(
        TestCommunication communication,
        IWriteClient<TestDataPointValue> writeClient,
        ILogger<TestOutgoingDataPort> logger,
        IDelayProvider? delayProvider = null,
        TimeProvider? timeProvider = null,
        IRetryPolicy? retryPolicy = null)
        : base(
            dataPoints: communication.DataPoints,
            channelSelector: dp => [dp.ChannelName],
            queueCapacity: communication.MaxPendingMessages,
            overflowStrategy: communication.OverflowStrategy,
            logger: logger,
            delayProvider: delayProvider,
            timeProvider: timeProvider)
    {
        WriteClient = writeClient;
        TestClient = new TestClient { IsConnected = false, ConnectionCount = 0 };
        RetryPolicy = retryPolicy ?? new InfiniteRetryPolicy();
    }

    // Strategy implementations (dependency injection)
    protected override IWriteClient<TestDataPointValue> WriteClient { get; }
    protected override IRetryPolicy RetryPolicy { get; }

    protected override IDataPointValueFactory<TestDataPoint, TestDataPointValue> ValueFactory
        => new TestValueFactory();

    // Connection lifecycle (simple test implementations)
    protected override Task<TestClient> AcquireConnectionAsync(CancellationToken cancellationToken)
    {
        TestClient.IsConnected = true;
        TestClient.ConnectionCount++;
        return Task.FromResult(TestClient);
    }

    protected override Task ReleaseConnectionAsync(CancellationToken cancellationToken)
    {
        TestClient.IsConnected = false;
        return Task.CompletedTask;
    }

    protected override Task ValidateConnectionAsync(CancellationToken cancellationToken)
    {
        if (!TestClient.IsConnected)
            throw new InvalidOperationException("Not connected");
        return Task.CompletedTask;
    }
}
