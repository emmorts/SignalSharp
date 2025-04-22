using Microsoft.Extensions.Logging;
using SignalSharp.Common;
using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Logging;
using SignalSharp.Utilities;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on the Gaussian negative log-likelihood for the PELT algorithm.
/// This cost function is sensitive to changes in both the mean and the variance of the signal.
/// </summary>
/// <remarks>
/// <para>
/// This cost function assumes that the data within each segment follows a Gaussian (normal) distribution,
/// independently for each dimension. It calculates the cost based on the negative log-likelihood of the
/// segment data given its estimated mean and variance (Maximum Likelihood Estimates - MLE).
/// </para>
/// <para>
/// The likelihood metric (proportional to -2 * log-likelihood) for a segment [start, end) of length <c>n = end - start</c>
/// is dominated by the term <c>Sum_dimensions [ n * log(σ²_hat_dim) ]</c>, where <c>σ²_hat_dim</c> is the MLE of the variance
/// for that segment and dimension. This makes the function sensitive to shifts in both central tendency (mean)
/// and dispersion (variance). The <see cref="ComputeCost"/> and <see cref="ComputeLikelihoodMetric"/> methods both return this metric value.
/// </para>
/// <para>
/// This cost/metric is calculated efficiently using precomputed prefix sums of the signal and its squares,
/// allowing O(D) calculation per segment after an O(N*D) precomputation step during <see cref="Fit(double[,])"/>.
/// </para>
/// <para>
/// Consider using the Gaussian Likelihood cost function when:
/// <list type="bullet">
///     <item><description>You expect changes in both the mean and variance of your signal.</description></item>
///     <item><description>The data within segments can be reasonably approximated by a normal distribution.</description></item>
///     <item><description>You need a statistically principled way to evaluate segment homogeneity under Gaussian assumptions.</description></item>
/// </list>
/// </para>
/// <para>
/// Note: This implementation uses the MLE for variance (dividing sum of squared deviations by <c>n</c>). If the segment variance
/// is zero or numerically very close to zero (e.g., all points in the segment are identical), the logarithm
/// would be undefined or negative infinity. To handle this, a small minimum variance floor based on <see cref="Constants.VarianceEpsilon"/>
/// is used to ensure numerical stability. This effectively assigns a very high cost/metric to zero-variance segments,
/// preventing issues with `log(0)` and ensuring such segments are unlikely to be chosen unless absolutely necessary.
/// </para>
/// </remarks>
public class GaussianLikelihoodCostFunction : CostFunctionBase, ILikelihoodCostFunction
{
    private int _numDimensions;
    private int _numPoints;
    private double[,] _prefixSum = null!;
    private double[,] _prefixSumSq = null!;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GaussianLikelihoodCostFunction"/> class.
    /// </summary>
    public GaussianLikelihoodCostFunction()
    {
        _logger = LoggerProvider.CreateLogger<GaussianLikelihoodCostFunction>();
    }

    /// <summary>
    /// Fits the cost function to the provided data by precomputing prefix sums.
    /// </summary>
    /// <param name="signalMatrix">The data array to fit (rows=dimensions, columns=time points).</param>
    /// <returns>The fitted <see cref="GaussianLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This method performs O(N*D) computation to calculate prefix sums of the signal and its squares,
    /// enabling O(D) cost/metric calculation per segment later. It must be called before cost/metric computation methods.
    /// </para>
    /// <example>
    /// <code>
    /// double[,] data = { { 1.0, 1.1, 1.0, 5.0, 5.1, 4.9 } };
    /// var gaussianCost = new GaussianLikelihoodCostFunction();
    /// gaussianCost.Fit(data);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="signalMatrix"/> is null.</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        _logger.LogTrace("Fitting GaussianLikelihoodCostFunction to {Rows}D signal of length {Length}.", signalMatrix.GetLength(0), signalMatrix.GetLength(1));

        _numDimensions = signalMatrix.GetLength(0);
        _numPoints = signalMatrix.GetLength(1);

