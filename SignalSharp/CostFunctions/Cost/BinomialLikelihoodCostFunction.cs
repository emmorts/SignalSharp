using Microsoft.Extensions.Logging;
using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Logging;
using SignalSharp.Utilities;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on the Binomial negative log-likelihood for the PELT algorithm.
/// This cost function is suitable for detecting changes in the success probability of binomial data (k successes out of n trials).
/// </summary>
/// <remarks>
/// <para>
/// This cost function assumes that the data within each segment follows a Binomial distribution,
/// characterized by a number of trials (<c>n</c>) and a number of successes (<c>k</c>) for each data point,
/// with a constant success probability (<c>p</c>) within the segment.
/// </para>
/// <para>
/// The input data is expected as a 2D array <c>signalMatrix</c> where:
/// <list type="bullet">
///     <item><description><c>signalMatrix[0, i]</c> represents the number of successes (<c>k_i</c>) at time point <c>i</c>.</description></item>
///     <item><description><c>signalMatrix[1, i]</c> represents the number of trials (<c>n_i</c>) at time point <c>i</c>.</description></item>
/// </list>
/// It is required that <c>0 &lt;= k_i &lt;= n_i</c>, <c>n_i >= 1</c>, and both <c>k_i</c> and <c>n_i</c> are effectively non-negative integers (within tolerance) for all <c>i</c>.
/// </para>
/// <para>
/// The cost for a segment <c>[start, end)</c> is derived from the maximized negative log-likelihood.
/// Let <c>K = Sum(k_i)</c> and <c>N = Sum(n_i)</c> be the total successes and trials in the segment, respectively.
/// The Maximum Likelihood Estimate (MLE) for the success probability <c>p</c> is <c>p_hat = K / N</c>.
/// </para>
/// <para>
/// The likelihood metric used for BIC/AIC calculations, proportional to the negative log-likelihood evaluated at <c>p_hat</c>, is:
/// <c>Metric(start, end) = -[ K * log(K) + (N - K) * log(N - K) - N * log(N) ]</c>
/// (derived from <c>-[ K * log(p_hat) + (N - K) * log(1 - p_hat) ]</c> and simplifying, dropping combinatorial terms).
/// The convention <c>0 * log(0) = 0</c> is used via the internal <c>XLogX</c> helper. This metric is sensitive to changes in the underlying success rate <c>p</c>.
/// A metric of 0 indicates a perfect fit (<c>p_hat = 0</c> or <c>p_hat = 1</c>).
/// The <see cref="ComputeCost"/> method returns this same metric value.
/// </para>
/// <para>
/// This cost/metric is calculated efficiently using precomputed prefix sums of successes (<c>k</c>) and trials (<c>n</c>),
/// allowing <c>O(1)</c> calculation per segment after an <c>O(M)</c> precomputation step during <see cref="Fit(double[,])"/>, where M is the number of data points.
/// </para>
/// <para>
/// Consider using the Binomial Likelihood cost function when:
/// <list type="bullet">
///     <item><description>Your data represents counts of successes out of a known number of trials at each time point.</description></item>
///     <item><description>You want to detect time points where the underlying success probability changes.</description></item>
///     <item><description>The assumption of a constant success probability within each segment is reasonable.</description></item>
/// </list>
/// </para>
/// </remarks>
public class BinomialLikelihoodCostFunction : CostFunctionBase, ILikelihoodCostFunction
{
    private int _numPoints;
    private double[] _prefixSumK = null!;
    private double[] _prefixSumN = null!;

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinomialLikelihoodCostFunction"/> class.
    /// </summary>
    public BinomialLikelihoodCostFunction()
    {
        _logger = LoggerProvider.CreateLogger<BinomialLikelihoodCostFunction>();
    }

    /// <summary>
    /// Fits the cost function to the provided binomial data by precomputing prefix sums.
    /// </summary>
    /// <param name="signalMatrix">
    /// The data array to fit (rows=dimensions, columns=time points).
    /// Must have exactly 2 rows: row 0 for successes (k), row 1 for trials (n).
    /// Values for k and n must be non-negative integers (within tolerance), with n >= 1 and k &lt;= n.
    /// </param>
    /// <returns>The fitted <see cref="BinomialLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This method performs <c>O(M)</c> computation (where M is the number of time points) to calculate prefix sums,
    /// enabling <c>O(1)</c> cost/metric calculation per segment later. It must be called before cost computation.
    /// Input data must satisfy <c>0 &lt;= k_i &lt;= n_i</c>, <c>n_i >= 1</c>, and be effectively integers (within tolerance).
    /// </para>
    /// <example>
    /// <code>
    /// // Data: k = [1, 2, 8, 9], n = [10, 10, 10, 10]
    /// double[,] data = {
    ///     { 1.0, 2.0, 8.0, 9.0 }, // Successes (k)
    ///     { 10.0, 10.0, 10.0, 10.0 } // Trials (n)
    /// };
    /// var binomialCost = new BinomialLikelihoodCostFunction();
    /// binomialCost.Fit(data);
    ///
    /// // Data: k = [5, 8], n = [20, 15] (varying n)
    /// double[,] dataVaryingN = {
    ///     { 5.0, 8.0 }, // Successes (k)
    ///     { 20.0, 15.0 } // Trials (n)
    /// };
    /// binomialCost.Fit(dataVaryingN);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="signalMatrix"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="signalMatrix"/> does not have exactly 2 rows,
    /// or if data is invalid (k &lt; 0, n &lt; 1, k > n, not effectively integers, NaN, or Infinity).</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        _logger.LogTrace("Fitting BinomialLikelihoodCostFunction to signal of length {Length}.", signalMatrix.GetLength(1));

