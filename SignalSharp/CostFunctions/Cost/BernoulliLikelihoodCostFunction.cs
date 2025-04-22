using Microsoft.Extensions.Logging;
using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Logging;
using SignalSharp.Utilities;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on the Binomial (specifically Bernoulli) negative log-likelihood for the PELT algorithm.
/// This cost function is sensitive to changes in the probability of success in binary (0/1) data.
/// </summary>
/// <remarks>
/// <para>
/// This cost function assumes that the data within each segment represents a sequence of independent Bernoulli trials
/// (outcomes are 0 or 1, e.g., failure/success) with a constant probability of success (<c>p</c>) for that segment.
/// It calculates the cost based on the negative log-likelihood of the segment data given its estimated success probability (Maximum Likelihood Estimate - MLE).
/// </para>
/// <para>
/// The MLE for the success probability <c>p</c> in a segment [start, end) of length <c>n = end - start</c> is the sample proportion of successes:
/// <c>p_hat = (Sum_{i=start}^{end-1} signal[i]) / n = S / n</c>, where <c>S</c> is the number of successes (sum of 1s).
/// </para>
/// <para>
/// The likelihood metric used for BIC/AIC calculations, typically proportional to <c>-2 * log-likelihood</c>, is calculated as:
/// <c>Metric(start, end) = -2 * [ S * log(S) + (n-S) * log(n-S) - n * log(n) ]</c>
/// where <c>S</c> is the number of successes (sum of 1s), <c>n</c> is the segment length, and <c>n-S</c> is the number of failures (sum of 0s).
/// This formula uses the convention <c>0 * log(0) = 0</c>, which is handled explicitly in the implementation for the edge cases where S=0 (all failures) or S=n (all successes). In these edge cases, the metric is 0, representing a perfect fit.
/// The <see cref="ComputeCost"/> method returns this same metric value.
/// </para>
/// <para>
/// This cost/metric is calculated efficiently using precomputed prefix sums of the signal (sum of 1s),
/// allowing <c>O(D)</c> calculation per segment after an <c>O(N*D)</c> precomputation step during <see cref="Fit(double[,])"/>, where D is the number of dimensions and N is the number of time points.
/// </para>
/// <para>
/// Consider using the Bernoulli Likelihood cost function when:
/// <list type="bullet">
///     <item><description>Your data consists of binary outcomes (0s and 1s) over time (e.g., machine status up/down, test pass/fail, presence/absence).</description></item>
///     <item><description>You expect changes in the underlying probability of the '1' outcome.</description></item>
///     <item><description>The data within segments can be reasonably approximated by a sequence of independent Bernoulli trials with a constant success probability.</description></item>
///     <item><description>The input data must contain only values numerically close to 0 or 1.</description></item>
/// </list>
/// </para>
/// <para>
/// Note: This implementation requires input data to be strictly 0 or 1 (or numerically very close, within the default double-precision tolerance). Other values will cause an exception during <see cref="Fit(double[,])"/>.
/// </para>
/// </remarks>
public class BernoulliLikelihoodCostFunction : CostFunctionBase, ILikelihoodCostFunction
{
    private int _numDimensions;
    private int _numPoints;
    private double[,] _prefixSumSuccesses = null!;

    private readonly ILogger _logger;

    private const double MinusTwo = -2.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BernoulliLikelihoodCostFunction"/> class.
    /// </summary>
    public BernoulliLikelihoodCostFunction()
    {
        _logger = LoggerProvider.CreateLogger<BernoulliLikelihoodCostFunction>();
    }

