using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Provides a base implementation of the <see cref="IPELTCostFunction"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This abstract class includes a default implementation for fitting one-dimensional time series data by
/// converting it to a two-dimensional format. Subclasses must implement the methods for fitting
/// multidimensional time series data and computing the cost for a data segment.
/// </para>
/// <para>
/// Subclasses may also optionally implement <see cref="ILikelihoodCostFunction"/> if they provide
/// likelihood-based metrics suitable for information criteria like BIC and AIC.
/// </para>
/// </remarks>
public abstract class CostFunctionBase : IPELTCostFunction
{
    /// <summary>
    /// Fits the cost function to the provided one-dimensional time series data.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to fit.</param>
    /// <returns>The fitted <see cref="IPELTCostFunction"/> instance.</returns>
    /// <remarks>
    /// This method converts the one-dimensional data into a two-dimensional format (1 row) and calls
    /// <see cref="Fit(double[,])"/> to perform the actual fitting.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the signal is null.</exception>
    public IPELTCostFunction Fit(double[] signal)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));

        var signalMatrix = new double[1, signal.Length];
        for (var i = 0; i < signal.Length; i++)
        {
            signalMatrix[0, i] = signal[i];
        }

        return Fit(signalMatrix);
    }

    /// <summary>
    /// Fits the cost function to the provided multi-dimensional time series data.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional time series data to fit, where each row represents a different time series.</param>
    /// <returns>The fitted <see cref="IPELTCostFunction"/> instance.</returns>
    /// <remarks>
    /// Subclasses must implement this method to initialize any internal structures or computations needed
    /// to evaluate segment costs later on. It prepares the cost function for subsequent calls to
    /// <see cref="ComputeCost(int?, int?)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the signal matrix is null.</exception>
    public abstract IPELTCostFunction Fit(double[,] signalMatrix);

    /// <summary>
    /// Computes the cost for a segment of the data.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>
    /// Subclasses must implement this method to calculate the cost or dissimilarity of a segment of the
    /// data, which is used by the PELT algorithm to determine the optimal segmentation.
    /// </para>
    /// <para>
    /// The exact definition of "cost" depends on the specific cost function implementation.
    /// For likelihood-based costs, this might be related to the negative log-likelihood.
    /// </para>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown if <see cref="Fit(double[,])"/> or <see cref="Fit(double[])"/> has not been called.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if segment indices are invalid.</exception>
    /// <exception cref="SegmentLengthException">Thrown if segment length is invalid for the cost function.</exception>
    public abstract double ComputeCost(int? start = null, int? end = null);
}