using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort.DataPoints.String;

/// <summary>A test data point whose value is represented by a <see cref="string"/>.</summary>
internal record StringDataPoint : TestDataPoint<string>
{
    public override ITypedDataPointValue<string> CreateTypedValue(string value) =>
        new StringDataPointValue(this, value);

    private readonly record struct StringDataPointValue(IDataPoint DataPoint, string TypedValue)
        : ITypedDataPointValue<string>
    {
        public bool IsInValueRange() => true;
    }
}
