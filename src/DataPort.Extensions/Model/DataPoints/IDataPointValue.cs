using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Represents a value associated with a data point, exposing the data point metadata and range validation.
/// </summary>
/// <remarks>
/// This is the non-typed view carried by the data port pipeline, so that a single port can carry values of
/// differing .NET types (e.g. an <c>int</c> value and a <c>string</c> value) through one common type.
/// The strongly-typed payload is exposed by <see cref="ITypedDataPointValue{TValue}"/>.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IDataPointValue
{
    /// <summary>
    /// Gets the data point metadata associated with this value.
    /// </summary>
    IDataPoint DataPoint { get; }

    /// <summary>
    /// Gets the value as an untyped object — the common view carried through the data port pipeline.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Determines whether the value falls within the acceptable range for this data point.
    /// </summary>
    /// <returns><c>true</c> if the value is within the valid range; otherwise, <c>false</c>.</returns>
    bool IsInValueRange();
}
