using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

/// <summary>
/// Represents a validation failure for a DataPointValue instance.
/// </summary>
/// <param name="DataPoint">The data point that failed validation.</param>
/// <param name="ActualValue">The runtime value that failed validation.</param>
/// <param name="Reason">The specific reason for the validation failure.</param>
/// <param name="Details">Human-readable details about the failure (e.g., expected vs actual types).</param>
public readonly record struct DataPointValueValidationFailure<TDataPointType>(
    TDataPointType DataPoint,
    object? ActualValue,
    ValidationFailureReason Reason,
    string Details) where TDataPointType : IDataPoint;
