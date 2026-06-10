using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// A data point value whose payload is strongly typed to the .NET type declared by its data point.
/// </summary>
/// <typeparam name="TValue">The .NET type used to represent the value.</typeparam>
/// <remarks>
/// Tying the value's type to the data point's declared <see cref="ITypedDataPoint{TValue}"/> type at compile time
/// means a value can never be paired with a data point of a different type.
/// This removes the need for a runtime type check during validation.
/// </remarks>
public interface ITypedDataPointValue<out TValue> : IDataPointValue
{
    /// <summary>
    /// Gets the strongly-typed value of this data point.
    /// </summary>
    TValue TypedValue { get; }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types")]
    object? IDataPointValue.Value => TypedValue;
}
