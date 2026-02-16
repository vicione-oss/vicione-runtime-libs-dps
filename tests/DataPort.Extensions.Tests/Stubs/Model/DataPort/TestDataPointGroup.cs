using System.Collections.Immutable;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;

/// <summary>
/// Test implementation of data point polling group for incoming data port tests.
/// </summary>
internal sealed class TestDataPointGroup : IDataPointPollingGroup<TestDataPoint>
{
    public required IImmutableList<TestDataPoint> DataPoints { get; init; }
    public required PollFrequency PollFrequency { get; init; }
}
