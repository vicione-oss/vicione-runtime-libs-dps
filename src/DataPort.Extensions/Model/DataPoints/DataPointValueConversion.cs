namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// The outcome of converting a single external value into a strongly-typed data point value:
/// either the converted value (<see cref="ConvertedDataPointValue{TDataPointValue}"/>) or a description
/// of why the conversion could not be performed (<see cref="NotConvertedDataPointValue{TDataPointValue}"/>).
/// </summary>
/// <typeparam name="TDataPointValue">The strongly-typed data point value type produced on success.</typeparam>
/// <remarks>
/// The hierarchy is closed to the two cases declared in this assembly. Replaces the previous pattern of
/// returning a nullable value, which silently lost the reason for a failure.
/// TODO: Replace this abstract-record-with-private-protected-ctor workaround with native C# union types
/// once the language feature ships and the minimum TFM supports it.
/// </remarks>
public abstract record DataPointValueConversion<TDataPointValue> where TDataPointValue : IDataPointValue
{
    private protected DataPointValueConversion() { }
}

/// <summary>The external value was successfully converted into a data point value.</summary>
/// <param name="Value">The converted, strongly-typed data point value.</param>
public sealed record ConvertedDataPointValue<TDataPointValue>(TDataPointValue Value)
    : DataPointValueConversion<TDataPointValue> where TDataPointValue : IDataPointValue;

/// <summary>The external value could not be represented as the data point's declared .NET type.</summary>
/// <param name="Failure">The reason the conversion did not succeed.</param>
public sealed record NotConvertedDataPointValue<TDataPointValue>(DataPointValueValidationFailure Failure)
    : DataPointValueConversion<TDataPointValue> where TDataPointValue : IDataPointValue;
