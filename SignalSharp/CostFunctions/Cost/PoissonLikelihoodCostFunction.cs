using Microsoft.Extensions.Logging;
using SignalSharp.Common;
using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Logging;
using SignalSharp.Utilities;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on the Poisson negative log-likelihood for the PELT algorithm.
/// This cost function is sensitive to changes in the rate (mean) of events in count data.
/// </summary>
/// <remarks>
/// <para>
/// This cost function assumes that the data within each segment represents counts following a Poisson distribution
/// independently for each dimension, with a constant rate parameter (<c>λ</c>) for that segment and dimension.
/// It calculates the cost based on the negative log-likelihood of the segment data given its estimated rate (Maximum Likelihood Estimate - MLE).
/// </para>
/// <para>
/// The MLE for the rate <c>λ</c> in a segment [start, end) of length <c>n = end - start</c> for a given dimension is the sample mean:
/// <c>λ_hat = (Sum_{i=start}^{end-1} signal[dim, i]) / n = S / n</c>, where <c>S</c> is the sum of counts for that dimension.
/// </para>
/// <para>
/// The likelihood metric used for BIC/AIC calculations, proportional to <c>-2 * log-likelihood</c>, is calculated as:
/// <c>Metric(start, end) = Sum_dimensions [ 2 * ( S - S * log(S) + S * log(n) ) ]</c>
/// where <c>S = Sum_{i=start}^{end-1} signal[dim, i]</c> is the sum of counts in the segment for that dimension, and <c>n = end - start</c> is the segment length.
/// This formula assumes the convention <c>0 * log(0) = 0</c>, which is handled by setting the metric contribution to 0 when <c>S=0</c> for a dimension.
/// The term <c>Sum log(signal[dim, i]!)</c> from the full likelihood is omitted as it depends only on the data points themselves.
/// The <see cref="ComputeCost"/> method returns this same metric value.
/// </para>
/// <para>
/// This cost/metric is calculated efficiently using precomputed prefix sums of the signal,
/// allowing <c>O(D)</c> calculation per segment after an <c>O(N*D)</c> precomputation step during <see cref="Fit(double[,])"/>,
/// where D is the number of dimensions and N is the number of time points.
/// </para>
/// <para>
/// Consider using the Poisson Likelihood cost function when:
/// <list type="bullet">
///     <item><description>Your data represents counts of events per interval (e.g., website hits per day, defects per batch, calls per hour) for one or more dimensions.</description></item>
///     <item><description>You expect changes in the average rate of these events.</description></item>
///     <item><description>The data within segments can be reasonably approximated by a Poisson distribution (variance roughly equals mean).</description></item>
///     <item><description>The input data contains non-negative values (counts cannot be negative).</description></item>
/// </list>
/// </para>
/// <para>
/// Note: While the function accepts <c>double</c> inputs, Poisson counts are theoretically non-negative integers. This implementation requires input data to be effectively non-negative (values <c>>= -Epsilon</c>). Values slightly below zero but within tolerance will be clamped to zero. Significantly negative values will cause an exception during <see cref="Fit(double[,])"/>.
/// </para>
/// </remarks>
public class PoissonLikelihoodCostFunction : CostFunctionBase, ILikelihoodCostFunction
{
    private int _numDimensions;
    private int _numPoints;
    private double[,] _prefixSum = null!;

    private readonly ILogger _logger;

    private const double Two = 2.0;
    private const double Epsilon = Constants.DefaultEpsilon; // Use standard epsilon for non-negativity check

    /// <summary>
    /// Initializes a new instance of the <see cref="PoissonLikelihoodCostFunction"/> class.
    /// </summary>
    public PoissonLikelihoodCostFunction()
    {
        _logger = LoggerProvider.CreateLogger<PoissonLikelihoodCostFunction>();
    }

