using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace ViciOne.Suite.DataPort.Extensions.Exceptions;

/// <summary>
/// Exception thrown when DataPort configuration validation fails.
/// </summary>
public class InvalidConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class.
    /// </summary>
    public InvalidConfigurationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidConfigurationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class for an invalid LinkedNode with validation errors.
    /// </summary>
    /// <param name="linkedNode">The linked node that failed validation.</param>
    /// <param name="validationErrors">The collection of validation failures.</param>
    public InvalidConfigurationException(LinkedNode linkedNode, IEnumerable<ValidationFailure> validationErrors)
        : base(BuildInvalidLinkedNodeMessage(linkedNode, validationErrors))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class for an invalid DataPortCommunication with validation errors.
    /// </summary>
    /// <param name="communication">The communication configuration that failed validation.</param>
    /// <param name="validationErrors">The collection of validation failures.</param>
    public InvalidConfigurationException(DataPortCommunication communication,
        IEnumerable<ValidationFailure> validationErrors)
        : base(BuildInvalidCommunicationMessage(communication, validationErrors))
    {
    }

    private static string BuildInvalidLinkedNodeMessage(LinkedNode linkedNode,
        IEnumerable<ValidationFailure> validationErrors)
    {
        var errors = string.Join("; ", validationErrors.Select(static e => $"{e.PropertyName}: {e.ErrorMessage}"));
        return $"Cannot map node '{linkedNode.Name}' (DesignId: '{linkedNode.DesignId}'). Validation errors: {errors}";
    }

    private static string BuildInvalidCommunicationMessage(DataPortCommunication communication,
        IEnumerable<ValidationFailure> validationErrors)
    {
        var errors = string.Join("; ", validationErrors.Select(static e => $"{e.PropertyName}: {e.ErrorMessage}"));
        var communicationType = communication.GetType().Name;
        return $"Cannot map communication (Type: '{communicationType}') to typed nodes. Validation errors: {errors}";
    }
}
