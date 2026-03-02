using System;

namespace ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping.Properties;

/// <summary>
/// Helper class for converting property values to strongly-typed values.
/// </summary>
public static class PropertyValueConverter
{
    /// <summary>
    /// Converts a property value to an enum, supporting both byte and string representations.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to convert to.</typeparam>
    /// <param name="value">The property value to convert.</param>
    /// <param name="propertyName">The name of the property for error messages.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="InvalidPropertyException">Thrown when the value cannot be converted to the enum type.</exception>
    public static TEnum ToEnum<TEnum>(object value, string propertyName) where TEnum : struct, Enum
    {
        switch (value)
        {
            // Try to convert from byte (enum underlying type)
            case byte byteValue:
                {
                    // Convert byte to int for Enum.IsDefined check since most enums have int as underlying type
                    var intValue = (int)byteValue;
                    if (!Enum.IsDefined(typeof(TEnum), intValue))
                    {
                        throw new InvalidPropertyException(
                            PropertyValidationErrorType.InvalidFormat,
                            propertyName);
                    }

                    return (TEnum)(object)intValue;
                }
            // Try to convert from int (default enum underlying type)
            case int integerValue when !Enum.IsDefined(typeof(TEnum), integerValue):
                throw new InvalidPropertyException(
                    PropertyValidationErrorType.InvalidFormat,
                    propertyName);
            case int integerValue:
                return (TEnum)(object)integerValue;
        }

        // Fallback: try to parse as string (for backwards compatibility)
        var stringValue = value.ToString() ?? string.Empty;
        if (!Enum.TryParse<TEnum>(stringValue, true, out var enumValue) || !Enum.IsDefined(enumValue))
        {
            throw new InvalidPropertyException(
                PropertyValidationErrorType.InvalidFormat,
                propertyName);
        }

        return enumValue;
    }

    /// <summary>
    /// Converts a property value to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to convert to.</typeparam>
    /// <param name="value">The property value to convert.</param>
    /// <param name="propertyName">The name of the property for error messages.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidPropertyException">Thrown when the value is not of type T.</exception>
    public static T To<T>(object value, string propertyName)
    {
        if (value is not T typedValue)
        {
            throw new InvalidPropertyException(
                PropertyValidationErrorType.InvalidType,
                propertyName);
        }

        return typedValue;
    }
}
