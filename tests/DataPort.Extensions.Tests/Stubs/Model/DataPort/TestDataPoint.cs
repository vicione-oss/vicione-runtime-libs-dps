using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;

/// <summary>
/// Shared base for test data points, holding the protocol-agnostic metadata common to every data type.
/// Concrete subclasses declare their value's .NET type via <see cref="ITypedDataPoint{TValue}"/>.
/// </summary>
internal abstract record TestDataPoint : IPollingDataPoint
{
    public string? Description { get; init; }
    public Channels Channels { get; init; } = new([], []);
    public DataPointIdentifier Identifier { get; init; } = new(string.Empty);
    public DataTypeName DataTypeName { get; init; } = new(string.Empty);
    public PollFrequency PollFrequency { get; init; } = PollFrequency.FromMilliseconds(100);

    /// <summary>
    /// Converts an untyped value into a <see cref="DataPointValueConversion{TDataPointValue}"/> for this data point,
    /// producing a <see cref="NotConvertedDataPointValue{TDataPointValue}"/> with a descriptive failure if the value
    /// does not match the data point's declared .NET type.
    /// </summary>
    public abstract DataPointValueConversion<IDataPointValue> ConvertValue(object? value);
}

internal abstract record TestDataPoint<TValue> : TestDataPoint, ITypedDataPoint<TValue>
{
    public abstract ITypedDataPointValue<TValue> CreateTypedValue(TValue value);

    public override DataPointValueConversion<IDataPointValue> ConvertValue(object? value) => value switch
    {
        null => Failure("value was null"),
        TValue typed => new ConvertedDataPointValue<IDataPointValue>(CreateTypedValue(typed)),
        _ => Failure($"value of type {value.GetType()} is not assignable to {typeof(TValue)}")
    };

    private NotConvertedDataPointValue<IDataPointValue> Failure(string detail)
        => new(new DataPointValueValidationFailure(
            this,
            ValidationFailureReason.ConversionFailure,
            $"Could not convert value at {Identifier} to {DataTypeName.Value}: {detail}"));
}
