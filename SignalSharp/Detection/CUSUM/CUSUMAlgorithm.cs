namespace SignalSharp.Detection.CUSUM;

/// <summary>
/// Implements the Cumulative Sum (CUSUM) algorithm for detecting change points in time series data.
/// </summary>
/// <remarks>
/// <para>
/// The CUSUM (Cumulative Sum Control Chart) algorithm is a sequential analysis technique used primarily for monitoring change detection in time series data.
/// It aims to identify points where the statistical properties of the data, such as the mean, change.
/// </para>
/// <para>
/// The algorithm maintains cumulative sums of the deviations of the signal values from the expected mean.
/// It uses two sums: a high sum (for detecting positive changes) and a low sum (for detecting negative changes).
/// When either sum exceeds a defined threshold, a change point is detected.
/// </para>
/// </remarks>
public class CUSUMAlgorithm
{
    private readonly CUSUMOptions _options;
    private readonly double _threshold;
    private readonly double _slack;

    /// <summary>
    /// Initializes a new instance of the <see cref="CUSUMAlgorithm"/> class with optional configuration settings.
    /// </summary>
    /// <param name="options">The configuration options for the CUSUM algorithm. If null, default options are used.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an option is not finite, when <see cref="CUSUMOptions.ExpectedStandardDeviation"/> or
    /// <see cref="CUSUMOptions.ThresholdFactor"/> is not positive, when <see cref="CUSUMOptions.SlackFactor"/> is negative,
    /// or when the derived threshold/slack overflow to a non-finite value.
    /// </exception>
    public CUSUMAlgorithm(CUSUMOptions? options = null)
    {
        _options = options ?? new CUSUMOptions();

        ValidateFiniteOption(_options.ExpectedMean, nameof(CUSUMOptions.ExpectedMean));
        ValidateFiniteOption(_options.ExpectedStandardDeviation, nameof(CUSUMOptions.ExpectedStandardDeviation));
        ValidateFiniteOption(_options.SlackFactor, nameof(CUSUMOptions.SlackFactor));
        ValidateFiniteOption(_options.ThresholdFactor, nameof(CUSUMOptions.ThresholdFactor));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_options.ExpectedStandardDeviation, nameof(CUSUMOptions.ExpectedStandardDeviation));
        ArgumentOutOfRangeException.ThrowIfNegative(_options.SlackFactor, nameof(CUSUMOptions.SlackFactor));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_options.ThresholdFactor, nameof(CUSUMOptions.ThresholdFactor));

        _threshold = _options.ThresholdFactor * _options.ExpectedStandardDeviation;
        _slack = _options.SlackFactor * _options.ExpectedStandardDeviation;

        if (!double.IsFinite(_threshold) || !double.IsFinite(_slack))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"Derived threshold ({_threshold}) and slack ({_slack}) must be finite; reduce ThresholdFactor, SlackFactor or ExpectedStandardDeviation."
            );
        }
    }

    /// <summary>
    /// Throws when an option value is not finite (NaN or infinity), which would silently disable change detection.
    /// </summary>
    private static void ValidateFiniteOption(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Option must be a finite value.");
        }
    }

    /// <summary>
    /// Detects change points in the time series using the CUSUM algorithm.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to fit.</param>
    /// <returns>An array of indices representing the change points. Empty when the signal has fewer than two samples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal data is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the signal contains NaN or infinite values.</exception>
    /// <example>
    /// <code>
    /// var options = new CUSUMOptions
    /// {
    ///     ExpectedMean = 0,
    ///     ExpectedStandardDeviation = 1,
    ///     ThresholdFactor = 1.2,
    ///     SlackFactor = 0.1
    /// };
    /// var cusum = new CUSUMAlgorithm(options);
    /// double[] signal = { 0.2, 0.1, 0.2, 4.0, 0.1, 0.2, -2.0, 0.2, 0.1 };
    /// int[] changePoints = cusum.Detect(signal);
    /// // changePoints will contain the indices where changes are detected, e.g., [3, 6]
    /// </code>
    /// </example>
    public int[] Detect(double[] signal)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));

        for (var i = 0; i < signal.Length; i++)
        {
            if (!double.IsFinite(signal[i]))
            {
                throw new ArgumentException($"Signal contains a non-finite value at index {i}; CUSUM requires finite input.", nameof(signal));
            }
        }

        if (signal.Length < 2)
            return [];

        var highSum = 0.0;
        var lowSum = 0.0;
        var changePoints = new List<int>();

        for (var i = 1; i < signal.Length; i++)
        {
            highSum = Math.Max(0, highSum + signal[i] - _options.ExpectedMean - _slack);
            lowSum = Math.Min(0, lowSum + signal[i] - _options.ExpectedMean + _slack);

            if (highSum > _threshold || lowSum < -_threshold)
            {
                changePoints.Add(i);
                highSum = 0;
                lowSum = 0;
            }
        }

        return changePoints.ToArray();
    }
}
