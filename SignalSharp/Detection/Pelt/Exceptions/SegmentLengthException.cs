namespace SignalSharp.Detection.Pelt.Exceptions;

/// <summary>
/// Exception thrown when the segment length of the RBF kernel is invalid.
/// </summary>
/// <param name="message">The exception message.</param>
public class SegmentLengthException(string? message) : Exception(message);