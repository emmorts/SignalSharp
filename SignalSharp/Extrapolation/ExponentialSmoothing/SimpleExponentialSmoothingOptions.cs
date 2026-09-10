namespace SignalSharp.Extrapolation.ExponentialSmoothing;

/// <summary>
/// Options for Simple Exponential Smoothing (SES).
/// Suitable for data with no clear trend or seasonality.
/// </summary>
public record SimpleExponentialSmoothingOptions
{
    private readonly double _alpha;

    /// <summary>
    /// Smoothing factor for the level (alpha). Must be a finite value in the range [0, 1].
    /// Higher values give more weight to recent observations.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is NaN, infinite, or outside [0, 1].</exception>
    public required double Alpha
    {
        get => _alpha;
        init
        {
            if (!double.IsFinite(value) || value < 0 || value > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(Alpha), value, "Alpha must be a finite value between 0 and 1.");
            }

            _alpha = value;
        }
    }

    /// <summary>
    /// Optional initial level value. If null, the first data point of the signal is used.
    /// </summary>
    public double? InitialLevel { get; init; }
}
