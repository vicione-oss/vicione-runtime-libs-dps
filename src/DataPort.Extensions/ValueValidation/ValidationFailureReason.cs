namespace ViciOne.Suite.DataPort.Extensions.ValueValidation;

/// <summary>
/// Specifies the reason why a DataPointValue validation failed.
/// </summary>
public enum ValidationFailureReason
{
    /// <summary>
    /// Runtime value type does not match the configured data point type.
    /// </summary>
    TypeMismatch,

    /// <summary>
    /// Value is outside the valid range for the configured data type.
    /// </summary>
    ValueOutOfRange
}
