namespace ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

/// <summary>
/// Specifies the reason why a DataPointValue validation failed.
/// </summary>
public enum ValidationFailureReason
{
    /// <summary>
    /// Value could not be converted to the data point's configured .NET type.
    /// </summary>
    ConversionFailure,

    /// <summary>
    /// Value is outside the valid range for the configured data type.
    /// </summary>
    ValueOutOfRange
}
