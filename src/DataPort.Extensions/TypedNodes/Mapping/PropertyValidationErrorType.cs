namespace ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

/// <summary>
/// Specifies the type of property validation failure.
/// </summary>
public enum PropertyValidationErrorType
{
    /// <summary>
    /// The property is missing or not found.
    /// </summary>
    Missing,

    /// <summary>
    /// The property value is null.
    /// </summary>
    Null,

    /// <summary>
    /// The property value cannot be converted to the expected type.
    /// </summary>
    InvalidType,

    /// <summary>
    /// The property value is out of the valid range.
    /// </summary>
    OutOfRange,

    /// <summary>
    /// The property value does not match the expected format.
    /// </summary>
    InvalidFormat
}
