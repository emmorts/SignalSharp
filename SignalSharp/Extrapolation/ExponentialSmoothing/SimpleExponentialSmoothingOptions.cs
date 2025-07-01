namespace SignalSharp.Extrapolation.ExponentialSmoothing;

/// <summary>
/// Options for Simple Exponential Smoothing (SES).
/// Suitable for data with no clear trend or seasonality.
/// </summary>
public record SimpleExponentialSmoothingOptions
{
    /// <summary>
    /// Smoothing factor for the level (alpha). Must be between 0 and 1.
    /// Higher values give more weight to recent observations.
    /// </summary>
    public required double Alpha { get; init; }

    /// <summary>
    /// Optional initial level value. If null, it will be estimated from the first few data points.
    /// </summary>
    public double? InitialLevel { get; init; }

    public SimpleExponentialSmoothingOptions()
    {
        if (Alpha is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Alpha), "Alpha must be between 0 and 1.");
        }
    }
}
