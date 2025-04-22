using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;

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
/// It is required that <c>0 &lt;= k_i &lt;= n_i</c>, <c>n_i >= 1</c>, and both <c>k_i</c> and <c>n_i</c> are non-negative integers for all <c>i</c>.
/// </para>
/// <para>
/// The cost for a segment <c>[start, end)</c> is derived from the maximized negative log-likelihood.
/// Let <c>K = Sum(k_i)</c> and <c>N = Sum(n_i)</c> be the total successes and trials in the segment, respectively.
/// The Maximum Likelihood Estimate (MLE) for the success probability <c>p</c> is <c>p_hat = K / N</c>.
/// </para>
/// <para>
/// The cost function used is proportional to the negative log-likelihood evaluated at <c>p_hat</c>, specifically:
/// <c>Cost(start, end) = -[ K * log(K) + (N - K) * log(N - K) - N * log(N) ]</c>
/// (derived from <c>-[ K * log(p_hat) + (N - K) * log(1 - p_hat) ]</c> and simplifying, dropping combinatorial terms).
/// The convention <c>0 * log(0) = 0</c> is used via the internal <c>XLogX</c> helper. This cost is sensitive to changes in the underlying success rate <c>p</c>.
/// A cost of 0 indicates a perfect fit (<c>p_hat = 0</c> or <c>p_hat = 1</c>).
/// </para>
/// <para>
/// This cost is calculated efficiently using precomputed prefix sums of successes (<c>k</c>) and trials (<c>n</c>),
/// allowing <c>O(1)</c> cost calculation per segment after an <c>O(M)</c> precomputation step during <see cref="Fit(double[,])"/>, where M is the number of data points.
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
public class BinomialLikelihoodCostFunction : CostFunctionBase
{
    private int _numPoints;
    private double[] _prefixSumK = null!;
    private double[] _prefixSumN = null!;
    
    private const double Tolerance = 1e-9;

    /// <summary>
    /// Fits the cost function to the provided binomial data by precomputing prefix sums.
    /// </summary>
    /// <param name="signalMatrix">
    /// The data array to fit (rows=dimensions, columns=time points).
    /// Must have exactly 2 rows: row 0 for successes (k), row 1 for trials (n).
    /// Values for k and n must be non-negative integers, with n >= 1 and k &lt;= n.
    /// </param>
    /// <returns>The fitted <see cref="BinomialLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This method performs <c>O(M)</c> computation (where M is the number of time points) to calculate prefix sums,
    /// enabling <c>O(1)</c> cost calculation per segment later. It must be called before <see cref="ComputeCost"/>.
    /// Input data must satisfy <c>0 &lt;= k_i &lt;= n_i</c>, <c>n_i >= 1</c>, and be effectively integers.
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
    /// or if data is invalid (k &lt; 0, n &lt; 1, k > n, or not effectively integers).</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));

        if (signalMatrix.GetLength(0) != 2)
        {
            throw new ArgumentException("Input signalMatrix must have exactly 2 rows (row 0: successes k, row 1: trials n).", nameof(signalMatrix));
        }

        _numPoints = signalMatrix.GetLength(1);

        _prefixSumK = new double[_numPoints + 1];
        _prefixSumN = new double[_numPoints + 1];

        for (var i = 0; i < _numPoints; i++)
        {
            var k = signalMatrix[0, i];
            var n = signalMatrix[1, i];

            var intK = Math.Round(k);
            var intN = Math.Round(n);

            var kIsInteger = Math.Abs(k - intK) < Tolerance;
            var nIsInteger = Math.Abs(n - intN) < Tolerance;

            if (double.IsNaN(k) || double.IsNaN(n) || double.IsInfinity(k) || double.IsInfinity(n))
            {
                 throw new ArgumentException(
                    $"Invalid data at index {i}: k={k}, n={n}. Values cannot be NaN or Infinity.",
                    nameof(signalMatrix));
            }

            if (!kIsInteger || !nIsInteger || intK < 0 || intN < 1 || intK > intN)
            {
                throw new ArgumentException(
                    $"Invalid data at index {i}: k={k}, n={n}. " +
                    $"Requirements: k and n must be non-negative integers (within tolerance {Tolerance}), 0 <= k <= n, n >= 1.",
                    nameof(signalMatrix));
            }

            _prefixSumK[i + 1] = _prefixSumK[i] + intK;
            _prefixSumN[i + 1] = _prefixSumN[i] + intN;
        }

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
    /// Must be called after <see cref="Fit(double[,])"/>.
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
        UninitializedDataException.ThrowIfUninitialized(_prefixSumK, "Fit() must be called before ComputeCost().");
        UninitializedDataException.ThrowIfUninitialized(_prefixSumN, "Fit() must be called before ComputeCost().");

        if (_numPoints == 0) return 0;

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;
        var segmentLength = endIndex - startIndex;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        SegmentLengthException.ThrowIfInvalid(segmentLength);

        var K = _prefixSumK[endIndex] - _prefixSumK[startIndex];
        var N = _prefixSumN[endIndex] - _prefixSumN[startIndex];

        // edge case: if total trials N is 0 or negative (shouldn't ever happen)
        if (N <= Tolerance)
        {
            // if N is effectively 0, K must also be 0, so treat as zero cost (consistent with p=0 or p=1 limits)
            return 0.0;
        }

        // edge cases for MLE probability p_hat = K/N being effectively 0 or 1
        if (K <= Tolerance) // p_hat approx 0 (K=0)
        {
            // cost formula simplifies to -[ 0 + N*log(N) - N*log(N) ] = 0
            return 0.0;
        }
        if (K >= N - Tolerance) // p_hat approx 1 (K=N)
        {
            // cost formula simplifies to -[ N*log(N) + 0 - N*log(N) ] = 0
             return 0.0;
        }

        // general case: 0 < K < N
        // Cost = - [ K*log(K) + (N-K)*log(N-K) - N*log(N) ]
        var cost = -(XLogX(K) + XLogX(N - K) - XLogX(N));

        if (double.IsNaN(cost) || double.IsInfinity(cost))
        {
             return double.MaxValue;
        }

        return Math.Max(0.0, cost);
    }

    /// <summary>
    /// Helper function to calculate <c>x * log(x)</c>, handling the case <c>x=0</c>.
    /// Conventionally, <c>0 * log(0)</c> is treated as 0 in entropy/likelihood calculations.
    /// </summary>
    /// <param name="x">Input value (must be non-negative).</param>
    /// <returns><c>x * log(x)</c> if x is effectively greater than 0, otherwise 0.</returns>
    private static double XLogX(double x)
    {
        return x <= Tolerance ? 0.0 : x * Math.Log(x);
    }

    /// <summary>
    /// Fitting with a 1D signal is not supported for the general Binomial likelihood cost function,
    /// as it requires both successes (k) and trials (n) information per point.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data.</param>
    /// <returns>Throws NotSupportedException.</returns>
    /// <exception cref="NotSupportedException">Always thrown, explaining the need for 2D input with specific row definitions.</exception>
    public new IPELTCostFunction Fit(double[] signal)
    {
        throw new NotSupportedException(
            $"{nameof(BinomialLikelihoodCostFunction)} requires 2D input data (successes 'k' in row 0, trials 'n' in row 1). " +
            $"Use the Fit(double[,]) overload instead.");
    }
}