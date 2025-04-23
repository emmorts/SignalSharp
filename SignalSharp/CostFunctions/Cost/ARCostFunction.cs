using Microsoft.Extensions.Logging;
using SignalSharp.Common;
using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Logging;
using SignalSharp.Utilities;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on fitting an Autoregressive (AR) model for the PELT method.
/// </summary>
/// <remarks>
/// <para>
/// This cost function evaluates the goodness-of-fit of an Autoregressive (AR) model of a specified order <c>p</c>
/// to a segment of the time series data. The cost is defined as the Residual Sum of Squares (RSS)
/// after fitting the model using Ordinary Least Squares (OLS).
/// </para>
/// <para>
/// The AR(p) model predicts the current value based on the <c>p</c> preceding values:
/// <c>signal[t] = c + a1*signal[t-1] + a2*signal[t-2] + ... + ap*signal[t-p] + error[t]</c>
/// where <c>c</c> is an optional intercept term and <c>a1, ..., ap</c> are the AR coefficients.
/// </para>
/// <para>
/// This cost function is useful for detecting changes in the underlying dynamics or autocorrelation
/// structure of a time series. A lower RSS indicates a better fit of the AR model to the segment.
/// </para>
/// <para>
/// Important Notes:
/// <list type="bullet">
///     <item>
///         <description>Minimum Segment Length: The segment must be long enough to both form the AR equations and solve the resulting linear system. The minimum required length is <c>max(order + 1, 2*order + k)</c>, where <c>k</c> is 1 if <c>includeIntercept</c> is true, and 0 otherwise. Shorter segments will cause a <see cref="SegmentLengthException"/>.</description>
///     </item>
///     <item>
///         <description>Constant Data Segments: Fitting an AR model with an intercept to a perfectly constant segment (e.g., <c>[5, 5, 5]</c>) leads to a singular linear system (collinearity between the intercept and lagged variables). This function handles this by returning <c>double.PositiveInfinity</c> cost for such segments. Fitting without an intercept may result in a near-zero cost if the constant value is non-zero.</description>
///     </item>
///     <item>
///          <description>This implementation currently only supports univariate (single-dimensional) time series.</description>
///     </item>
///     <item>
///         <description>When used with information criteria (BIC/AIC), this function estimates the residual variance and uses it to calculate the likelihood metric. The number of parameters includes AR coefficients, intercept (if used), and residual variance.</description>
///     </item>
/// </list>
/// </para>
/// </remarks>
public class ARCostFunction : CostFunctionBase, ILikelihoodCostFunction
{
    private readonly int _order;
    private readonly bool _includeIntercept;
    private double[] _signal = null!;

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ARCostFunction"/> class.
    /// </summary>
    /// <param name="order">The order <c>p</c> of the Autoregressive model (must be >= 1).</param>
    /// <param name="includeIntercept">Whether to include an intercept term (constant) in the AR model. Default is true.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if order is less than 1.</exception>
    public ARCostFunction(int order, bool includeIntercept = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(order, 1, nameof(order));

        _order = order;
        _includeIntercept = includeIntercept;

        _logger = LoggerProvider.CreateLogger<ARCostFunction>();
        _logger.LogDebug("ARCostFunction initialized with Order={Order}, IncludeIntercept={IncludeIntercept}.", _order, _includeIntercept);
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
            throw new ArgumentException($"Signal length must be at least order + 1 ({_order + 1}) for AR({_order}) model.", nameof(signal));
        }
        _logger.LogTrace("Fitting ARCostFunction to 1D signal of length {SignalLength}.", signal.Length);
        _signal = signal;
        return this;
    }

    /// <summary>
    /// Fits the cost function to the provided multi-dimensional time series data.
    /// <remark>
    /// Note: ARCostFunction currently only supports univariate (1D) signals.
    /// </remark>
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
            var message = $"{nameof(ARCostFunction)} currently only supports univariate (1D) signals. Input matrix has {signalMatrix.GetLength(0)} dimensions.";
            _logger.LogError(message);
            throw new NotSupportedException(message);
        }

        var signalLength = signalMatrix.GetLength(1);
        _logger.LogTrace("Fitting ARCostFunction to 1D signal (from matrix) of length {SignalLength}.", signalLength);
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
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than the minimum required length.</exception>
    /// <exception cref="CostFunctionException">Thrown if an unexpected error occurs during fitting.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        UninitializedDataException.ThrowIfUninitialized(_signal, "Fit() must be called before ComputeCost().");

        var startIndex = start ?? 0;
        var endIndex = end ?? _signal.Length;

        _logger.LogTrace("Computing AR cost for segment [{StartIndex}, {EndIndex}).", startIndex, endIndex);
        CheckSegmentValidity(startIndex, endIndex);

        var segmentData = _signal.AsSpan(startIndex, endIndex - startIndex);

        try
        {
            return !FitARAndGetRSS(segmentData, _order, _includeIntercept, startIndex, endIndex, out var rss) ? double.PositiveInfinity : rss;
        }
        catch (Exception ex)
            when (ex is not SegmentLengthException and not ArgumentOutOfRangeException and not UninitializedDataException and not CostFunctionException)
        {
            var message = $"Unexpected error during AR({_order}) model fitting for segment [{startIndex}, {endIndex}). Reason: {ex.Message}";
            _logger.LogError(ex, message);
            throw new CostFunctionException(message, ex);
        }
    }

    /// <summary>
    /// Computes the Gaussian negative log-likelihood metric <c>-2 * logL</c> for the segment, assuming errors are Gaussian.
    /// <c>Metric = n_eff * log(variance_mle)</c>, where <c>variance_mle = RSS / n_eff</c> and <c>n_eff = segmentLength - order</c>.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive).</param>
    /// <param name="end">The end index of the segment (exclusive).</param>
    /// <returns>The computed likelihood metric, or double.PositiveInfinity if the model cannot be fit or variance is near zero.</returns>
    /// <exception cref="UninitializedDataException">Thrown when Fit method has not been called.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is invalid.</exception>
    /// <exception cref="CostFunctionException">Thrown if an unexpected error occurs during calculation.</exception>
    public double ComputeLikelihoodMetric(int start, int end)
    {
        UninitializedDataException.ThrowIfUninitialized(_signal, "Fit() must be called before ComputeLikelihoodMetric().");

        _logger.LogTrace("Computing AR likelihood metric for segment [{StartIndex}, {EndIndex}).", start, end);
        CheckSegmentValidity(start, end);

        var segmentData = _signal.AsSpan(start, end - start);
        var numEquations = (end - start) - _order;

        try
        {
            if (!FitARAndGetRSS(segmentData, _order, _includeIntercept, start, end, out var rss))
            {
                _logger.LogWarning(
                    "AR model fit failed for segment [{StartIndex}, {EndIndex}) during likelihood calculation. Returning +Infinity.",
                    start,
                    end
                );
                return double.PositiveInfinity;
            }

            if (numEquations <= 0)
            {
                _logger.LogWarning(
                    "Number of effective equations ({NumEquations}) is non-positive for segment [{StartIndex}, {EndIndex}) in likelihood calculation. Returning +Infinity.",
                    numEquations,
                    start,
                    end
                );
                return double.PositiveInfinity;
            }

            var varianceMle = rss / numEquations;

            // use variance epsilon for check
            if (NumericUtils.IsEffectivelyZero(varianceMle, Constants.VarianceEpsilon))
            {
                _logger.LogWarning(
                    "Estimated variance ({VarianceMle}) is effectively zero for segment [{StartIndex}, {EndIndex}) in likelihood calculation. Returning +Infinity.",
                    varianceMle,
                    start,
                    end
                );
                return double.PositiveInfinity;
            }

            // core likelihood term (omitting constants like log(2*pi))
            // n_eff * log(var_mle) term dominates BIC/AIC comparisons.
            var metric = numEquations * Math.Log(varianceMle);

            if (double.IsNaN(metric) || double.IsInfinity(metric))
            {
                _logger.LogWarning(
                    "Likelihood metric calculation resulted in NaN or Infinity for segment [{StartIndex}, {EndIndex}) (Variance: {VarianceMle}). Returning +Infinity.",
                    start,
                    end,
                    varianceMle
                );
                return double.PositiveInfinity;
            }

            _logger.LogTrace("Computed AR likelihood metric for segment [{StartIndex}, {EndIndex}): {MetricValue}", start, end, metric);
            return metric;
        }
        catch (Exception ex)
            when (ex is not SegmentLengthException and not ArgumentOutOfRangeException and not UninitializedDataException and not CostFunctionException)
        {
            var message = $"Unexpected error during AR({_order}) likelihood calculation for segment [{start}, {end}). Reason: {ex.Message}";
            _logger.LogError(ex, message);
            throw new CostFunctionException(message, ex);
        }
    }

    /// <summary>
    /// Gets the number of parameters estimated for an AR(p) model segment.
    /// Includes AR coefficients, intercept (if applicable), and residual variance.
    /// </summary>
    /// <param name="segmentLength">The length of the segment (unused in this implementation as parameter count depends only on order and intercept).</param>
    /// <returns>Number of parameters: p (coeffs) + 1 (variance) [+ 1 (intercept)].</returns>
    public int GetSegmentParameterCount(int segmentLength)
    {
        // parameters: p AR coefficients + 1 residual variance + 1 intercept (if included)
        var count = _order + 1 + (_includeIntercept ? 1 : 0);
        _logger.LogTrace("Parameter count for AR({Order}, Intercept={IncludeIntercept}) is {ParameterCount}.", _order, _includeIntercept, count);
        return count;
    }

    /// <summary>
    /// Indicates that this cost function provides likelihood metrics suitable for BIC/AIC.
    /// </summary>
    public bool SupportsInformationCriteria => true;

    /// <summary>
    /// Checks if the segment indices and length are valid for AR model fitting.
    /// </summary>
    private void CheckSegmentValidity(int startIndex, int endIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(startIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _signal.Length, nameof(endIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, endIndex, nameof(startIndex)); // Ensure start <= end

        var segmentLength = endIndex - startIndex;

        // minimum length needed to form the design matrix and target vector (p lags + 1 current point)
        var minFormationLength = _order + 1;
        // minimum length needed to have at least as many equations as parameters for OLS solver
        // equations = n - p. parameters = p [+ 1 intercept]; need n - p >= p [+ 1 intercept]
        var minSolverLength = _includeIntercept ? (2 * _order + 1) : (2 * _order);
        var minRequiredLength = Math.Max(minFormationLength, minSolverLength);

        SegmentLengthException.ThrowIfInvalid(
            segmentLength,
            minRequiredLength,
            $"AR({_order}, Intercept={_includeIntercept}) model fitting requires at least {minRequiredLength} points in a segment, but got {segmentLength} for [{startIndex}, {endIndex})."
        );
    }

    /// <summary>
    /// Fits an AR(p) model to the data segment using OLS and returns the Residual Sum of Squares (RSS).
    /// </summary>
    /// <param name="segmentData">The data segment.</param>
    /// <param name="order">The AR order p.</param>
    /// <param name="includeIntercept">Whether to include an intercept term.</param>
    /// <param name="startIndex">Original start index (for logging).</param>
    /// <param name="endIndex">Original end index (for logging).</param>
    /// <param name="rss">Output: Residual Sum of Squares (RSS) if successful.</param>
    /// <returns>True if the model was fit successfully, false otherwise (e.g., singular matrix).</returns>
    private bool FitARAndGetRSS(ReadOnlySpan<double> segmentData, int order, bool includeIntercept, int startIndex, int endIndex, out double rss)
    {
        rss = double.NaN;
        var n = segmentData.Length;
        var numEquations = n - order;
        // this check should technically be covered by CheckSegmentValidity, but double-check here
        if (numEquations <= 0)
        {
            _logger.LogError(
                "Internal error: FitARAndGetRSS called with insufficient effective points ({NumEquations}) for segment [{StartIndex}, {EndIndex}).",
                numEquations,
                startIndex,
                endIndex
            );
            return false;
        }

        var numPredictors = includeIntercept ? order + 1 : order;
        var designMatrix = new double[numEquations, numPredictors];
        var targetVector = new double[numEquations];

        // construct design matrix and target vector
        for (var i = 0; i < numEquations; i++)
        {
            var currentTime = order + i; // index in segmentData for the current target value
            targetVector[i] = segmentData[currentTime];

            var predictorCol = 0;
            if (includeIntercept)
            {
                designMatrix[i, predictorCol++] = 1.0;
            }

            // fill lagged predictors: y[t-1], y[t-2], ..., y[t-p]
            // these correspond to segmentData indices: currentTime-1, currentTime-2, ..., currentTime-order
            for (var lag = 1; lag <= order; lag++)
            {
                designMatrix[i, predictorCol++] = segmentData[currentTime - lag];
            }
        }

        if (includeIntercept && IsSegmentConstant(segmentData))
        {
            _logger.LogWarning(
                "AR({Order}, Intercept=true) fitting cannot proceed for constant data segment [{StartIndex}, {EndIndex}] due to perfect collinearity. Returning infinite cost.",
                order,
                startIndex,
                endIndex
            );
            rss = double.PositiveInfinity; // explicit assignment for clarity
            return false; // signal failure, cost function should handle this
        }

        if (!MatrixOperations.TrySolveLinearSystemQR(designMatrix, targetVector, out var coefficients) || coefficients == null)
        {
            _logger.LogWarning(
                "AR({Order}, Intercept={IncludeIntercept}) solver failed for segment [{StartIndex}, {EndIndex}] likely due to singular matrix or collinearity. Returning failure.",
                order,
                includeIntercept,
                startIndex,
                endIndex
            );
            return false; // solver failed
        }

        // compute residuals and RSS
        var currentRss = 0.0;
        var predictedVector = MatrixOperations.Multiply(designMatrix, coefficients); // Y_hat = X * coeffs

        for (var i = 0; i < numEquations; i++)
        {
            var residual = targetVector[i] - predictedVector[i];
            currentRss += residual * residual;
        }

        if (double.IsNaN(currentRss) || double.IsInfinity(currentRss))
        {
            _logger.LogWarning(
                "AR({Order}, Intercept={IncludeIntercept}) fitting resulted in NaN/Infinite RSS for segment [{StartIndex}, {EndIndex}]. Returning failure.",
                order,
                includeIntercept,
                startIndex,
                endIndex
            );
            return false; // calculation resulted in invalid RSS
        }

        rss = currentRss;
        _logger.LogTrace(
            "Successfully fit AR({Order}, Intercept={IncludeIntercept}) for segment [{StartIndex}, {EndIndex}) with RSS={RssValue}.",
            order,
            includeIntercept,
            startIndex,
            endIndex,
            rss
        );
        return true;
    }

    /// <summary>
    /// Checks if a segment contains effectively constant data using default tolerance.
    /// </summary>
    private static bool IsSegmentConstant(ReadOnlySpan<double> segmentData)
    {
        if (segmentData.Length <= 1)
            return true;

        var firstVal = segmentData[0];
        for (var i = 1; i < segmentData.Length; i++)
        {
            // use default epsilon for double comparison
            if (!NumericUtils.AreApproximatelyEqual(segmentData[i], firstVal))
            {
                return false;
            }
        }
        return true;
    }
}