    /// <summary>
    /// Fits the cost function to the provided binary (0/1) data by precomputing prefix sums of successes.
    /// </summary>
    /// <param name="signalMatrix">The binary data array to fit (rows=dimensions, columns=time points). Values must be effectively 0 or 1 (within default tolerance).</param>
    /// <returns>The fitted <see cref="BernoulliLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This method performs <c>O(N*D)</c> computation to calculate prefix sums, enabling <c>O(D)</c> cost/metric calculation per segment later via <see cref="ComputeCost"/> and <see cref="ComputeLikelihoodMetric"/>.
    /// It must be called before cost/metric computation methods.
    /// It validates that all input data points are close to either 0 or 1 using the default double-precision epsilon from <see cref="NumericUtils"/>.
    /// Values are effectively clamped to 0 or 1 for the internal sum calculation.
    /// </para>
    /// <example>
    /// <code>
    /// // Example: Machine status (1=up, 0=down)
    /// double[,] status = { { 1.0, 1.0, 1.0, 0.9999999999, 0.0, 0.0000000001, 0.0, 1.0, 1.0, 1.0 } };
    /// var bernoulliCost = new BernoulliLikelihoodCostFunction();
    /// bernoulliCost.Fit(status);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="signalMatrix"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any data point in <paramref name="signalMatrix"/> is not close to 0 or 1 within the default tolerance.</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        _logger.LogTrace("Fitting BernoulliLikelihoodCostFunction to {Rows}D signal of length {Length}.", signalMatrix.GetLength(0), signalMatrix.GetLength(1));

        _numDimensions = signalMatrix.GetLength(0);
        _numPoints = signalMatrix.GetLength(1);

