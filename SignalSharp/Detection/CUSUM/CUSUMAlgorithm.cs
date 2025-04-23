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
    public CUSUMAlgorithm(CUSUMOptions? options = null)
    {
        _options = options ?? new CUSUMOptions();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_options.ExpectedStandardDeviation, nameof(_options.ExpectedStandardDeviation));
        ArgumentOutOfRangeException.ThrowIfNegative(_options.SlackFactor, nameof(_options.SlackFactor));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_options.ThresholdFactor, nameof(_options.ThresholdFactor));

        _threshold = _options.ThresholdFactor * _options.ExpectedStandardDeviation;
        _slack = _options.SlackFactor * _options.ExpectedStandardDeviation;
    }

    /// <summary>
    /// Detects change points in the time series using the CUSUM algorithm.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to fit.</param>
    /// <returns>An array of indices representing the change points.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal data is null.</exception>
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