    /// <summary>
    /// Fits the cost function to the provided count data by precomputing prefix sums.
    /// </summary>
    /// <param name="signalMatrix">The count data array to fit (rows=dimensions, columns=time points). Values must be effectively non-negative (>= -Epsilon).</param>
    /// <returns>The fitted <see cref="PoissonLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This method performs <c>O(N*D)</c> computation to calculate prefix sums, enabling <c>O(D)</c> cost/metric calculation per segment later.
    /// It must be called before cost/metric computation methods.
    /// It validates that all input data points are non-negative within a small tolerance (<c>Epsilon</c>). Values slightly below zero but within tolerance will be clamped to zero for the sum.
    /// </para>
    /// <example>
    /// <code>
    /// // Example: Number of website hits per hour
    /// double[,] counts = { { 5, 8, 6, 7, 25, 30, 28, 10, 9, 12 } };
    /// var poissonCost = new PoissonLikelihoodCostFunction();
    /// poissonCost.Fit(counts);
    ///
    /// // Example with near-zero value
    /// double[,] countsNearZero = { { 5, 8, 1e-10, 7, 25, 30, -1e-11, 10, 9, 12 } };
    /// poissonCost.Fit(countsNearZero); // Should work
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="signalMatrix"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any data point in <paramref name="signalMatrix"/> is less than -<c>Epsilon</c>.</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        _logger.LogTrace("Fitting PoissonLikelihoodCostFunction to {Rows}D signal of length {Length}.", signalMatrix.GetLength(0), signalMatrix.GetLength(1));

        _numDimensions = signalMatrix.GetLength(0);
        _numPoints = signalMatrix.GetLength(1);

