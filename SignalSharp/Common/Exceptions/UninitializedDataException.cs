namespace SignalSharp.Common.Exceptions;

/// <summary>
/// Exception thrown when an operation is attempted on data that has not been initialized (for example,
/// calling a calculation before <c>Fit</c>).
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/> because it signals an operation-ordering violation.
/// </remarks>
public class UninitializedDataException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UninitializedDataException"/> class.
    /// </summary>
    public UninitializedDataException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UninitializedDataException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UninitializedDataException(string? message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UninitializedDataException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UninitializedDataException(string? message, Exception? innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Throws an exception if the data is not initialized.
    /// </summary>
    /// <param name="data">The data to validate.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="UninitializedDataException">Thrown when data is not initialized.</exception>
    public static void ThrowIfUninitialized(object data, string message)
    {
        if (data is null)
        {
            throw new UninitializedDataException(message);
        }
    }

    /// <summary>
    /// Throws an exception if the condition is false.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="UninitializedDataException">Thrown when the condition is false.</exception>
    public static void ThrowIfFalse(bool condition, string message)
    {
        if (!condition)
        {
            throw new UninitializedDataException(message);
        }
    }
}
