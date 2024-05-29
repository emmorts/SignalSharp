using SignalSharp.Detection.Pelt.Exceptions;

namespace SignalSharp.Detection.Pelt.Cost;

/// <summary>
/// Represents a cost function using the Radial Basis Function (RBF) kernel for the Piecewise Linear Trend Change (PELT) method.
/// </summary>
/// <remarks>
/// <para>
/// The Radial Basis Function (RBF) kernel is commonly used in various machine learning algorithms, 
/// such as support vector machines and Gaussian processes, due to its ability to handle non-linear relationships 
/// between data points. It transforms the input data into a higher-dimensional space where it becomes easier 
/// to separate or cluster the data using linear methods.
/// </para>
///
/// <para>
/// In the context of the Piecewise Linear Trend Change (PELT) method, the RBF kernel is utilized to compute 
/// the cost of segmenting a time series or sequential data into different segments where the statistical properties 
/// change. This approach is beneficial when the underlying data relationships are complex and non-linear, 
/// making simple linear models insufficient.
/// </para>
///
/// <para>
/// The gamma parameter in the RBF kernel controls the influence of individual data points. A small gamma value 
/// means that the influence of a single training example reaches far, while a large gamma value means that 
/// the influence is close. The algorithm can automatically compute an appropriate gamma if not provided, 
/// using the median heuristic from the pairwise distances.
/// </para>
///
/// <para>
/// Consider using the RBF cost function in scenarios where:
/// <list type="bullet">
///     <item>The data exhibits complex, non-linear patterns.</item>
///     <item>You need to segment time series data into distinct regimes or phases.</item>
///     <item>Traditional linear models are not capturing the underlying data structure effectively.</item>
/// </list>
/// </para>
/// </remarks>
public class RBFCostFunction : IPELTCostFunction
{
    private double[] _data = null!;
    private double[,] _distances = null!;
    private double[,] _prefixSum = null!;
    
    private readonly double? _gamma;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RBFCostFunction"/> class with an optional gamma parameter.
    /// </summary>
    /// <param name="gamma">The gamma parameter for the RBF kernel. If null, it will be calculated automatically based on the data.</param>
    /// <remarks>
    /// The gamma parameter defines the influence of individual data points in the RBF kernel. 
    /// A smaller gamma value means a broader influence, while a larger gamma value means a more localized influence. 
    /// If not provided, the gamma parameter is estimated using the median heuristic based on pairwise distances 
    /// between data points.
    /// </remarks>
    public RBFCostFunction(double? gamma = null)
    {
        _gamma = gamma;
    }
    
    /// <summary>
    /// Fits the cost function to the provided data.
    /// </summary>
    /// <param name="data">The data array to fit.</param>
    /// <returns>The fitted <see cref="RBFCostFunction"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <remarks>
    /// This method initializes the internal structures needed to compute the cost for segments of the data. 
    /// It calculates the pairwise distances between data points, applies the RBF kernel to these distances, 
    /// and precomputes the prefix sum matrix for efficient cost computation later.
    ///
    /// <example>
    /// For example, to fit the cost function to a data array:
    /// <code>
    /// double[] data = {1.0, 2.0, 3.0, 4.0};
    /// var rbfCost = new RBFCostFunction().Fit(data);
    /// </code>
    /// This initializes the cost function with the provided data, making it ready for segment cost computation.
    /// </example>
    /// </remarks>
    public IPELTCostFunction Fit(double[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data), "Data must not be null.");

        _distances = GenerateGramMatrix(data, _gamma);
        _prefixSum = PrecomputePrefixSum(_distances);

