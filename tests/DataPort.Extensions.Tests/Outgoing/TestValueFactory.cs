using ViciOne.Suite.DataPort.Extensions.Outgoing;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Simple test-only value factory (no validation for tests).
/// </summary>
internal class TestValueFactory : IDataPointValueFactory<TestDataPoint, TestDataPointValue>
{
    public TestDataPointValue Create(TestDataPoint dataPoint, object? value)
    {
        return new TestDataPointValue
        {
            ChannelName = dataPoint.ChannelName,
            Value = value,
            DataPoint = dataPoint
        };
    }
}
