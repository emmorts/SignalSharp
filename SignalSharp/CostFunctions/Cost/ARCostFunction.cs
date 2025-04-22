using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Utilities;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on fitting an Autoregressive (AR) model for the PELT method.
/// </summary>
/// <remarks>
/// <para>
/// This cost function evaluates the goodness-of-fit of an Autoregressive (AR) model of a specified order `p`
/// to a segment of the time series data. The cost is defined as the Residual Sum of Squares (RSS)
/// after fitting the model using Ordinary Least Squares (OLS).
/// </para>
/// <para>
/// The AR(p) model predicts the current value based on the `p` preceding values:
/// `signal[t] = c + a1*signal[t-1] + a2*signal[t-2] + ... + ap*signal[t-p] + error[t]`
/// where `c` is an optional intercept term and `a1, ..., ap` are the AR coefficients.
/// </para>
/// <para>
/// This cost function is useful for detecting changes in the underlying dynamics or autocorrelation
/// structure of a time series. A lower RSS indicates a better fit of the AR model to the segment.
/// </para>
/// <para>
/// Important Notes:
/// <list type="bullet">
///     <item>
///         Minimum Segment Length: The segment must be long enough to both form the AR equations and solve the resulting linear system. The minimum required length is `max(order + 1, 2*order + k)`, where `k` is 1 if `includeIntercept` is true, and 0 otherwise. Shorter segments will cause a <see cref="SegmentLengthException"/>.
///     </item>
///     <item>
///         Constant Data Segments: Fitting an AR model *with* an intercept to a perfectly constant segment (e.g., `[5, 5, 5]`) leads to a singular linear system (collinearity between the intercept and lagged variables). This function handles this by returning `double.PositiveInfinity` cost for such segments. Fitting *without* an intercept may result in a near-zero cost if the constant value is non-zero.
///     </item>
///     <item>
///          This implementation currently only supports univariate (single-dimensional) time series.
///     </item>
/// </list>
/// </para>
/// </remarks>
public class ARCostFunction : CostFunctionBase
{
    private readonly int _order;
    private readonly bool _includeIntercept;
    private double[] _signal = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ARCostFunction"/> class.
    /// </summary>
    /// <param name="order">The order 'p' of the Autoregressive model (must be >= 1).</param>
    /// <param name="includeIntercept">Whether to include an intercept term (constant) in the AR model. Default is true.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if order is less than 1.</exception>
    public ARCostFunction(int order, bool includeIntercept = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(order, 1, nameof(order));
        _order = order;
        _includeIntercept = includeIntercept;
    }

    /// <summary>
    /// Fits the cost function to the provided one-dimensional time series data.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to fit.</param>
    /// <returns>The fitted <see cref="ARCostFunction"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the signal is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the signal length is less than order + 1.</exception>
    public new IPELTCostFunction Fit(double[] signal)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        if (signal.Length < _order + 1)
        {
            // need at least p past points and 1 current point to fit
            throw new ArgumentException(
                $"Signal length must be at least order + 1 ({_order + 1}) for AR({_order}) model.", nameof(signal));
        }

        _signal = signal;
        return this;
    }

    /// <summary>
    /// Fits the cost function to the provided multi-dimensional time series data.
    /// Note: ARCostFunction currently only supports univariate (1D) signals.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional time series data. Must have exactly one row.</param>
    /// <returns>The fitted <see cref="ARCostFunction"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the signal matrix is null.</exception>
    /// <exception cref="NotSupportedException">Thrown if the signal matrix has more than one dimension (row).</exception>
    /// <exception cref="ArgumentException">Thrown if the signal length is less than order + 1.</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        if (signalMatrix.GetLength(0) != 1)
        {
            throw new NotSupportedException(
                $"{nameof(ARCostFunction)} currently only supports univariate (1D) signals. Input matrix has {signalMatrix.GetLength(0)} dimensions.");
        }

        var signalLength = signalMatrix.GetLength(1);
        var signal = new double[signalLength];
        for (var i = 0; i < signalLength; i++)
        {
            signal[i] = signalMatrix[0, i];
        }

        return Fit(signal);
    }

    /// <summary>
    /// Computes the cost (Residual Sum of Squares) for a segment by fitting an AR(p) model.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the signal.</param>
    /// <returns>The computed RSS cost for the segment, or double.PositiveInfinity if the model cannot be fit.</returns>
    /// <exception cref="UninitializedDataException">Thrown when Fit method has not been called before ComputeCost.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than order + 1.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        UninitializedDataException.ThrowIfUninitialized(_signal, "Fit() must be called before ComputeCost().");

        var startIndex = start ?? 0;
        var endIndex = end ?? _signal.Length;
        var segmentLength = endIndex - startIndex;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _signal.Length, nameof(end));
        
        var minSolverLength = _includeIntercept ? (2 * _order + 1) : (2 * _order);
        // also need at least order+1 points to form the first equation's lags and target
        var minRequiredLength = Math.Max(_order + 1, minSolverLength);

        SegmentLengthException.ThrowIfInvalid(segmentLength, minRequiredLength);

        var segmentData = _signal.AsSpan(startIndex, segmentLength);

        try
        {
            return FitARAndGetRSS(segmentData, _order, _includeIntercept, startIndex, endIndex);
        }
        catch (Exception ex)
        {
            throw new ARCostFunctionException(
                $"Unexpected error during AR({_order}) model fitting for segment [{startIndex}, {endIndex}). Reason: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Fits an AR(p) model to the data segment using OLS and returns the Residual Sum of Squares (RSS).
    /// </summary>
    /// <param name="segmentData">The data segment.</param>
    /// <param name="order">The AR order p.</param>
    /// <param name="includeIntercept">Whether to include an intercept term.</param>
    /// <returns>Residual Sum of Squares (RSS).</returns>
    private static double FitARAndGetRSS(ReadOnlySpan<double> segmentData, int order, bool includeIntercept, int startIndex, int endIndex)
    {
        var n = segmentData.Length;
        var numEquations = n - order;

        var numPredictors = includeIntercept ? order + 1 : order;
        var designMatrix = new double[numEquations, numPredictors];
        var targetVector = new double[numEquations];

        for (var i = 0; i < numEquations; i++)
        {
            var currentTime = order + i;
            targetVector[i] = segmentData[currentTime];

            var predictorCol = 0;
            if (includeIntercept)
            {
                designMatrix[i, predictorCol++] = 1.0;
            }

            for (var lag = 1; lag <= order; lag++)
            {
                designMatrix[i, predictorCol++] = segmentData[currentTime - lag];
            }
        }

        if (!MatrixOperations.TrySolveLinearSystemQR(designMatrix, targetVector, out var coefficients))
        {
            Console.Error.WriteLine($"Warning: AR({order}) solver failed for segment [{startIndex}, {endIndex}] due to singular matrix.");
            
            return double.PositiveInfinity;
        }

        // calculate residuals and RSS
        var rss = 0.0;
        var predictedVector = MatrixOperations.Multiply(designMatrix, coefficients!); // Y_hat = X * coeffs

        for (var i = 0; i < numEquations; i++)
        {
            var residual = targetVector[i] - predictedVector[i];
            rss += residual * residual;
        }

        if (double.IsNaN(rss) || double.IsInfinity(rss))
        {
            throw new ARCostFunctionException(
                "AR model fitting resulted in NaN or Infinite RSS, likely due to ill-conditioned matrix.");
        }

        return rss;
    }
}