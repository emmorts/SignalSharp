using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;

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
/// The cost for the segment is derived from -2 * log-likelihood evaluated at the MLE. Specifically, it's calculated as:
/// <c>Cost(start, end) = -2 * [ S * log(S) + (n-S) * log(n-S) - n * log(n) ]</c>
/// where <c>S</c> is the number of successes (sum of 1s), <c>n</c> is the segment length, and <c>n-S</c> is the number of failures (sum of 0s).
/// This formula uses the convention <c>0 * log(0) = 0</c>, which is handled explicitly in the implementation for the edge cases where S=0 (all failures) or S=n (all successes). In these edge cases, the cost is 0, representing a perfect fit.
/// </para>
/// <para>
/// This cost is calculated efficiently using precomputed prefix sums of the signal (sum of 1s),
/// allowing <c>O(1)</c> cost calculation per segment after an <c>O(N*D)</c> precomputation step during <see cref="Fit(double[,])"/>.
/// </para>
/// <para>
/// Consider using the Bernoulli Likelihood cost function when:
/// <list type="bullet">
///     <item><description>Your data consists of binary outcomes (0s and 1s) over time (e.g., machine status up/down, test pass/fail, presence/absence).</description></item>
///     <item><description>You expect changes in the underlying probability of the '1' outcome.</description></item>
///     <item><description>The data within segments can be reasonably approximated by a sequence of independent Bernoulli trials with a constant success probability.</description></item>
///     <item><description>The input data must contain only values of 0 or 1.</description></item>
/// </list>
/// </para>
/// <para>
/// Note: This implementation requires input data to be strictly 0 or 1 (or numerically very close, within a defined tolerance). Other values will cause an exception during <see cref="Fit(double[,])"/>.
/// </para>
/// </remarks>
public class BernoulliLikelihoodCostFunction : CostFunctionBase
{
    private int _numDimensions;
    private int _numPoints;
    private double[,] _prefixSumSuccesses = null!;

    private const double MinusTwo = -2.0;
    private const double Epsilon = 1e-9;

    /// <summary>
    /// Fits the cost function to the provided binary (0/1) data by precomputing prefix sums of successes.
    /// </summary>
    /// <param name="signalMatrix">The binary data array to fit (rows=dimensions, columns=time points). Values must be effectively 0 or 1 (within tolerance).</param>
    /// <returns>The fitted <see cref="BernoulliLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This method performs <c>O(N*D)</c> computation to calculate prefix sums, enabling <c>O(1)</c> cost calculation per segment later via <see cref="ComputeCost"/>.
    /// It must be called before <see cref="ComputeCost"/>.
    /// It validates that all input data points are close to either 0 or 1 using a small tolerance (<c>Epsilon</c>).
    /// Values are effectively clamped to 0 or 1 for the internal sum calculation.
    /// </para>
    /// <example>
    /// <code>
    /// // Example: Machine status (1=up, 0=down)
    /// double[,] status = { { 1.0, 1.0, 1.0, 0.9999999999, 0.0, 0.0000000001, 0.0, 1.0, 1.0, 1.0 } };
    /// var binomialCost = new BernoulliLikelihoodCostFunction().Fit(status);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="signalMatrix"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any data point in <paramref name="signalMatrix"/> is not close to 0 or 1 within the defined <c>Epsilon</c> tolerance.</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));

        _numDimensions = signalMatrix.GetLength(0);
        _numPoints = signalMatrix.GetLength(1);

        _prefixSumSuccesses = new double[_numDimensions, _numPoints + 1];

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            for (var i = 0; i < _numPoints; i++)
            {
                var value = signalMatrix[dim, i];
                
                // check if the value is close to 0 or close to 1 within the tolerance
                var isNearZero = Math.Abs(value - 0.0) < Epsilon;
                var isNearOne = Math.Abs(value - 1.0) < Epsilon;

                if (!isNearZero && !isNearOne)
                {
                    throw new ArgumentException($"Input data must be effectively 0 or 1 (within epsilon={Epsilon}) for Bernoulli Likelihood cost. Found value at [{dim}, {i}]: {value}", nameof(signalMatrix));
                }

                // clamp the value to exactly 0.0 or 1.0 for the sum calculation
                // this ensures the counts (S and n-S) used in ComputeCost are effectively integers,
                // aligning with the discrete nature of the Bernouli model
                var valueToAdd = isNearOne ? 1.0 : 0.0;

                _prefixSumSuccesses[dim, i + 1] = _prefixSumSuccesses[dim, i] + valueToAdd;
            }
        }

        return this;
    }

    /// <summary>
    /// Computes the cost for a segment [start, end) based on the Binomial (Bernoulli) negative log-likelihood.
    /// Cost = <c>-2 * [ S*log(S) + (n-S)*log(n-S) - n*log(n) ]</c> where <c>S</c> = successes, <c>n</c> = length.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment (0 if all successes or all failures, positive otherwise).</returns>
    /// <remarks>
    /// <para>
    /// Calculates the cost in <c>O(D)</c> time using precomputed prefix sums, where D is the number of dimensions.
    /// Correctly handles the edge cases where the segment contains only 0s or only 1s (cost is 0).
    /// Must be called after <see cref="Fit(double[,])"/>.
    /// </para>
    /// <example>
    /// <code>
    /// // Assuming 'status' data from Fit example
    /// var bernouliCost = new BernoulliLikelihoodCostFunction().Fit(status);
    /// double costSegment1 = bernouliCost.ComputeCost(0, 4); // Cost for segment of ~all 1s (should be close to 0)
    /// double costSegment2 = bernouliCost.ComputeCost(4, 7); // Cost for segment of ~all 0s (should be close to 0)
    /// double costSegmentMix = bernouliCost.ComputeCost(0, 7); // Cost for mixed segment (positive)
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (<see cref="Fit(double[,])"/> not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices (<paramref name="start"/>, <paramref name="end"/>) are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length (<c>end - start</c>) is less than 1.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSumSuccesses, "Fit() must be called before ComputeCost().");

        if (_numDimensions == 0 || _numPoints == 0) return 0;

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;
        var segmentLength = endIndex - startIndex;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        SegmentLengthException.ThrowIfInvalid(segmentLength);

        double totalCost = 0;

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            // S = number of successes (sum of 1s)
            var segmentSumSuccesses = _prefixSumSuccesses[dim, endIndex] - _prefixSumSuccesses[dim, startIndex];
            var numFailures = segmentLength - segmentSumSuccesses; // n - S

            double costDim;

            // edge cases: all successes or all failures
            // if S=0 or S=n, the MLE p_hat is 0 or 1 respectively, and the log-likelihood is 0 (-2*logL = 0)
            // this also avoids log(0)
            if (segmentSumSuccesses <= Epsilon || segmentSumSuccesses >= segmentLength - Epsilon)
            {
                costDim = 0.0;
            }
            else
            {
                // general case: 0 < S < n
                var logN = Math.Log(segmentLength);
                var logS = Math.Log(segmentSumSuccesses);
                var logF = Math.Log(numFailures);

                // Cost = -2 * [ S*log(S) + (n-S)*log(n-S) - n*log(n) ]
                costDim = MinusTwo * (segmentSumSuccesses * logS + numFailures * logF - segmentLength * logN);
            }

            totalCost += costDim;
        }

        if (double.IsNaN(totalCost) || double.IsInfinity(totalCost))
        {
             return double.MaxValue;
        }

        return totalCost;
    }
}