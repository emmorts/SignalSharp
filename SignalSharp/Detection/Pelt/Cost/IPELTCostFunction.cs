namespace SignalSharp.Detection.Pelt.Cost;

/// <summary>
/// Defines the interface for cost functions used in the Piecewise Linear Trend Change (PELT) algorithm.
/// </summary>
/// <remarks>
/// Implementations of this interface provide methods to fit the cost function to the data and to compute 
/// the cost of a segment of the data. These cost functions are used by the PELT algorithm to evaluate 
/// the quality of segments and detect change points.
/// </remarks>
public interface IPELTCostFunction
{
    /// <summary>
    /// Fits the cost function to the provided data.
    /// </summary>
    /// <param name="signal">The time series data to fit.</param>
    /// <returns>The fitted <see cref="IPELTCostFunction"/> instance.</returns>
    /// <remarks>
    /// This method initializes any internal structures or computations needed to evaluate segment costs 
    /// later on. It prepares the cost function for subsequent calls to <see cref="ComputeCost(int?, int?)"/>.
    /// </remarks>
    IPELTCostFunction Fit(double[] signal);
    
    /// <summary>
    /// Computes the cost for a segment of the data.
    /// </summary>
    /// <param name="start">The start index of the segment. If null, defaults to the beginning of the data.</param>
    /// <param name="end">The end index of the segment. If null, defaults to the end of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// This method calculates the cost or dissimilarity of a segment of the data, which is used by the 
    /// PELT algorithm to determine the optimal segmentation.
    /// </remarks>
    double ComputeCost(int? start = null, int? end = null);
}