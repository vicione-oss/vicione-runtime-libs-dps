using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.ValueValidation;

namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Minimal test-only data point value.
/// </summary>
#pragma warning disable CA1515 // types are used by NSubstitute and cannot be avoided
public class TestDataPointValue : IDataPointValue<TestDataPoint>
#pragma warning restore CA1515 // types are used by NSubstitute and cannot be avoided
{
    public required string ChannelName { get; init; }
    public required object? Value { get; init; }
    public TestDataPoint DataPoint { get; init; } = null!;
    public bool IsInValueRange() => true;

    public TypeValidationResult ValidateType()
    {
        var type = Value?.GetType();

        if (type is null)
        {
            return new(false, null, []);
        }

        return new(true, type, [type]);
    }
}
