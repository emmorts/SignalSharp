namespace SignalSharp.Detection.CUSUM;

/// <summary>
/// Represents the configuration options for the Cumulative Sum (CUSUM) algorithm.
/// </summary>
/// <remarks>
/// <para>
/// These options allow customization of the CUSUM algorithm's behavior, including the expected mean, expected standard deviation, slack factor, and threshold factor.
/// Adjusting these parameters enables fine-tuning of the change detection sensitivity and robustness to noise in the data.
/// </para>
/// </remarks>
public record CUSUMOptions
{
    /// <summary>
    /// The expected mean value of the process. This is the average value around which the time series data is expected to fluctuate.
    /// The default is 0.
    /// </summary>
    public double ExpectedMean { get; init; } = 0;

    /// <summary>
    /// The expected standard deviation (σ) of the process. This represents the expected variability in the data.
    /// The default is 1.
    /// </summary>
    public double ExpectedStandardDeviation { get; init; } = 1;

    /// <summary>
    /// The slack factor, which determines the slack allowed in the process before a change is detected.
    /// It is multiplied by the expected standard deviation to compute the slack value.
    /// The default is 0.
    /// </summary>
    public double SlackFactor { get; init; } = 0;

    /// <summary>
    /// The threshold factor for change detection, which sets the sensitivity of the algorithm.
    /// It is multiplied by the expected standard deviation to compute the threshold value.
    /// A higher threshold factor makes the algorithm less sensitive to small changes.
    /// The default value is 5.
    /// </summary>
    public double ThresholdFactor { get; init; } = 5;
}
