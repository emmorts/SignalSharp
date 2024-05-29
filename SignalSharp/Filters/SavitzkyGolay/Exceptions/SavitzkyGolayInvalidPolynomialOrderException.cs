namespace SignalSharp.Filters.SavitzkyGolay.Exceptions;

/// <summary>
/// Exception thrown when the polynomial order of the Savitzky-Golay filter is invalid.
/// </summary>
/// <param name="message">The exception message.</param>
public class SavitzkyGolayInvalidPolynomialOrderException(string? message) : Exception(message);