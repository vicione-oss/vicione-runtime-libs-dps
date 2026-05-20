using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort.DataPoints.Int;

/// <summary>A test data point whose value is represented by an <see cref="int"/>.</summary>
internal record IntDataPoint : TestDataPoint<int>
{
    public override ITypedDataPointValue<int> CreateTypedValue(int value) => new IntDataPointValue(this, value);

    private readonly record struct IntDataPointValue(IDataPoint DataPoint, int TypedValue) : ITypedDataPointValue<int>
    {
        public bool IsInValueRange() => true;
    }
}