        _prefixSumSuccesses = new double[_numDimensions, _numPoints + 1];
        var epsilon = NumericUtils.GetDefaultEpsilon<double>();

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            for (var i = 0; i < _numPoints; i++)
            {
                var value = signalMatrix[dim, i];

                var isNearZero = NumericUtils.IsEffectivelyZero(value, epsilon);
                var isNearOne = NumericUtils.AreApproximatelyEqual(value, 1.0, epsilon);

                if (!isNearZero && !isNearOne)
                {
                    var message = $"Input data must be effectively 0 or 1 (within epsilon={epsilon}) for Bernoulli Likelihood cost. Found value at [{dim}, {i}]: {value}";
                    _logger.LogError(message);
                    throw new ArgumentException(message, nameof(signalMatrix));
                }

                // clamp the value to exactly 0.0 or 1.0 for the sum calculation
                var valueToAdd = isNearOne ? 1.0 : 0.0;

                _prefixSumSuccesses[dim, i + 1] = _prefixSumSuccesses[dim, i] + valueToAdd;
            }
        }
        _logger.LogDebug("Prefix sums computed successfully.");
        return this;
    }

    /// <summary>
    /// Computes the cost for a segment [start, end) based on the Bernoulli negative log-likelihood.
    /// Cost = <c>-2 * [ S*log(S) + (n-S)*log(n-S) - n*log(n) ]</c> where <c>S</c> = successes, <c>n</c> = length.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment (0 if all successes or all failures, positive otherwise).</returns>
    /// <remarks>
    /// <para>
    /// Calculates the cost in <c>O(D)</c> time using precomputed prefix sums, where D is the number of dimensions.
    /// Correctly handles the edge cases where the segment contains only 0s or only 1s (cost is 0).
    /// Must be called after <see cref="Fit(double[,])"/>. This method returns the same value as <see cref="ComputeLikelihoodMetric"/>.
    /// </para>
    /// <example>
    /// <code>
    /// // Assuming 'status' data from Fit example
    /// var bernoulliCost = new BernoulliLikelihoodCostFunction().Fit(status);
    /// double costSegment1 = bernoulliCost.ComputeCost(0, 4); // Cost for segment of ~all 1s (should be close to 0)
    /// double costSegment2 = bernoulliCost.ComputeCost(4, 7); // Cost for segment of ~all 0s (should be close to 0)
    /// double costSegmentMix = bernoulliCost.ComputeCost(0, 7); // Cost for mixed segment (positive)
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (<see cref="Fit(double[,])"/> not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices (<paramref name="start"/>, <paramref name="end"/>) are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length (<c>end - start</c>) is less than 1.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        // this method computes the same value as the likelihood metric
        return ComputeLikelihoodMetricInternal(start, end, "ComputeCost");
    }

    /// <summary>
    /// Computes the likelihood metric for a segment [start, end) based on the Bernoulli negative log-likelihood.
    /// Metric = <c>-2 * [ S*log(S) + (n-S)*log(n-S) - n*log(n) ]</c> where <c>S</c> = successes, <c>n</c> = length.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive).</param>
    /// <param name="end">The end index of the segment (exclusive).</param>
    /// <returns>The computed likelihood metric for the segment (0 if all successes or all failures, positive otherwise).</returns>
    /// <remarks>
    /// <para>
    /// Calculates the metric in <c>O(D)</c> time using precomputed prefix sums.
    /// Correctly handles the edge cases where the segment contains only 0s or only 1s (metric is 0).
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
    /// Gets the number of parameters estimated for a Bernoulli model segment.
    /// This is 1 parameter (the success probability 'p') per dimension.
    /// </summary>
    /// <param name="segmentLength">The length of the segment (unused in this implementation as parameter count is constant per dimension).</param>
    /// <returns>Number of parameters: Number of dimensions * 1.</returns>
    public int GetSegmentParameterCount(int segmentLength)
    {
         UninitializedDataException.ThrowIfUninitialized(_prefixSumSuccesses, "Fit() must be called before GetSegmentParameterCount().");
        _logger.LogTrace("Parameter count for Bernoulli is 1 per dimension, total {ParameterCount} for {NumDimensions} dimensions.", _numDimensions, _numDimensions);
        // 1 parameter (p) per dimension
        return _numDimensions;
    }

    /// <summary>
    /// Indicates that this cost function provides likelihood metrics suitable for BIC/AIC.
    /// </summary>
    public bool SupportsInformationCriteria => true;

    private double ComputeLikelihoodMetricInternal(int? start, int? end, string callerName)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSumSuccesses, $"Fit() must be called before {callerName}().");

        if (_numDimensions == 0 || _numPoints == 0) return 0;

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, endIndex, nameof(start));

        var segmentLength = endIndex - startIndex;
        SegmentLengthException.ThrowIfInvalid(segmentLength, 1);

        _logger.LogTrace("Calculating Bernoulli likelihood metric for segment [{StartIndex}, {EndIndex}) (Length: {SegmentLength}).", startIndex, endIndex, segmentLength);

        double totalMetric = 0;
        var epsilon = NumericUtils.GetDefaultEpsilon<double>();

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            // S = number of successes (sum of 1s)
            var segmentSumSuccesses = _prefixSumSuccesses[dim, endIndex] - _prefixSumSuccesses[dim, startIndex];
            var numFailures = segmentLength - segmentSumSuccesses; // n - S

            double metricDim;

            // edge cases: all successes or all failures
            // if S=0 or S=n, the MLE p_hat is 0 or 1 respectively, and the log-likelihood term is 0 (-2*logL = 0)
            // using tolerance to check for effective 0 or n
            if (NumericUtils.IsEffectivelyZero(segmentSumSuccesses, epsilon) || NumericUtils.AreApproximatelyEqual(segmentSumSuccesses, segmentLength, epsilon))
            {
                metricDim = 0.0;
                _logger.LogTrace("Segment [{StartIndex}, {EndIndex}), Dim {Dimension} is all zeros or all ones. Metric = 0.", startIndex, endIndex, dim);
            }
            else
            {
                // general case: 0 < S < n
                var logN = Math.Log(segmentLength);
                var logS = Math.Log(segmentSumSuccesses); // S > epsilon, log is safe
                var logF = Math.Log(numFailures);       // n-S > epsilon, log is safe

                // Metric = -2 * [ S*log(S) + (n-S)*log(n-S) - n*log(n) ]
                metricDim = MinusTwo * (segmentSumSuccesses * logS + numFailures * logF - segmentLength * logN);
                _logger.LogTrace("Segment [{StartIndex}, {EndIndex}), Dim {Dimension}: S={S}, n-S={F}, n={N}. Metric = {MetricValue}", startIndex, endIndex, dim, segmentSumSuccesses, numFailures, segmentLength, metricDim);
            }

            if (double.IsNaN(metricDim) || double.IsInfinity(metricDim))
            {
                 _logger.LogWarning("Metric calculation resulted in NaN or Infinity for dimension {Dimension} in segment [{StartIndex}, {EndIndex}). Returning PositiveInfinity.", dim, startIndex, endIndex);
                 return double.PositiveInfinity;
            }

            totalMetric += metricDim;
        }

        _logger.LogTrace("Total Bernoulli likelihood metric for segment [{StartIndex}, {EndIndex}): {TotalMetric}", startIndex, endIndex, totalMetric);
        return totalMetric;
    }
}