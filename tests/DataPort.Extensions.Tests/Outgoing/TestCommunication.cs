using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Queue;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Minimal test-only communication configuration.
/// </summary>
#pragma warning disable CA1515 // types are used by NSubstitute and cannot be avoided
public class TestCommunication
#pragma warning restore CA1515 // types are used by NSubstitute and cannot be avoided
{
    public required string IpAddress { get; init; }
    public required int Port { get; init; }
    public required List<TestDataPoint> DataPoints { get; init; }
    public int MaxPendingMessages { get; init; } = 10;
    public QueueStrategy OverflowStrategy { get; init; } = QueueStrategy.DropOldest;
}
