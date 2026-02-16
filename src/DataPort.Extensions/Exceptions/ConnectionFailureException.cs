using System;

namespace ViciOne.Suite.DataPort.Extensions.Exceptions;

/// <summary>
/// Exception thrown when a device connection fails.
/// </summary>
public class ConnectionFailureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailureException"/> class.
    /// </summary>
    public ConnectionFailureException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailureException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConnectionFailureException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailureException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ConnectionFailureException(string message, Exception inner) : base(message, inner) { }
}
