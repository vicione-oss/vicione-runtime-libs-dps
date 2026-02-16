using System;

namespace ViciOne.Suite.DataPort.Extensions.Exceptions;

/// <summary>
/// Exception thrown when loading data from the external system fails.
/// </summary>
public class DataRetrievalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataRetrievalException"/> class.
    /// </summary>
    public DataRetrievalException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataRetrievalException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DataRetrievalException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataRetrievalException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public DataRetrievalException(string message, Exception inner) : base(message, inner) { }
}