        return this;
    }

    /// <summary>
    /// Computes the cost for a segment of the data using the RBF kernel.
    /// </summary>
    /// <param name="start">The start index of the segment. If null, defaults to 0.</param>
    /// <param name="end">The end index of the segment. If null, defaults to the length of the data.</param>
    /// <returns>The computed cost for the segment.</returns>
    /// <remarks>
    /// <para>The cost function measures the dissimilarity of the segment compared to the rest of the data,
    /// which is useful for detecting change points in time series analysis.</para>
    ///
    /// <para>This method must be called after the <see cref="Fit(double[])"/> method has been used to 
    /// initialize the data and compute the necessary matrices.</para>
    ///
    /// <example>
    /// For example, given a fitted RBFCostFunction instance:
    /// <code>
    /// var rbfCost = new RBFCostFunction().Fit(data);
    /// double cost = rbfCost.ComputeCost(0, 10);
    /// </code>
    /// This computes the cost for the segment of the data from index 0 to index 10.
    /// </example>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when data, distances, or prefix sum is not initialized.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    public double ComputeCost(int? start = null, int? end = null)
    {
        if (_data is null)
        {
            throw new InvalidOperationException("Data must be set before calling ComputeCost.");
        }

        var startIndex = start ?? 0;
        var endIndex = end ?? _data.Length;

        var segmentLength = endIndex - startIndex;
        if (segmentLength < 1)
        {
            throw new SegmentLengthException("Segment length must be at least 1.");
        }

        if (_distances is null)
        {
            throw new InvalidOperationException("Distances matrix is not initialized. Call Fit method first.");
        }
        
        if (_prefixSum is null)
        {
            throw new InvalidOperationException("Data must be set before calling ComputeCost.");
        }

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Segment start index must be non-negative.");
        }
        
        if (endIndex > _data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "Segment end index must be within the bounds of the data array.");
        }
        
        var sumAll = ComputeSumFromPrefixSum(_prefixSum, startIndex, endIndex);

        return segmentLength - sumAll / segmentLength;
    }
    
    /// <summary>
    /// Precomputes the prefix sum matrix from the distances matrix.
    /// </summary>
    /// <param name="distances">The distances matrix.</param>
    /// <returns>The prefix sum matrix.</returns>
    private static double[,] PrecomputePrefixSum(double[,] distances)
    {
        var n = distances.GetLength(0);
        
        var prefixSum = new double[n, n];

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                prefixSum[i, j] = distances[i, j] // current
                                   + (i > 0 ? prefixSum[i - 1, j] : 0) // top
                                   + (j > 0 ? prefixSum[i, j - 1] : 0) // left
                                   - (i > 0 && j > 0 ? prefixSum[i - 1, j - 1] : 0); // above left overlap
            }
        }

        return prefixSum;
    }
    
    /// <summary>
    /// Computes the sum from the prefix sum matrix for a given segment.
    /// </summary>
    /// <param name="prefixSum">The prefix sum matrix.</param>
    /// <param name="start">The start index of the segment.</param>
    /// <param name="end">The end index of the segment.</param>
    /// <returns>The computed sum for the segment.</returns>
    private static double ComputeSumFromPrefixSum(double[,] prefixSum, int start, int end)
    {
        var endRow = end - 1;
        var endCol = end - 1;
        var startRow = start - 1;
        var startCol = start - 1;

        var totalSum = prefixSum[endRow, endCol];
        var topSum = startRow >= 0 ? prefixSum[startRow, endCol] : 0;
        var leftSum = startCol >= 0 ? prefixSum[endRow, startCol] : 0;
        var overlapSum = (startRow >= 0 && startCol >= 0) ? prefixSum[startRow, startCol] : 0;

        return totalSum - topSum - leftSum + overlapSum;
    }

    /// <summary>
    /// Generates the Gram matrix using the RBF kernel.
    /// </summary>
    /// <param name="data">The data array.</param>
    /// <param name="gamma">The gamma parameter for the RBF kernel.</param>
    /// <returns>The Gram matrix.</returns>
    private static double[,] GenerateGramMatrix(double[] data, double? gamma = null)
    {
        var pairwiseDistances = CalculatePairwiseDistances(data);

        gamma ??= CalculateGamma(pairwiseDistances);

        return ApplyRBFKernel(pairwiseDistances, gamma.Value);
    }

    /// <summary>
    /// Calculates the pairwise distances between elements in the data array.
    /// </summary>
    /// <param name="data">The data array.</param>
    /// <returns>The pairwise distances matrix.</returns>
    private static double[,] CalculatePairwiseDistances(double[] data)
    {
        var n = data.Length;
        var distances = new double[n, n];

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var dist = Math.Pow(data[i] - data[j], 2);

                distances[i, j] = dist;
            }
        }

        return distances;
    }

    /// <summary>
    /// Calculates the gamma parameter based on the pairwise distances matrix.
    /// </summary>
    /// <param name="distances">The pairwise distances matrix.</param>
    /// <returns>The calculated gamma value.</returns>
    private static double CalculateGamma(double[,] distances)
    {
        var upperTriangleValues = GetUpperTriangleValues(distances);

        var median = upperTriangleValues.Length > 0 ? Median(upperTriangleValues) : 1.0;

        return median != 0.0
            ? 1.0 / median
            : 1.0;
    }

    /// <summary>
    /// Applies the RBF kernel to the pairwise distances matrix.
    /// </summary>
    /// <param name="distances">The pairwise distances matrix.</param>
    /// <param name="gamma">The gamma parameter for the RBF kernel.</param>
    /// <returns>The Gram matrix after applying the RBF kernel.</returns>
    private static double[,] ApplyRBFKernel(double[,] distances, double gamma)
    {
        var n = distances.GetLength(0);
        var gramMatrix = new double[n, n];

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                if (distances[i, j] == 0)
                {
                    gramMatrix[i, j] = 1;
                }
                else
                {
                    var value = distances[i, j] * gamma;

                    value = Math.Clamp(value, 1e-2, 1e2); // Clipping to avoid exponential under/overflow

                    gramMatrix[i, j] = Math.Exp(-value);
                }
            }
        }

        return gramMatrix;
    }

    /// <summary>
    /// Extracts the upper triangle values from a matrix.
    /// </summary>
    /// <param name="matrix">The matrix to extract values from.</param>
    /// <returns>An array of values from the upper triangle of the matrix.</returns>
    private static double[] GetUpperTriangleValues(double[,] matrix)
    {
        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);
        var values = new List<double>();

        for (var i = 0; i < rows; i++)
        {
            for (var j = i + 1; j < cols; j++)
            {
                values.Add(matrix[i, j]);
            }
        }

        return values.ToArray();
    }

    /// <summary>
    /// Computes the median value of an array of doubles.
    /// </summary>
    /// <param name="data">The array of doubles.</param>
    /// <returns>The median value.</returns>
    private static double Median(double[] data)
    {
        Array.Sort(data);

        var n = data.Length;

        if (n % 2 == 0)
        {
            return (data[n / 2 - 1] + data[n / 2]) / 2.0;
        }

        return data[n / 2];
    }
}