        if (signalMatrix.GetLength(0) != 2)
        {
            var message = "Input signalMatrix must have exactly 2 rows (row 0: successes k, row 1: trials n).";
            _logger.LogError(message);
            throw new ArgumentException(message, nameof(signalMatrix));
        }

        _numPoints = signalMatrix.GetLength(1);

        _prefixSumK = new double[_numPoints + 1];
        _prefixSumN = new double[_numPoints + 1];

        var tolerance = NumericUtils.GetDefaultEpsilon<double>(); // Use standard double epsilon

        for (var i = 0; i < _numPoints; i++)
        {
            var k = signalMatrix[0, i];
            var n = signalMatrix[1, i];

            if (double.IsNaN(k) || double.IsNaN(n) || double.IsInfinity(k) || double.IsInfinity(n))
            {
                 var message = $"Invalid data at index {i}: k={k}, n={n}. Values cannot be NaN or Infinity.";
                 _logger.LogError(message);
                 throw new ArgumentException(message, nameof(signalMatrix));
            }

            // Check if k and n are effectively integers
            var kIsInteger = NumericUtils.IsEffectivelyInteger(k, tolerance);
            var nIsInteger = NumericUtils.IsEffectivelyInteger(n, tolerance);
            var intK = Math.Round(k); // Use rounded value for checks and sums
            var intN = Math.Round(n);

            if (!kIsInteger || !nIsInteger || intK < 0 || intN < 1 || intK > intN)
            {
                var message = $"Invalid data at index {i}: k={k}, n={n}. Requirements: k and n must be non-negative integers (within tolerance {tolerance}), 0 <= k <= n, n >= 1.";
                _logger.LogError(message);
                throw new ArgumentException(message, nameof(signalMatrix));
            }

            _prefixSumK[i + 1] = _prefixSumK[i] + intK;
            _prefixSumN[i + 1] = _prefixSumN[i] + intN;
        }
        _logger.LogDebug("Prefix sums computed successfully.");
        return this;
    }

    /// <summary>
    /// Computes the cost for a segment <c>[start, end)</c> based on the Binomial negative log-likelihood.
    /// Cost = <c>-[ K*log(K) + (N-K)*log(N-K) - N*log(N) ]</c> where K=total successes, N=total trials.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment (0 if p_hat=0 or p_hat=1, positive otherwise).</returns>
    /// <remarks>
    /// <para>
    /// Calculates the cost in <c>O(1)</c> time using precomputed prefix sums.
    /// Correctly handles the edge cases where the estimated success probability is 0 or 1 (cost is 0).
    /// Must be called after <see cref="Fit(double[,])"/>. This returns the same value as <see cref="ComputeLikelihoodMetric"/>.
    /// </para>
    /// <example>
    /// <code>
    /// // Assuming 'binomialCost' is an instance fitted with data
    /// double costSegment = binomialCost.ComputeCost(0, 10);
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
    /// Computes the likelihood metric for a segment <c>[start, end)</c> based on the Binomial negative log-likelihood.
    /// Metric = <c>-[ K*log(K) + (N-K)*log(N-K) - N*log(N) ]</c> where K=total successes, N=total trials.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive).</param>
    /// <param name="end">The end index of the segment (exclusive).</param>
    /// <returns>The computed likelihood metric for the segment (0 if p_hat=0 or p_hat=1, positive otherwise).</returns>
    /// <remarks>
    /// <para>
    /// Calculates the metric in <c>O(1)</c> time using precomputed prefix sums.
    /// Correctly handles the edge cases where the estimated success probability is 0 or 1 (metric is 0).
    /// Must be called after <see cref="Fit(double[,])"/>. This returns the same value as <see cref="ComputeCost"/>.
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
    /// Gets the number of parameters estimated for a Binomial model segment.
    /// This is 1 parameter (the success probability 'p').
    /// </summary>
    /// <param name="segmentLength">The length of the segment (unused).</param>
    /// <returns>Number of parameters: 1.</returns>
    public int GetSegmentParameterCount(int segmentLength)
    {
         UninitializedDataException.ThrowIfUninitialized(_prefixSumK, "Fit() must be called before GetSegmentParameterCount().");
        _logger.LogTrace("Parameter count for Binomial is 1.");
        // 1 parameter (p) estimated for the segment
        return 1;
    }

    /// <summary>
    /// Indicates that this cost function provides likelihood metrics suitable for BIC/AIC.
    /// </summary>
    public bool SupportsInformationCriteria => true;

    /// <summary>
    /// Fitting with a 1D signal is not supported for the general Binomial likelihood cost function,
    /// as it requires both successes (k) and trials (n) information per point.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data.</param>
    /// <returns>Throws NotSupportedException.</returns>
    /// <exception cref="NotSupportedException">Always thrown, explaining the need for 2D input with specific row definitions.</exception>
    public new IPELTCostFunction Fit(double[] signal)
    {
        var message = $"{nameof(BinomialLikelihoodCostFunction)} requires 2D input data (successes 'k' in row 0, trials 'n' in row 1). Use the Fit(double[,]) overload instead.";
        _logger.LogError(message);
        throw new NotSupportedException(message);
    }

    private double ComputeLikelihoodMetricInternal(int? start, int? end, string callerName)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSumK, $"Fit() must be called before {callerName}().");
        UninitializedDataException.ThrowIfUninitialized(_prefixSumN, $"Fit() must be called before {callerName}().");

        if (_numPoints == 0) return 0;

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, endIndex, nameof(start));

        var segmentLength = endIndex - startIndex;
        SegmentLengthException.ThrowIfInvalid(segmentLength, 1);

        _logger.LogTrace("Calculating Binomial likelihood metric for segment [{StartIndex}, {EndIndex}) (Length: {SegmentLength}).", startIndex, endIndex, segmentLength);

        var K = _prefixSumK[endIndex] - _prefixSumK[startIndex]; // Total successes
        var N = _prefixSumN[endIndex] - _prefixSumN[startIndex]; // Total trials

        var tolerance = NumericUtils.GetDefaultEpsilon<double>();

        // edge case: if total trials N is effectively 0
        if (NumericUtils.IsEffectivelyZero(N, tolerance))
        {
            // if N is effectively 0, K must also be 0 (due to fit validation), so treat as zero cost/metric
             _logger.LogTrace("Segment [{StartIndex}, {EndIndex}) has N ~= 0. Metric = 0.", startIndex, endIndex);
            return 0.0;
        }

        // edge cases for MLE probability p_hat = K/N being effectively 0 or 1
        if (NumericUtils.IsEffectivelyZero(K, tolerance)) // p_hat approx 0 (K=0)
        {
            // metric formula simplifies to -[ 0 + N*log(N) - N*log(N) ] = 0
             _logger.LogTrace("Segment [{StartIndex}, {EndIndex}) has K ~= 0 (N={NValue}). Metric = 0.", startIndex, endIndex, N);
            return 0.0;
        }
        if (NumericUtils.AreApproximatelyEqual(K, N, tolerance)) // p_hat approx 1 (K=N)
        {
            // metric formula simplifies to -[ N*log(N) + 0 - N*log(N) ] = 0
            _logger.LogTrace("Segment [{StartIndex}, {EndIndex}) has K ~= N (K={KValue}, N={NValue}). Metric = 0.", startIndex, endIndex, K, N);
             return 0.0;
        }

        // general case: 0 < K < N
        // Metric = - [ K*log(K) + (N-K)*log(N-K) - N*log(N) ]
        var metric = -(XLogX(K, tolerance) + XLogX(N - K, tolerance) - XLogX(N, tolerance));
        _logger.LogTrace("Segment [{StartIndex}, {EndIndex}) has K={KValue}, N={NValue}. Metric = {MetricValue}", startIndex, endIndex, K, N, metric);

        if (double.IsNaN(metric) || double.IsInfinity(metric))
        {
             _logger.LogWarning("Metric calculation resulted in NaN or Infinity for segment [{StartIndex}, {EndIndex}). Returning PositiveInfinity.", startIndex, endIndex);
             return double.PositiveInfinity;
        }

        // Ensure metric is non-negative (can sometimes be slightly negative due to float precision)
        return Math.Max(0.0, metric);
    }

    /// <summary>
    /// Helper function to calculate <c>x * log(x)</c>, handling the case <c>x</c> is near zero.
    /// Conventionally, <c>0 * log(0)</c> is treated as 0 in entropy/likelihood calculations.
    /// </summary>
    /// <param name="x">Input value (must be non-negative).</param>
    /// <param name="tolerance">Tolerance to check if x is effectively zero.</param>
    /// <returns><c>x * log(x)</c> if x is greater than tolerance, otherwise 0.</returns>
    private static double XLogX(double x, double tolerance)
    {
        return x <= tolerance ? 0.0 : x * Math.Log(x);
    }
}