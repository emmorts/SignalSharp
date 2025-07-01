namespace SignalSharp.Extrapolation.ExponentialSmoothing;

/// <summary>
/// Options for Holt's Linear Trend Method.
/// Suitable for data with a trend but no seasonality.
/// </summary>
public record HoltMethodOptions
{
    /// <summary>
    /// Smoothing factor for the level (alpha). Must be between 0 and 1 (inclusive).
    /// Higher values give more weight to recent observations.
    /// If null, the parameter will be optimized during the Fit process.
    /// </summary>
    public double? Alpha { get; init; }

    /// <summary>
    /// Smoothing factor for the trend (beta). Must be between 0 and 1 (inclusive).
    /// Higher values give more weight to recent trend changes.
    /// If null, the parameter will be optimized during the Fit process.
    /// </summary>
    public double? Beta { get; init; }

    /// <summary>
    /// The type of trend component. Typically Additive for Holt's method.
    /// </summary>
    public HoltMethodTrendType TrendType { get; init; } = HoltMethodTrendType.Additive;

    /// <summary>
    /// Optional initial level value. If null, estimated from data.
    /// </summary>
    public double? InitialLevel { get; init; }

    /// <summary>
    /// Optional initial trend value. If null, estimated from data.
    /// </summary>
    public double? InitialTrend { get; init; }

    /// <summary>
    /// Gets a value indicating whether to use a damped trend. Defaults to false.
    /// If true, the trend component is moderated over the forecast horizon using the Phi parameter.
    /// </summary>
    public bool DampTrend { get; init; }

    /// <summary>
    /// The damping parameter (phi) for the trend. Must be strictly between 0 and 1 (0 &lt; Phi &lt; 1).
    /// Required only if <see cref="DampTrend"/> is true. Lower values mean stronger damping.
    /// If null and DampTrend is true, the parameter will be optimized during the Fit process.
    /// </summary>
    public double? Phi { get; init; }

    /// <summary>
    /// The number of steps to use per parameter in the grid search when optimizing parameters.
    /// Only used if Alpha, Beta, or Phi are null. Defaults to 10.
    /// Higher values increase accuracy but significantly increase computation time.
    /// </summary>
    public int OptimizationGridSteps { get; init; } = 10;
}
