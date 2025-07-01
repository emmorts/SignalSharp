namespace SignalSharp.Common.Exceptions;

/// <summary>
/// Exception thrown when data is not initialized.
/// </summary>
/// <param name="message">The exception message.</param>
public class UninitializedDataException(string? message) : Exception(message)
{
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
