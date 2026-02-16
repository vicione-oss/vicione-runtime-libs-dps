using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;

/// <summary>
/// Minimal test-only data point value supporting both incoming and outgoing data ports.
/// </summary>
internal class TestDataPointValue : IDataPointValue<TestDataPoint>
{
    public required object? Value { get; init; }
    public TestDataPoint DataPoint { get; init; } = null!;
    public List<string> TransferredChannels { get; init; } = [];
    public bool IsInValueRange() => true;

    public TypeValidationResult ValidateType()
    {
        var type = Value?.GetType();

        return type is null ? new TypeValidationResult(false, null, []) : new TypeValidationResult(true, type, [type]);
    }
}
