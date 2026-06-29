namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Defines a data point that declares the .NET type used to represent its value.
/// </summary>
/// <typeparam name="TValue">The .NET type a value of this data point is represented by.</typeparam>
/// <remarks>
/// The CLR type argument replaces the former enum/string discriminator as the source of truth
/// for a data point's data type, enabling strongly-typed <see cref="ITypedDataPointValue{TValue}"/> values.
/// </remarks>
public interface ITypedDataPoint<TValue> : IDataPoint
{
    /// <summary>
    /// Creates a strongly-typed <see cref="ITypedDataPointValue{TValue}"/> for this data point.
    /// </summary>
    /// <param name="value">The strongly-typed value to wrap.</param>
    /// <returns>A typed data point value carrying the supplied value.</returns>
    ITypedDataPointValue<TValue> CreateTypedValue(TValue value);
}
