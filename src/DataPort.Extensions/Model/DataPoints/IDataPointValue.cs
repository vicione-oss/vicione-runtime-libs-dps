using System.Diagnostics.CodeAnalysis;
using ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Represents a value associated with a data point, including validation capabilities.
/// </summary>
/// <typeparam name="TDataPointType">The type of data point this value is associated with, must implement <see cref="IDataPoint"/>.</typeparam>
/// <remarks>
/// Data point values encapsulate both the raw value and its associated data point metadata,
/// enabling validation of type compatibility and value range constraints.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IDataPointValue<TDataPointType> where TDataPointType : IDataPoint
{
    /// <summary>
    /// Gets or initializes the raw value of this data point.
    /// </summary>
    /// <remarks>
    /// The value may be <c>null</c> to represent missing or uninitialized data.
    /// </remarks>
    object? Value { get; init; }

    /// <summary>
    /// Gets or initializes the data point metadata associated with this value.
    /// </summary>
    TDataPointType DataPoint { get; init; }

    /// <summary>
    /// Determines whether the value falls within the acceptable range for this data point.
    /// </summary>
    /// <returns><c>true</c> if the value is within the valid range; otherwise, <c>false</c>.</returns>
    bool IsInValueRange();

    /// <summary>
    /// Validates that the value's type is compatible with the data point's expected type.
    /// </summary>
    /// <returns>A <see cref="TypeValidationResult"/> indicating whether the type is valid and providing details about any type mismatch.</returns>
    TypeValidationResult ValidateType();
}
