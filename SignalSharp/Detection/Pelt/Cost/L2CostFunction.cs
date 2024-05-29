using SignalSharp.Detection.Pelt.Exceptions;

namespace SignalSharp.Detection.Pelt.Cost;

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
public class L2CostFunction : IPELTCostFunction
{
    private double[] _data = null!;
    private double[,] _means = null!;
    
    /// <summary>
    /// Fits the cost function to the provided data.
    /// </summary>
    /// <param name="data">The data array to fit.</param>
    /// <returns>The fitted <see cref="L2CostFunction"/> instance.</returns>
    /// <remarks>
    /// This method initializes the internal data needed to compute the cost for segments of the data.
    ///
    /// <example>
    /// For example, to fit the cost function to a data array:
    /// <code>
    /// double[] data = {1.0, 2.0, 3.0, 4.0};
    /// var l2Cost = new L2CostFunction().Fit(data);
    /// </code>
    /// This initializes the cost function with the provided data, making it ready for segment cost computation.
    /// </example>
    /// </remarks>
    public IPELTCostFunction Fit(double[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data), "Data must not be null.");
        _means = PrecomputeMeans(data);

        return this;
    }
    
    /// <summary>
    /// Computes the cost for a segment of the data using the L2 norm.
    /// </summary>
    /// <param name="start">The start index of the segment. If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment. If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>The cost function measures the sum of squared deviations from the mean of the segment,
    /// which is useful for detecting change points in time series analysis.</para>
    ///
    /// <para>This method must be called after the <see cref="Fit(double[])"/> method has been used to 
    /// initialize the data.</para>
    ///
    /// <example>
    /// For example, given a fitted L2CostFunction instance:
    /// <code>
    /// var l2Cost = new L2CostFunction().Fit(data);
    /// double cost = l2Cost.ComputeCost(0, 10);
    /// </code>
    /// This computes the cost for the segment of the data from index 0 to index 10.
    /// </example>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when data is not initialized.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    public double ComputeCost(int? start, int? end)
    {
        if (_data is null)
        {
            throw new InvalidOperationException("Data must be set before calling ComputeCost.");
        }

        if (_data.Length == 0)
        {
            return 0;
        }
        
        var startIndex = start ?? 0;
        var endIndex = end ?? _data.Length;

        var segmentLength = endIndex - startIndex;
        if (segmentLength < 1)
        {
            throw new SegmentLengthException("Segment length must be at least 1.");
        }

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Segment start index must be non-negative.");
        }
        
        if (endIndex > _data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "Segment end index must be within the bounds of the data array.");
        }
        
        var mean = _means[startIndex, endIndex - 1];
        
        double sum = 0;
        for (var i = startIndex; i < endIndex; i++)
        {
            sum += Math.Pow(_data[i] - mean, 2);
        }
        
        return sum;
    }

    /// <summary>
    /// Calculates the mean of a segment of the data array.
    /// </summary>
    /// <param name="data">The data array.</param>
    /// <param name="start">The start index of the segment.</param>
    /// <param name="end">The end index of the segment.</param>
    /// <returns>The mean value of the segment.</returns>
    private static double CalculateMean(double[] data, int start, int end)
    {
        double sum = 0;
        
        for (var i = start; i < end; i++)
        {
            sum += data[i];
        }
        
        return sum / (end - start);
    }
    
    /// <summary>
    /// Precomputes the means for all possible segments of the data array.
    /// </summary>
    /// <param name="data">The data array.</param>
    /// <returns>A 2D array of precomputed means for all segments.</returns>
    private static double[,] PrecomputeMeans(double[] data)
    {
        var n = data.Length;
        var means = new double[n, n];

        for (var i = 0; i < n; i++)
        {
            for (var j = i; j < n; j++)
            {
                means[i, j] = CalculateMean(data, i, j + 1);
            }
        }

        return means;
    }
}