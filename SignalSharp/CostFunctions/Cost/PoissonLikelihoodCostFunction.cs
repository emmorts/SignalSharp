using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on the Poisson negative log-likelihood for the PELT algorithm.
/// This cost function is sensitive to changes in the rate (mean) of events in count data.
/// </summary>
/// <remarks>
/// <para>
/// This cost function assumes that the data within each segment represents counts following a Poisson distribution
/// with a constant rate parameter (<c>λ</c>) for that segment. It calculates the cost based on the negative log-likelihood
/// of the segment data given its estimated rate (Maximum Likelihood Estimate - MLE).
/// </para>
/// <para>
/// The MLE for the rate <c>λ</c> in a segment [start, end) of length <c>n = end - start</c> is the sample mean:
/// <c>λ_hat = (Sum_{i=start}^{end-1} signal[i]) / n = S / n</c>, where <c>S</c> is the sum of counts.
/// </para>
/// <para>
/// The cost for the segment is derived from -2 * log-likelihood evaluated at the MLE. Specifically, it's calculated as:
/// <c>Cost(start, end) = 2 * [ S - S * log(S) + S * log(n) ]</c>
/// where <c>S = Sum_{i=start}^{end-1} signal[i]</c> is the sum of counts in the segment, and <c>n = end - start</c> is the segment length.
/// This formula assumes the convention <c>0 * log(0) = 0</c>, which is handled by setting the cost to 0 when <c>S=0</c>.
/// The term <c>Sum log(signal[i]!)</c> from the full likelihood is often omitted as it depends only on the data points themselves
/// and not the segmentation structure, effectively being handled by the PELT penalty or relative cost comparisons.
/// </para>
/// <para>
/// This cost is calculated efficiently using precomputed prefix sums of the signal,
/// allowing <c>O(1)</c> cost calculation per segment after an <c>O(N*D)</c> precomputation step during <see cref="Fit(double[,])"/>.
/// </para>
/// <para>
/// Consider using the Poisson Likelihood cost function when:
/// <list type="bullet">
///     <item><description>Your data represents counts of events per interval (e.g., website hits per day, defects per batch, calls per hour).</description></item>
///     <item><description>You expect changes in the average rate of these events.</description></item>
///     <item><description>The data within segments can be reasonably approximated by a Poisson distribution (variance roughly equals mean).</description></item>
///     <item><description>The input data contains non-negative values (counts cannot be negative).</description></item>
/// </list>
/// </para>
/// <para>
/// Note: While the function accepts <c>double</c> inputs, Poisson counts are theoretically non-negative integers. This implementation requires input data to be effectively non-negative (values <c>>= -Epsilon</c>). Slightly negative values close to zero might be tolerated and clamped to zero, but significantly negative values will cause an exception during <see cref="Fit(double[,])"/>.
/// </para>
/// </remarks>
public class PoissonLikelihoodCostFunction : CostFunctionBase
{
    private int _numDimensions;
    private int _numPoints;
    private double[,] _prefixSum = null!;

    private const double Two = 2.0;
    private const double Epsilon = 1e-9;

    /// <summary>
    /// Fits the cost function to the provided count data by precomputing prefix sums.
    /// </summary>
    /// <param name="signalMatrix">The count data array to fit (rows=dimensions, columns=time points). Values must be effectively non-negative (>= -Epsilon).</param>
    /// <returns>The fitted <see cref="PoissonLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This method performs <c>O(N*D)</c> computation to calculate prefix sums, enabling <c>O(1)</c> cost calculation per segment later via <see cref="ComputeCost"/>.
    /// It must be called before <see cref="ComputeCost"/>.
    /// It validates that all input data points are non-negative within a small tolerance (<c>Epsilon</c>). Values slightly below zero but within tolerance will be clamped to zero for the sum.
    /// </para>
    /// <example>
    /// <code>
    /// // Example: Number of website hits per hour
    /// double[,] counts = { { 5, 8, 6, 7, 25, 30, 28, 10, 9, 12 } };
    /// var poissonCost = new PoissonLikelihoodCostFunction().Fit(counts);
    ///
    /// // Example with near-zero value
    /// double[,] countsNearZero = { { 5, 8, 1e-10, 7, 25, 30, -1e-11, 10, 9, 12 } };
    /// poissonCost = new PoissonLikelihoodCostFunction().Fit(countsNearZero); // Should work
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="signalMatrix"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any data point in <paramref name="signalMatrix"/> is less than -<c>Epsilon</c>.</exception>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));

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
                    throw new ArgumentException($"Input data must be non-negative (>= -{Epsilon}) for Poisson Likelihood cost. Found negative value at [{dim}, {i}]: {value}", nameof(signalMatrix));
                }

                var valueToAdd = Math.Max(0.0, value);

                _prefixSum[dim, i + 1] = _prefixSum[dim, i] + valueToAdd;
            }
        }

        return this;
    }

    /// <summary>
    /// Computes the cost for a segment [start, end) based on the Poisson negative log-likelihood.
    /// The cost is <c>2 * [ S - S * log(S) + S * log(n) ]</c>, where S is the sum of counts and n is the length.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>
    /// Calculates the cost in <c>O(D)</c> time using precomputed prefix sums, where D is the number of dimensions.
    /// Handles the <c>segmentSum = 0</c> case correctly based on the limit <c>x*log(x) -> 0</c> as <c>x -> 0</c>, resulting in zero cost for segments with zero total count.
    /// Must be called after <see cref="Fit(double[,])"/>.
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
        UninitializedDataException.ThrowIfUninitialized(_prefixSum, "Fit() must be called before ComputeCost().");

        if (_numDimensions == 0 || _numPoints == 0) return 0;

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;
        var segmentLength = endIndex - startIndex;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        SegmentLengthException.ThrowIfInvalid(segmentLength);

        double totalCost = 0;
        var logSegmentLength = Math.Log(segmentLength);

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            var segmentSum = _prefixSum[dim, endIndex] - _prefixSum[dim, startIndex];
            double costDim;

            // edge case: if sum is 0 (or very close due to clamping), the MLE rate is 0
            if (segmentSum <= Epsilon)
            {
                costDim = 0.0;
            }
            else
            {
                // Cost = 2 * [ S - S * log(S) + S * log(n) ]
                var logSegmentSum = Math.Log(segmentSum); // S > Epsilon, so log is safe
                costDim = Two * (segmentSum - segmentSum * logSegmentSum + segmentSum * logSegmentLength);
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