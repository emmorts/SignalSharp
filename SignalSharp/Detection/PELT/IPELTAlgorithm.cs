namespace SignalSharp.Detection.PELT;

/// <summary>
/// Interface for the PELT algorithm implementation.
/// </summary>
public interface IPELTAlgorithm
{
    /// <summary>
    /// Gets the options used to configure this PELT algorithm instance.
    /// </summary>
    PELTOptions Options { get; }

    /// <summary>
    /// Fits the PELT algorithm to the provided one-dimensional time series data.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to fit.</param>
    /// <returns>The fitted <see cref="IPELTAlgorithm"/> instance.</returns>
    IPELTAlgorithm Fit(double[] signal);

    /// <summary>
    /// Fits the PELT algorithm to the provided multi-dimensional time series data.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional time series data to fit.</param>
    /// <returns>The fitted <see cref="IPELTAlgorithm"/> instance.</returns>
    IPELTAlgorithm Fit(double[,] signalMatrix);

    /// <summary>
    /// Detects the change points in the fitted signal using the specified penalty value.
    /// </summary>
    /// <param name="penalty">The penalty value to control the number of change points.</param>
    /// <returns>An array of indices representing the change points in the signal.</returns>
    int[] Detect(double penalty);

    /// <summary>
    /// Fits the PELT algorithm to the provided one-dimensional signal data and detects the change points using the specified penalty value.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to be segmented.</param>
    /// <param name="penalty">The penalty value to control the number of change points.</param>
    /// <returns>An array of indices representing the change points in the signal.</returns>
    int[] FitAndDetect(double[] signal, double penalty);

    /// <summary>
    /// Fits the PELT algorithm to the provided multi-dimensional signal data and detects the change points using the specified penalty value.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional time series data to be segmented.</param>
    /// <param name="penalty">The penalty value to control the number of change points.</param>
    /// <returns>An array of indices representing the change points in the signal.</returns>
    int[] FitAndDetect(double[,] signalMatrix, double penalty);
}
