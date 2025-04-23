using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Represents a cost function using the L2 norm (Euclidean distance) for the Piecewise Linear Trend Change (PELT) method.
/// </summary>
/// <remarks>
/// <para>
/// The L2 norm, also known as the Euclidean distance, is a measure of the straight-line distance
/// between points in a multi-dimensional space. It is calculated as the square root of the sum of the
/// squared differences between the coordinates of the points.
/// </para>
///
/// <para>
/// In the context of the Piecewise Linear Trend Change (PELT) method, the L2 cost function is used to
/// compute the cost of segmenting a time series or sequential data into different segments where the
/// statistical properties change. The L2 norm is sensitive to outliers, making it a good choice when the
/// data is relatively clean and normally distributed.
/// </para>
///
/// <para>
/// Consider using the L2 cost function in scenarios where:
/// <list type="bullet">
///     <item>The data is relatively clean and free of outliers.</item>
///     <item>You need a precise measure of segment dissimilarity.</item>
///     <item>The underlying data distribution is approximately normal.</item>
/// </list>
/// </para>
/// </remarks>
public class L2CostFunction : CostFunctionBase
{
    private int _numDimensions;
    private int _numPoints;
    private double[,] _prefixSum = null!;
    private double[,] _prefixSumSq = null!;

    /// <summary>
    /// Fits the cost function to the provided data.
    /// </summary>
    /// <param name="signalMatrix">The data array to fit.</param>
    /// <returns>The fitted <see cref="L2CostFunction"/> instance.</returns>
    /// <remarks>
    /// This method performs O(N*D) computation to calculate prefix sums of the signal and its squares,
    /// enabling O(1) cost calculation per segment later via <see cref="ComputeCost"/>.
    ///
    /// <example>
    /// <code>
    /// double[,] data = { { 1.0, 2.0, 3.0, 4.0 } };
    /// var l2Cost = new L2CostFunction().Fit(data);
    /// </code>
    /// This initializes the cost function with the provided data, making it ready for segment cost computation.
    /// </example>
    /// </remarks>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));

        _numDimensions = signalMatrix.GetLength(0);
        _numPoints = signalMatrix.GetLength(1);

        // initialize prefix sum arrays with size N+1 to handle segments starting at index 0
        // _prefixSum[d, 0] and _prefixSumSq[d, 0] will remain 0.
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
    /// Computes the cost for a segment of the data using the L2 norm (sum of squared errors).
    /// Cost(start, end) = Sum_{i=start}^{end-1} (signal[i] - mean(segment))^2
    /// This is calculated efficiently in O(1) time using precomputed prefix sums as:
    /// Sum(signal[i]^2) - (Sum(signal[i])^2 / segmentLength) for the segment [start, end).
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive). If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment (exclusive). If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>This method must be called after the <see cref="Fit(double[,])"/> method has been used to
    /// initialize the prefix sums.</para>
    /// <para>The calculation relies on the identity: Sum((x_i - mu)^2) = Sum(x_i^2) - (Sum(x_i)^2 / n).</para>
    /// <example>
    /// For example, given a fitted L2CostFunction instance:
    /// <code>
    /// var l2Cost = new L2CostFunction().Fit(data);
    /// double cost = l2Cost.ComputeCost(0, 10); // Cost for segment from index 0 up to (but not including) 10
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="UninitializedDataException">Thrown when prefix sums are not initialized (Fit not called).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        if (_numDimensions == 0 || _numPoints == 0)
            return 0;

        UninitializedDataException.ThrowIfUninitialized(_prefixSum, "Fit() must be called before ComputeCost().");
        UninitializedDataException.ThrowIfUninitialized(_prefixSumSq, "Fit() must be called before ComputeCost().");

        var startIndex = start ?? 0;
        var endIndex = end ?? _numPoints;

        var segmentLength = endIndex - startIndex;

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _numPoints, nameof(end));
        SegmentLengthException.ThrowIfInvalid(endIndex - startIndex);

        double totalCost = 0;

        for (var dim = 0; dim < _numDimensions; dim++)
        {
            // Sum_{i=start}^{end-1} x[i] = prefix_sum[end] - prefix_sum[start]
            var segmentSum = _prefixSum[dim, endIndex] - _prefixSum[dim, startIndex];

            // Sum_{i=start}^{end-1} x[i]^2 = prefix_sum_sq[end] - prefix_sum_sq[start]
            var segmentSumSq = _prefixSumSq[dim, endIndex] - _prefixSumSq[dim, startIndex];

            // calculate cost for this dimension: Sum(x^2) - (Sum(x))^2 / n
            var costDim = segmentSumSq - (segmentSum * segmentSum) / segmentLength;

            totalCost += costDim;
        }

        return totalCost;
    }
}
