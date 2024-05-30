namespace SignalSharp.Detection.PELT.Exceptions;

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
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    public static void ThrowIfInvalid(int segmentLength)
    {
        if (segmentLength < 1)
        {
            throw new SegmentLengthException("Segment length must be at least 1.");
        }
    }
}