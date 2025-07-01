namespace SignalSharp.Extrapolation.Linear;

/// <summary>
/// Options for the Linear Extrapolation method.
/// </summary>
public record LinearExtrapolationOptions
{
    /// <summary>
    /// The number of recent historical data points to use for fitting the linear trend.
    /// Must be at least 2.
    /// If null, the entire signal history provided to Fit() will be used.
    /// </summary>
    /// <remarks>
    /// Using a smaller window focuses on the most recent trend but is more sensitive to noise.
    /// Using the entire history provides a more stable trend estimate but might miss recent changes.
    /// </remarks>
    public int? WindowSize { get; init; }
}
