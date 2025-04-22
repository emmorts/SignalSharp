using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function based on the Gaussian negative log-likelihood for the PELT algorithm.
/// This cost function is sensitive to changes in both the mean and the variance of the signal.
/// </summary>
/// <remarks>
/// <para>
/// This cost function assumes that the data within each segment follows a Gaussian (normal) distribution.
/// It calculates the cost based on the negative log-likelihood of the segment data given its estimated
/// mean and variance (Maximum Likelihood Estimates).
/// </para>
/// <para>
/// The cost for a segment [start, end) of length `n = end - start` is primarily driven by `n * log(σ²_hat)`,
/// where `σ²_hat` is the MLE of the variance for that segment. This makes the function sensitive to shifts
/// in both central tendency (mean) and dispersion (variance).
/// </para>
/// <para>
/// This cost is calculated efficiently using precomputed prefix sums of the signal and its squares,
/// allowing O(1) cost calculation per segment after an O(N*D) precomputation step during `Fit`.
/// </para>
/// <para>
/// Consider using the Gaussian Likelihood cost function when:
/// <list type="bullet">
///     <item>You expect changes in both the mean and variance of your signal.</item>
///     <item>The data within segments can be reasonably approximated by a normal distribution.</item>
///     <item>You need a statistically principled way to evaluate segment homogeneity under Gaussian assumptions.</item>
/// </list>
/// </para>
/// <para>
/// Note: This implementation uses the MLE for variance (dividing by `n`). If the segment variance is zero
/// or numerically very close to zero (e.g., all points in the segment are identical), the logarithm
/// would be undefined or negative infinity. To handle this, a small minimum variance (`epsilon`) is assumed
/// to ensure numerical stability, effectively assigning a very high cost to zero-variance segments.
/// </para>
/// </remarks>
public class GaussianLikelihoodCostFunction : CostFunctionBase
{
    private int _numDimensions;
    private int _numPoints;
    private double[,] _prefixSum = null!;
    private double[,] _prefixSumSq = null!;
    private const double Epsilon = 1e-10; // small value to prevent log(0)

    /// <summary>
    /// Fits the cost function to the provided data by precomputing prefix sums.
    /// </summary>
    /// <param name="signalMatrix">The data array to fit (rows=dimensions, columns=time points).</param>
    /// <returns>The fitted <see cref="GaussianLikelihoodCostFunction"/> instance.</returns>
    /// <remarks>
    /// This method performs O(N*D) computation to calculate prefix sums, enabling O(1) cost calculation per segment later.
    /// It must be called before <see cref="ComputeCost"/>.
    /// <example>
    /// <code>
    /// double[,] data = { { 1.0, 1.1, 1.0, 5.0, 5.1, 4.9 } };
    /// var gaussianCost = new GaussianLikelihoodCostFunction().Fit(data);
    /// </code>
    /// </example>
    /// </remarks>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
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

        return this;
    }

    /// <summary>
    /// Computes the cost for a segment [start, end) based on the Gaussian negative log-likelihood.
    /// The cost is approximately `Sum_dimensions [ n * log(variance_mle_dim) ]`.
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// Calculates the cost in O(D) time using precomputed prefix sums, where D is the number of dimensions.
    /// Handles potential zero variance by adding a small epsilon to prevent `log(0)`.
    /// Must be called after <see cref="Fit(double[,])"/>.
    /// <example>
    /// <code>
    /// var gaussianCost = new GaussianLikelihoodCostFunction().Fit(data);
    /// double cost = gaussianCost.ComputeCost(0, 10); // Cost for segment from index 0 up to (but not including) 10
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (Fit not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        UninitializedDataException.ThrowIfUninitialized(_prefixSum, "Fit() must be called before ComputeCost().");
        UninitializedDataException.ThrowIfUninitialized(_prefixSumSq, "Fit() must be called before ComputeCost().");
        
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
            var segmentSum = _prefixSum[dim, endIndex] - _prefixSum[dim, startIndex];
            var segmentSumSq = _prefixSumSq[dim, endIndex] - _prefixSumSq[dim, startIndex];

            var sumSqDev = segmentSumSq - (segmentSum * segmentSum) / segmentLength;

            // calculate MLE variance: SumSqDev / n
            // clamp sumSqDev to a small positive number to avoid log(0) or log(<0) due to floating point errors
            // when all values in the segment are identical.
            var varianceMLE = Math.Max(sumSqDev, Epsilon) / segmentLength;

            // cost for this dimension is n * log(variance_mle)
            // note: additive constants like n*log(2pi) + n from the full -2*logL are often omitted
            // as they depend only on n and are implicitly handled by PELT's penalty or comparison logic
            var costDim = segmentLength * Math.Log(varianceMLE);

            totalCost += costDim;
        }

        if (double.IsNaN(totalCost) || double.IsInfinity(totalCost))
        {
            // return a very large cost instead of NaN/Infinity (this can happen in edge cases with the log function)
             return double.MaxValue; 
        }

        return totalCost;
    }
}