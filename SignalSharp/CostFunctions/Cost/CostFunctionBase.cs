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
    public virtual IPELTCostFunction Fit(double[] signal)
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
    /// Validates the signal matrix passed to <see cref="Fit(double[,])"/>: it must be non-null and contain only
    /// finite values. Empty signals are permitted by design and yield a zero cost.
    /// </summary>
    /// <param name="signalMatrix">The signal matrix to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="signalMatrix"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the matrix contains NaN or infinite values.</exception>
    protected static void ValidateSignalFiniteness(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix);

        var dimensions = signalMatrix.GetLength(0);
        var points = signalMatrix.GetLength(1);

        for (var dim = 0; dim < dimensions; dim++)
        {
            for (var i = 0; i < points; i++)
            {
                if (!double.IsFinite(signalMatrix[dim, i]))
                {
                    throw new ArgumentException($"Signal matrix contains a non-finite value at dimension {dim}, index {i}.", nameof(signalMatrix));
                }
            }
        }
    }

    /// <summary>
    /// Validates that a cost function requiring O(dimensions * points^2) storage can allocate its precomputed
    /// structures for the supplied signal, throwing a descriptive error instead of failing with an
    /// <see cref="OutOfMemoryException"/> or a framework array-length error.
    /// </summary>
    /// <param name="dimensions">The number of signal dimensions (rows).</param>
    /// <param name="points">The number of data points (columns).</param>
    /// <param name="costFunctionName">The name of the cost function, used in the error message.</param>
    /// <exception cref="ArgumentException">Thrown when the required element count exceeds the maximum array length.</exception>
    protected static void ValidateQuadraticStorageCapacity(int dimensions, int points, string costFunctionName)
    {
        var requiredElements = (long)dimensions * points * points;

        if (requiredElements > Array.MaxLength)
        {
            throw new ArgumentException(
                $"{costFunctionName} requires O(dimensions * points^2) storage ({requiredElements} elements for "
                    + $"{dimensions} dimensions and {points} points), which exceeds the maximum array length ({Array.MaxLength}). "
                    + "Use fewer data points or reduce the number of dimensions.",
                nameof(points)
            );
        }
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
