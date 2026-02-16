using System;

namespace ViciOne.Suite.DataPort.Extensions.Exceptions;

/// <summary>
/// Exception thrown when the maximum number of retry attempts has been exceeded.
/// </summary>
/// <remarks>
/// <para>
/// This exception is used to signal that a retryable operation has failed after exhausting
/// all configured retry attempts. It provides information about the number of attempts made
/// before giving up.
/// </para>
/// <para>
/// Common scenarios where this exception is thrown:
/// <list type="bullet">
/// <item><description>Write operations to external system that fail repeatedly</description></item>
/// <item><description>Connection attempts that cannot be established</description></item>
/// <item><description>Any operation with a configured maximum retry limit</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     await WriteWithRetryAsync(data, maxRetries: 5);
/// }
/// catch (MaxRetriesExceededException ex)
/// {
///     logger.LogError("Operation failed after {Attempts} attempts", ex.AttemptCount);
///     // Handle the failure - possibly alert, fallback, or rethrow
/// }
/// </code>
/// </example>
public class MaxRetriesExceededException : Exception
{
    /// <summary>
    /// Gets the number of retry attempts that were made before exceeding the limit.
    /// </summary>
    /// <value>
    /// The total number of attempts made. A value of 0 indicates the property was not set
    /// (when using parameterless constructors for compatibility).
    /// </value>
    public long AttemptCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRetriesExceededException"/> class
    /// with the specified attempt count.
    /// </summary>
    /// <param name="attemptCount">The number of retry attempts made before exceeding the limit.</param>
    public MaxRetriesExceededException(long attemptCount)
        : base($"Maximum retry attempts ({attemptCount}) exceeded.")
    {
        AttemptCount = attemptCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRetriesExceededException"/> class
    /// with the specified attempt count and error message.
    /// </summary>
    /// <param name="attemptCount">The number of retry attempts made before exceeding the limit.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public MaxRetriesExceededException(long attemptCount, string message)
        : base(message)
    {
        AttemptCount = attemptCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRetriesExceededException"/> class
    /// with the specified attempt count, error message, and inner exception.
    /// </summary>
    /// <param name="attemptCount">The number of retry attempts made before exceeding the limit.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, typically the last
    /// exception thrown during retry attempts.
    /// </param>
    public MaxRetriesExceededException(long attemptCount, string message, Exception innerException)
        : base(message, innerException)
    {
        AttemptCount = attemptCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRetriesExceededException"/> class
    /// with default values.
    /// </summary>
    /// <remarks>
    /// This constructor is provided for compatibility with exception handling patterns
    /// and serialization. The <see cref="AttemptCount"/> property will be 0 when using
    /// this constructor.
    /// </remarks>
    public MaxRetriesExceededException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRetriesExceededException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <remarks>
    /// This constructor is provided for compatibility. The <see cref="AttemptCount"/> property
    /// will be 0 when using this constructor.
    /// </remarks>
    public MaxRetriesExceededException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRetriesExceededException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
    /// parameter is not <see langword="null"/>, the current exception is raised in a catch block
    /// that handles the inner exception.
    /// </param>
    /// <remarks>
    /// This constructor is provided for compatibility. The <see cref="AttemptCount"/> property
    /// will be 0 when using this constructor.
    /// </remarks>
    public MaxRetriesExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