        _prefixSum = new double[_numDimensions, _numPoints + 1];

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            for (var i = 0; i < _numPoints; i++)
            {
                var value = signalMatrix[dim, i];
                if (value < -Epsilon)
                {
                    var message = $"Input data must be non-negative (>= -{Epsilon}) for Poisson Likelihood cost. Found negative value at [{dim}, {i}]: {value}";
                    _logger.LogError(message);
                    throw new ArgumentException(message, nameof(signalMatrix));
                }

                // clamp slightly negative values to zero for the sum
                var valueToAdd = Math.Max(0.0, value);

                _prefixSum[dim, i + 1] = _prefixSum[dim, i] + valueToAdd;
            }
        }
        _logger.LogDebug("Prefix sums computed successfully.");
        return this;
    }

    /// <summary>
    /// Computes the cost for a segment [start, end) based on the Poisson negative log-likelihood.
    /// The cost is <c>Sum_dimensions [ 2 * ( S - S * log(S) + S * log(n) ) ]</c>, where S is the sum of counts and n is the length.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>
    /// Calculates the cost in <c>O(D)</c> time using precomputed prefix sums, where D is the number of dimensions.
    /// Handles the <c>segmentSum = 0</c> case correctly based on the limit <c>x*log(x) -> 0</c> as <c>x -> 0</c>, resulting in zero cost contribution for dimensions with zero total count.
    /// Must be called after <see cref="Fit(double[,])"/>. This method returns the same value as <see cref="ComputeLikelihoodMetric"/>.
    /// </para>
    /// <example>
    /// <code>
    /// // Assuming 'counts' data from Fit example
    /// var poissonCost = new PoissonLikelihoodCostFunction().Fit(counts);
    /// double costSegment1 = poissonCost.ComputeCost(0, 4); // Cost for segment with lower counts
    /// double costSegment2 = poissonCost.ComputeCost(4, 7); // Cost for the segment with higher counts
    ///
    /// // Example with zero-sum segment
    /// double[,] zeroCounts = { { 0, 0, 0, 5, 5 } };
    /// var zeroCost = new PoissonLikelihoodCostFunction().Fit(zeroCounts);
    /// double costZeroSeg = zeroCost.ComputeCost(0, 3); // Should be 0.0
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (<see cref="Fit(double[,])"/> not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices (<paramref name="start"/>, <paramref name="end"/>) are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length (<c>end - start</c>) is less than 1.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        return ComputeLikelihoodMetricInternal(start, end, "ComputeCost");
    }

    /// <summary>
    /// Computes the likelihood metric for a segment [start, end) based on the Poisson negative log-likelihood.
    /// The metric is <c>Sum_dimensions [ 2 * ( S - S * log(S) + S * log(n) ) ]</c>, where S is the sum of counts and n is the length.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive).</param>
    /// <param name="end">The end index of the segment (exclusive).</param>
    /// <returns>The computed likelihood metric for the segment.</returns>
    /// <remarks>
    /// <para>
    /// Calculates the metric in <c>O(D)</c> time using precomputed prefix sums.
    /// Handles the <c>segmentSum = 0</c> case correctly.
    /// Must be called after <see cref="Fit(double[,])"/>. This method returns the same value as <see cref="ComputeCost"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (<see cref="Fit(double[,])"/> not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices (<paramref name="start"/>, <paramref name="end"/>) are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length (<c>end - start</c>) is less than 1.</exception>
    public double ComputeLikelihoodMetric(int start, int end)
    {
        return ComputeLikelihoodMetricInternal(start, end, "ComputeLikelihoodMetric");
    }

    /// <summary>
    /// Gets the number of parameters estimated for a Poisson model segment.
    /// This is 1 parameter (the rate 'λ') per dimension.
    /// </summary>
    /// <param name="segmentLength">The length of the segment (unused).</param>
    /// <returns>Number of parameters: Number of dimensions * 1.</returns>
    public int GetSegmentParameterCount(int segmentLength)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSum, "Fit() must be called before GetSegmentParameterCount().");
        _logger.LogTrace(
            "Parameter count for Poisson is 1 per dimension, total {ParameterCount} for {NumDimensions} dimensions.",
            _numDimensions,
            _numDimensions
        );
        // 1 parameter (lambda) per dimension
        return _numDimensions;
    }

    /// <summary>
    /// Indicates that this cost function provides likelihood metrics suitable for BIC/AIC.
    /// </summary>
    public bool SupportsInformationCriteria => true;

    private double ComputeLikelihoodMetricInternal(int? start, int? end, string callerName)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSum, $"Fit() must be called before {callerName}().");

        if (_numDimensions == 0 || _numPoints == 0)
            return 0;

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, endIndex, nameof(start));

        var segmentLength = endIndex - startIndex;
        SegmentLengthException.ThrowIfInvalid(segmentLength, 1);

        _logger.LogTrace(
            "Calculating Poisson likelihood metric for segment [{StartIndex}, {EndIndex}) (Length: {SegmentLength}).",
            startIndex,
            endIndex,
            segmentLength
        );

        double totalMetric = 0;
        var logSegmentLength = Math.Log(segmentLength); // Calculate once
        var sumTolerance = NumericUtils.GetDefaultEpsilon<double>(); // Tolerance for checking if sum is zero

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            var segmentSum = _prefixSum[dim, endIndex] - _prefixSum[dim, startIndex];
            double metricDim;

            // edge case: if sum is effectively 0, the MLE rate is 0, metric contribution is 0
            if (NumericUtils.IsEffectivelyZero(segmentSum, sumTolerance))
            {
                metricDim = 0.0;
                _logger.LogTrace("Segment [{StartIndex}, {EndIndex}), Dim {Dimension} has Sum ~= 0. Metric Contribution = 0.", startIndex, endIndex, dim);
            }
            else
            {
                // Metric = 2 * [ S - S * log(S) + S * log(n) ]
                var logSegmentSum = Math.Log(segmentSum); // S > sumTolerance, so log is safe
                metricDim = Two * (segmentSum - segmentSum * logSegmentSum + segmentSum * logSegmentLength);
                _logger.LogTrace(
                    "Segment [{StartIndex}, {EndIndex}), Dim {Dimension}: S={S}, n={N}. Metric Contribution = {MetricValue}",
                    startIndex,
                    endIndex,
                    dim,
                    segmentSum,
                    segmentLength,
                    metricDim
                );
            }

            if (double.IsNaN(metricDim) || double.IsInfinity(metricDim))
            {
                _logger.LogWarning(
                    "Metric calculation resulted in NaN or Infinity for dimension {Dimension} in segment [{StartIndex}, {EndIndex}). Returning PositiveInfinity.",
                    dim,
                    startIndex,
                    endIndex
                );
                return double.PositiveInfinity;
            }

            totalMetric += metricDim;
        }

        _logger.LogTrace("Total Poisson likelihood metric for segment [{StartIndex}, {EndIndex}): {TotalMetric}", startIndex, endIndex, totalMetric);
        return totalMetric;
    }
}
