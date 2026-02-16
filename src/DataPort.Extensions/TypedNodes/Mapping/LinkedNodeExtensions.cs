using System;
using System.Collections.Generic;

namespace ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

/// <summary>
/// Extension methods for LinkedNode to work with properties.
/// </summary>
public static class LinkedNodeExtensions
{
    /// <summary>
    /// Gets a property from the LinkedNode, throwing an exception if it doesn't exist.
    /// </summary>
    /// <param name="node">The LinkedNode to get the property from.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The Property object.</returns>
    /// <exception cref="InvalidPropertyException">Thrown when the property is missing.</exception>
    public static Property GetRequiredProperty(this LinkedNode node, string propertyName) =>
        !node.Properties.TryGetValue(propertyName, out var property)
            ? throw new InvalidPropertyException(PropertyValidationErrorType.Missing, propertyName)
            : property;

    /// <summary>
    /// Gets a property from the LinkedNode, returning null if it doesn't exist.
    /// </summary>
    /// <param name="node">The LinkedNode to get the property from.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The Property object if it exists; otherwise null.</returns>
    public static Property? GetOptionalProperty(this LinkedNode node, string propertyName) =>
        node.Properties.GetValueOrDefault(propertyName);

    /// <summary>
    /// Checks whether a property exists in the LinkedNode's properties dictionary.
    /// </summary>
    /// <param name="node">The LinkedNode to check.</param>
    /// <param name="propertyName">The name of the property to check for.</param>
    /// <returns>True if the property exists; otherwise false.</returns>
    public static bool HasProperty(this LinkedNode node, string propertyName) =>
        node.Properties.ContainsKey(propertyName);

    /// <summary>
    /// Checks whether a property exists and has a value of the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="node">The LinkedNode to check.</param>
    /// <param name="propertyName">The name of the property to check for.</param>
    /// <returns>True if the property exists and its value is of type T; otherwise false.</returns>
    public static bool HasPropertyOfType<T>(this LinkedNode node, string propertyName)
    {
        var property = node.GetOptionalProperty(propertyName);
        return property is { Value: T };
    }

    /// <summary>
    /// Checks whether a property exists and has a value that can be parsed as the specified enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to check against.</typeparam>
    /// <param name="node">The LinkedNode to check.</param>
    /// <param name="propertyName">The name of the property to check for.</param>
    /// <returns>True if the property exists and can be parsed as TEnum; otherwise false.</returns>
    public static bool HasPropertyAsEnum<TEnum>(this LinkedNode node, string propertyName) where TEnum : struct, Enum
    {
        var property = node.GetOptionalProperty(propertyName);
        if (property == null)
            return false;

        // Check if the value is a byte and if it's defined in the enum
        if (property.Value is byte byteValue)
        {
            return Enum.IsDefined(typeof(TEnum), (int)byteValue);
        }

        // Fallback: try to parse as string (for backwards compatibility)
        var stringValue = property.Value.ToString() ?? string.Empty;
        return Enum.TryParse<TEnum>(stringValue, true, out var enumValue) && Enum.IsDefined(enumValue);
    }

    /// <summary>
    /// Checks whether a property exists and has a value that is an integer within the specified range.
    /// </summary>
    /// <param name="node">The LinkedNode to check.</param>
    /// <param name="propertyName">The name of the property to check for.</param>
    /// <param name="minValue">The minimum allowed value (inclusive).</param>
    /// <param name="maxValue">The maximum allowed value (inclusive).</param>
    /// <returns>True if the property exists and is an integer within range; otherwise false.</returns>
    public static bool HasPropertyAsIntInRange(this LinkedNode node, string propertyName, int minValue, int maxValue)
    {
        var property = node.GetOptionalProperty(propertyName);
        return property?.Value is int intValue && intValue >= minValue && intValue <= maxValue;
    }

    /// <summary>
    /// Gets a property value as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to retrieve the value as.</typeparam>
    /// <param name="node">The LinkedNode to get the property from.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The property value cast to the specified type.</returns>
    /// <exception cref="InvalidPropertyException">Thrown when the property is missing or not of type T.</exception>
    public static T GetRequiredPropertyValue<T>(this LinkedNode node, string propertyName)
    {
        var property = node.GetRequiredProperty(propertyName);
        return PropertyValueConverter.To<T>(property.Value, propertyName);
    }

    /// <summary>
    /// Gets an optional property value as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to retrieve the value as.</typeparam>
    /// <param name="node">The LinkedNode to get the property from.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The property value cast to the specified type, or null if the property doesn't exist.</returns>
    /// <exception cref="InvalidPropertyException">Thrown when the property exists but is not of type T.</exception>
    public static T? GetOptionalPropertyValue<T>(this LinkedNode node, string propertyName)
    {
        var property = node.GetOptionalProperty(propertyName);
        return property == null ? default : PropertyValueConverter.To<T>(property.Value, propertyName);
    }
}
