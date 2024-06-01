namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Provides a base implementation of the <see cref="IPELTCostFunction"/> interface.
/// </summary>
/// <remarks>
/// This abstract class includes a default implementation for fitting one-dimensional time series data by 
/// converting it to a two-dimensional format. Subclasses must implement the methods for fitting 
/// multidimensional time series data and computing the cost for a data segment.
/// </remarks>
public abstract class CostFunctionBase : IPELTCostFunction
{
    /// <summary>
    /// Fits the cost function to the provided one-dimensional time series data.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to fit.</param>
    /// <returns>The fitted <see cref="IPELTCostFunction"/> instance.</returns>
    /// <remarks>
    /// This method converts the one-dimensional data into a two-dimensional format and calls 
    /// <see cref="Fit(double[,])"/> to perform the actual fitting.
    /// </remarks>
    public IPELTCostFunction Fit(double[] signal)
    {
        var signalMatrix = new double[1, signal.Length];
        for (var i = 0; i < signal.Length; i++)
        {
            signalMatrix[0, i] = signal[i];
        }
        
        return Fit(signalMatrix);
    }

    /// <summary>
    /// Fits the cost function to the provided multi-dimensional time series data.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional time series data to fit, where each row represents a different time series.</param>
    /// <returns>The fitted <see cref="IPELTCostFunction"/> instance.</returns>
    /// <remarks>
    /// Subclasses must implement this method to initialize any internal structures or computations needed 
    /// to evaluate segment costs later on. It prepares the cost function for subsequent calls to 
    /// <see cref="ComputeCost(int?, int?)"/>.
    /// </remarks>
    public abstract IPELTCostFunction Fit(double[,] signalMatrix);

    /// <summary>
    /// Computes the cost for a segment of the data.
    /// </summary>
    /// <param name="start">The start index of the segment. If null, defaults to the beginning of the data.</param>
    /// <param name="end">The end index of the segment. If null, defaults to the end of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// Subclasses must implement this method to calculate the cost or dissimilarity of a segment of the 
    /// data, which is used by the PELT algorithm to determine the optimal segmentation.
    /// </remarks>
    public abstract double ComputeCost(int? start = null, int? end = null);
}