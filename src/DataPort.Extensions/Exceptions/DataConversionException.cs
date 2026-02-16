using System;

namespace ViciOne.Suite.DataPort.Extensions.Exceptions;

/// <summary>
/// Exception thrown when data type conversion fails during read or write operations.
/// </summary>
public class DataConversionException : Exception
{
    private DataConversionException() { }
    private DataConversionException(string message) : base(message) { }
#pragma warning disable IDE0051
    private DataConversionException(string message, Exception innerException) : base(message, innerException)
#pragma warning restore IDE0051
    {
    }
    /// <summary>
    /// Creates an exception for unsupported value type conversions during read operations.
    /// </summary>
    /// <param name="systemTypeName">The S7 system type name (e.g., "S7_Bool").</param>
    /// <param name="valueType">The unsupported value type.</param>
    /// <returns>A DataConversionException with a formatted message.</returns>
    public static DataConversionException UnsupportedValueType(string systemTypeName, object valueType) =>
        new($"Unsupported value type '{valueType}' for {systemTypeName} read conversion.");

    /// <summary>
    /// Creates an exception for unsupported system type conversions during read operations.
    /// </summary>
    /// <param name="systemType">The unsupported system type.</param>
    /// <returns>A DataConversionException with a formatted message.</returns>
    public static DataConversionException UnsupportedSystemType(object systemType) =>
        new($"Unsupported system type '{systemType}' for read conversion.");

    /// <summary>
    /// Creates an exception for unsupported value type conversions during write operations.
    /// </summary>
    /// <param name="systemTypeName">The S7 system type name (e.g., "S7_Bool").</param>
    /// <param name="valueType">The unsupported value type.</param>
    /// <returns>A DataConversionException with a formatted message.</returns>
    public static DataConversionException UnsupportedWriteValueType(string systemTypeName, object valueType) =>
        new($"Unsupported value type '{valueType}' for {systemTypeName} write conversion.");

    /// <summary>
    /// Creates an exception for unsupported system type conversions during write operations.
    /// </summary>
    /// <param name="systemType">The unsupported system type.</param>
    /// <returns>A DataConversionException with a formatted message.</returns>
    public static DataConversionException UnsupportedWriteSystemType(object systemType) =>
        new($"Unsupported system type '{systemType}' for write conversion.");

    /// <summary>
    /// Creates an exception when setting a value into the access buffer fails.
    /// </summary>
    /// <param name="valueTypeName">The type name of the value being written (e.g., "STRING8", "STRING16").</param>
    /// <param name="errorMessage">The error message from the underlying API.</param>
    /// <returns>A DataConversionException with a formatted message.</returns>
    public static DataConversionException FailedToSetAccessBuffer(string valueTypeName, string errorMessage) =>
        new($"Failed to set {valueTypeName} value in access buffer. Error: {errorMessage}");

    /// <summary>
    /// Creates an exception when getting a value from the access buffer fails.
    /// </summary>
    /// <param name="valueTypeName">The type name of the value being read (e.g., "DateTime", "String").</param>
    /// <param name="errorMessage">The error message from the underlying API.</param>
    /// <returns>A DataConversionException with a formatted message.</returns>
    public static DataConversionException FailedToGetAccessBuffer(string valueTypeName, string errorMessage) =>
        new($"Failed to get {valueTypeName} value from access buffer. Error: {errorMessage}");
}