        _prefixSum = new double[_numDimensions, _numPoints + 1];
        _prefixSumSq = new double[_numDimensions, _numPoints + 1];

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            for (var i = 0; i < _numPoints; i++)
            {
                var value = signalMatrix[dim, i];
                _prefixSum[dim, i + 1] = _prefixSum[dim, i] + value;
                _prefixSumSq[dim, i + 1] = _prefixSumSq[dim, i] + value * value;
            }
        }

        _logger.LogDebug("Prefix sums computed successfully.");
        return this;
    }

    /// <summary>
    /// Computes the cost for a segment [start, end) based on the Gaussian negative log-likelihood.
    /// The cost is <c>Sum_dimensions [ n * log(variance_mle_dim) ]</c>.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>
    /// Calculates the cost in O(D) time using precomputed prefix sums, where D is the number of dimensions.
    /// Handles potential zero variance by using a small minimum variance floor to prevent `log(0)`.
    /// Must be called after <see cref="Fit(double[,])"/>. This method returns the same value as <see cref="ComputeLikelihoodMetric"/>.
    /// </para>
    /// <example>
    /// <code>
    /// var gaussianCost = new GaussianLikelihoodCostFunction().Fit(data);
    /// double cost = gaussianCost.ComputeCost(0, 10); // Cost for segment from index 0 up to (but not including) 10
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (<see cref="Fit(double[,])"/> not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    /// <exception cref="CostFunctionException">Thrown if an unexpected error occurs during cost calculation.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        // This method computes the same value as the likelihood metric
        return ComputeLikelihoodMetricInternal(start, end, "ComputeCost");
    }

    /// <summary>
    /// Computes the likelihood metric for a segment [start, end) based on the Gaussian negative log-likelihood.
    /// The metric is <c>Sum_dimensions [ n * log(variance_mle_dim) ]</c>.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive).</param>
    /// <param name="end">The end index of the segment (exclusive).</param>
    /// <returns>The computed likelihood metric for the segment. Returns <c>double.PositiveInfinity</c> if the metric cannot be computed (e.g., due to numerical issues).</returns>
    /// <remarks>
    /// <para>
    /// Calculates the metric in O(D) time using precomputed prefix sums.
    /// Handles potential zero variance by using a small minimum variance floor.
    /// Must be called after <see cref="Fit(double[,])"/>. This method returns the same value as <see cref="ComputeCost"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (<see cref="Fit(double[,])"/> not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices (<paramref name="start"/>, <paramref name="end"/>) are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length (<c>end - start</c>) is less than 1.</exception>
    /// <exception cref="CostFunctionException">Thrown if an unexpected error occurs during metric calculation.</exception>
    public double ComputeLikelihoodMetric(int start, int end)
    {
        return ComputeLikelihoodMetricInternal(start, end, "ComputeLikelihoodMetric");
    }

    /// <summary>
    /// Gets the number of parameters estimated for a Gaussian model segment.
    /// This is 2 parameters (mean and variance) per dimension.
    /// </summary>
    /// <param name="segmentLength">The length of the segment (unused).</param>
    /// <returns>Number of parameters: Number of dimensions * 2.</returns>
    /// <exception cref="UninitializedDataException">Thrown if <see cref="Fit(double[,])"/> has not been called.</exception>
    public int GetSegmentParameterCount(int segmentLength)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSum, "Fit() must be called before GetSegmentParameterCount().");
        var paramCount = _numDimensions * 2;
        _logger.LogTrace("Parameter count for Gaussian is 2 per dimension (mean, variance), total {ParameterCount} for {NumDimensions} dimensions.", paramCount, _numDimensions);
        return paramCount;
    }

    /// <summary>
    /// Indicates that this cost function provides likelihood metrics suitable for BIC/AIC.
    /// </summary>
    public bool SupportsInformationCriteria => true;

    private double ComputeLikelihoodMetricInternal(int? start, int? end, string callerName)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSum, $"Fit() must be called before {callerName}().");
        UninitializedDataException.ThrowIfUninitialized(_prefixSumSq, $"Fit() must be called before {callerName}().");

        if (_numDimensions == 0 || _numPoints == 0) return 0;

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, endIndex, nameof(start)); // ensure start <= end

        var segmentLength = endIndex - startIndex;
        SegmentLengthException.ThrowIfInvalid(segmentLength, 1);

        _logger.LogTrace("Calculating Gaussian likelihood metric for segment [{StartIndex}, {EndIndex}) (Length: {SegmentLength}).", startIndex, endIndex, segmentLength);

        double totalMetric = 0;
        var varianceEpsilon = NumericUtils.GetVarianceEpsilon<double>(); // Use specific epsilon for variance

        try
        {
            for (var dim = 0; dim < _numDimensions; dim++)
            {
                var segmentSum = _prefixSum[dim, endIndex] - _prefixSum[dim, startIndex];
                var segmentSumSq = _prefixSumSq[dim, endIndex] - _prefixSumSq[dim, startIndex];

                // Sum of squared deviations: Sum(x^2) - (Sum(x))^2 / n
                var sumSqDev = segmentSumSq - (segmentSum * segmentSum) / segmentLength;

                // Calculate MLE variance: SumSqDev / n
                // Clamp variance to a small positive number (varianceEpsilon) to avoid log(0) or log(<0)
                // This handles segments with identical values robustly.
                var varianceMle = Math.Max(sumSqDev, 0.0) / segmentLength; // Calculate raw MLE var
                var clampedVarianceMle = Math.Max(varianceMle, varianceEpsilon); // Apply floor

                if (varianceMle < varianceEpsilon)
                {
                    _logger.LogTrace("Segment [{StartIndex}, {EndIndex}), Dim {Dimension}: Raw variance {RawVariance} is near zero. Clamped to {ClampedVariance} using VarianceEpsilon {VarianceEpsilon}.",
                                     startIndex, endIndex, dim, varianceMle, clampedVarianceMle, varianceEpsilon);
                }

                // Metric for this dimension is n * log(variance_mle)
                // Note: Additive constants like n*log(2pi)+n from the full -2*logL are omitted
                // as they depend only on n and usually cancel in PELT comparisons or information criteria.
                var metricDim = segmentLength * Math.Log(clampedVarianceMle);

                if (double.IsNaN(metricDim) || double.IsInfinity(metricDim))
                {
                    _logger.LogWarning("Metric calculation resulted in NaN or Infinity for dimension {Dimension} in segment [{StartIndex}, {EndIndex}). Clamped Variance: {ClampedVariance}. Returning PositiveInfinity.",
                                       dim, startIndex, endIndex, clampedVarianceMle);
                    return double.PositiveInfinity;
                }
                _logger.LogTrace("Segment [{StartIndex}, {EndIndex}), Dim {Dimension}: Sum={Sum}, SumSq={SumSq}, SumSqDev={SumSqDev}, VarMLE={VarMLE}, ClampedVarMLE={ClampedVar}, Metric={MetricValue}",
                                 startIndex, endIndex, dim, segmentSum, segmentSumSq, sumSqDev, varianceMle, clampedVarianceMle, metricDim);

                totalMetric += metricDim;
            }

            if (double.IsNaN(totalMetric) || double.IsInfinity(totalMetric))
            {
                _logger.LogWarning("Total Gaussian likelihood metric calculation resulted in NaN or Infinity for segment [{StartIndex}, {EndIndex}). Returning PositiveInfinity.", startIndex, endIndex);
                return double.PositiveInfinity;
            }

            _logger.LogTrace("Total Gaussian likelihood metric for segment [{StartIndex}, {EndIndex}): {TotalMetric}", startIndex, endIndex, totalMetric);
            return totalMetric;
        }
        catch (Exception ex) when (ex is not SegmentLengthException and not ArgumentOutOfRangeException and not UninitializedDataException and not CostFunctionException)
        {
            var message = $"Unexpected error during Gaussian likelihood calculation for segment [{startIndex}, {endIndex}). Reason: {ex.Message}";
            _logger.LogError(ex, message);
            throw new CostFunctionException(message, ex);
        }
    }
}