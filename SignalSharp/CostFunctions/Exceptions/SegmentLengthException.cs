namespace SignalSharp.CostFunctions.Exceptions;

/// <summary>
/// Exception thrown when the segment length of the RBF kernel is invalid.
/// </summary>
/// <param name="message">The exception message.</param>
public class SegmentLengthException(string? message) : Exception(message)
{
    /// <summary>
    /// Throws an exception if the segment length is invalid.
    /// </summary>
    /// <param name="segmentLength">The segment length to validate.</param>
    /// <param name="minSegmentLength">The minimum valid segment length (default is 1).</param>
    /// <param name="message">Custom message for the exception.</param>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    public static void ThrowIfInvalid(int segmentLength, int minSegmentLength = 1, string? message = null)
    {
        if (segmentLength < minSegmentLength)
        {
            throw new SegmentLengthException(message ?? $"Segment length must be at least {minSegmentLength}.");
        }
    }
}