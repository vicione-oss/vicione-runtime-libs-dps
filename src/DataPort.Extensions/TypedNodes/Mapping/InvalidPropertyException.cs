using System;

namespace ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

/// <summary>
/// Exception thrown when a property value is invalid or cannot be converted to the expected type.
/// </summary>

/// <remarks>
/// Initializes a new instance of the <see cref="InvalidPropertyException"/> class 
/// with a standardized error message based on the validation error type.
/// </remarks>
/// <example>
/// <code>
/// throw new InvalidPropertyException(PropertyValidationErrorType.Missing, "PropertyName");
/// </code>
/// </example>
/// <remarks>
/// Exception thrown when a property value is invalid or cannot be converted to the expected type.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidPropertyException"/> class 
/// with a standardized error message based on the validation error type.
/// </remarks>
/// <param name="errorType">The type of validation error that occurred.</param>
/// <param name="propertyName">The name of the property that failed validation.</param>
/// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if not applicable.</param>
/// <example>
/// <code>
/// throw new InvalidPropertyException(PropertyValidationErrorType.Missing, "PropertyName");
/// </code>
/// </example>
// ReSharper disable once ClassWithoutDefaultConstructor - Exception requires specific parameters for meaningful error messages
#pragma warning disable CA1032 // Implement standard exception constructors - Exception requires specific parameters
public class InvalidPropertyException(
    PropertyValidationErrorType errorType,
    string propertyName,
    Exception? innerException = null) : Exception(BuildMessage(errorType, propertyName), innerException)
{
    /// <summary>
    /// Gets the name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    private static string MissingMessageTemplate(string propertyName) =>
        $"Property '{propertyName}' is missing or not found.";

    private static string NullMessageTemplate(string propertyName) => $"Property '{propertyName}' cannot be null.";

    private static string InvalidTypeMessageTemplate(string propertyName) =>
        $"Property '{propertyName}' has an invalid type or cannot be converted to the expected type.";

    private static string OutOfRangeMessageTemplate(string propertyName) =>
        $"Property '{propertyName}' value is out of the valid range.";

    private static string InvalidFormatMessageTemplate(string propertyName) =>
        $"Property '{propertyName}' has an invalid format.";

    private static string BuildMessage(PropertyValidationErrorType errorType, string propertyName) => errorType switch
    {
        PropertyValidationErrorType.Missing => MissingMessageTemplate(propertyName),
        PropertyValidationErrorType.Null => NullMessageTemplate(propertyName),
        PropertyValidationErrorType.InvalidType => InvalidTypeMessageTemplate(propertyName),
        PropertyValidationErrorType.OutOfRange => OutOfRangeMessageTemplate(propertyName),
        PropertyValidationErrorType.InvalidFormat => InvalidFormatMessageTemplate(propertyName),
        _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType,
            $"Unknown error type for property '{propertyName}'.")
    };
}
