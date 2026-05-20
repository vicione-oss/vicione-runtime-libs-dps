namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Represents a validation failure for a DataPointValue instance.
/// </summary>
/// <param name="DataPoint">The data point that failed validation.</param>
/// <param name="Reason">The specific reason for the validation failure.</param>
/// <param name="Details">Human-readable details about the failure.</param>
public readonly record struct DataPointValueValidationFailure(
    IDataPoint DataPoint,
    ValidationFailureReason Reason,
    string Details);
