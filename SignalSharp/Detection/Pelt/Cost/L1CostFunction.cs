using SignalSharp.Detection.Pelt.Exceptions;

namespace SignalSharp.Detection.Pelt.Cost;

/// <summary>
/// Represents a cost function using the L1 norm for the Piecewise Linear Trend Change (PELT) method.
/// </summary>
/// <remarks>
/// <para>
/// The L1 norm, also known as the Manhattan distance or absolute deviation, is a measure of distance 
/// between points in a multi-dimensional space. It is calculated as the sum of the absolute differences 
/// between the coordinates of the points.
/// </para>
///
/// <para>
/// In the context of the Piecewise Linear Trend Change (PELT) method, the L1 cost function is used to 
/// compute the cost of segmenting a time series or sequential data into different segments where the 
/// statistical properties change. The L1 norm is particularly robust to outliers, making it a good choice 
/// when the data contains anomalies or non-Gaussian noise.
/// </para>
///
/// <para>
/// Consider using the L1 cost function in scenarios where:
/// <list type="bullet">
///     <item>The data contains outliers or non-Gaussian noise.</item>
///     <item>You need a robust measure of segment dissimilarity.</item>
///     <item>Traditional L2 norm (Euclidean distance) based models are too sensitive to outliers.</item>
/// </list>
/// </para>
/// </remarks>
public class L1CostFunction : IPELTCostFunction
{
    private double[] _data = null!;
    
    /// <summary>
    /// Fits the cost function to the provided data.
    /// </summary>
    /// <param name="data">The data array to fit.</param>
    /// <returns>The fitted <see cref="L1CostFunction"/> instance.</returns>
    /// <remarks>
    /// This method initializes the internal data needed to compute the cost for segments of the data.
    ///
    /// <example>
    /// For example, to fit the cost function to a data array:
    /// <code>
    /// double[] data = {1.0, 2.0, 3.0, 4.0};
    /// var l1Cost = new L1CostFunction().Fit(data);
    /// </code>
    /// This initializes the cost function with the provided data, making it ready for segment cost computation.
    /// </example>
    /// </remarks>
    public IPELTCostFunction Fit(double[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data), "Data must not be null.");

        return this;
    }
    
    /// <summary>
    /// Computes the cost for a segment of the data using the L1 norm.
    /// </summary>
    /// <param name="start">The start index of the segment. If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment. If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>The cost function measures the sum of absolute deviations from the median of the segment,
    /// which is useful for detecting change points in time series analysis.</para>
    ///
    /// <para>This method must be called after the <see cref="Fit(double[])"/> method has been used to 
    /// initialize the data.</para>
    ///
    /// <example>
    /// For example, given a fitted L1CostFunction instance:
    /// <code>
    /// var l1Cost = new L1CostFunction().Fit(data);
    /// double cost = l1Cost.ComputeCost(0, 10);
    /// </code>
    /// This computes the cost for the segment of the data from index 0 to index 10.
    /// </example>
    /// </remarks>
    /// <exception cref="PeltSignalDataUndefinedException">Thrown when data is not initialized.</exception>
    public double ComputeCost(int? start, int? end)
    {
        if (_data is null)
        {
            throw new PeltSignalDataUndefinedException("Data must be set before calling ComputeCost.");
        }
        
        double sum = 0;
            
        start ??= 0;
        end ??= _data.Length;
        
        var median = CalculateMedian(_data, start.Value, end.Value);
        
        for (var i = start.Value; i < end.Value; i++)
        {
            sum += Math.Abs(_data[i] - median);
        }
        
        return sum;
    }

    /// <summary>
    /// Calculates the median of a segment of the data array.
    /// </summary>
    /// <param name="data">The data array.</param>
    /// <param name="start">The start index of the segment.</param>
    /// <param name="end">The end index of the segment.</param>
    /// <returns>The median value of the segment.</returns>
    private static double CalculateMedian(double[] data, int start, int end)
    {
        var length = end - start;
        var sortedData = new double[length];
        
        Array.Copy(data, start, sortedData, 0, length);
        Array.Sort(sortedData);
        
        if (length % 2 == 0)
        {
            return (sortedData[length / 2 - 1] + sortedData[length / 2]) / 2;
        }

        return sortedData[length / 2];
    }
}