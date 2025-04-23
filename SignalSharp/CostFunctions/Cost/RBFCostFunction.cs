using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Utilities;

namespace SignalSharp.CostFunctions.Cost;

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
public class RBFCostFunction : CostFunctionBase
{
    private double[,] _data = null!;
    private double[,,] _prefixSum = null!;

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
    /// <param name="signalMatrix">The signal matrix to fit.</param>
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
    /// double[,] data = { { 1.0, 2.0, 3.0, 4.0 } };
    /// var rbfCost = new RBFCostFunction().Fit(data);
    /// </code>
    /// This initializes the cost function with the provided data, making it ready for segment cost computation.
    /// </example>
    /// </remarks>
    public override IPELTCostFunction Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        _data = signalMatrix;

        var distances = GenerateGramMatrix(signalMatrix, _gamma);
        PrecomputePrefixSum(distances);
        _prefixSum = distances;

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
    /// <para>This method must be called after the <see cref="Fit(double[,])"/> method has been used to
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
    /// <exception cref="UninitializedDataException">Thrown when data, distances, or prefix sum is not initialized.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the segment indices are out of bounds.</exception>
    /// <exception cref="SegmentLengthException">Thrown when the segment length is less than 1.</exception>
    public override double ComputeCost(int? start = null, int? end = null)
    {
        UninitializedDataException.ThrowIfUninitialized(_data, "Fit() must be called before ComputeCost().");

        var startIndex = start ?? 0;
        var endIndex = end ?? _data.GetLength(1);
        var segmentLength = endIndex - startIndex;

        SegmentLengthException.ThrowIfInvalid(segmentLength);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, _data.GetLength(1), nameof(end));

        double sum = 0;
        for (var dim = 0; dim < _data.GetLength(0); dim++)
        {
            var sumAll = ComputeSumFromPrefixSum(_prefixSum, dim, startIndex, endIndex);
            sum += segmentLength - sumAll / segmentLength;
        }

        return sum;
    }

    /// <summary>
    /// Precomputes the prefix sum matrix from the distances matrix.
    /// </summary>
    /// <param name="distances">The distance matrix.</param>
    private static void PrecomputePrefixSum(double[,,] distances)
    {
        var numDimensions = distances.GetLength(0);
        var numPoints = distances.GetLength(1);

        for (var dim = 0; dim < numDimensions; dim++)
        {
            for (var i = 1; i < numPoints; i++)
            {
                distances[dim, i, 0] += distances[dim, i - 1, 0];
                distances[dim, 0, i] = distances[dim, i, 0];
            }

            for (var i = 1; i < numPoints; i++)
            {
                for (var j = 1; j <= i; j++)
                {
                    distances[dim, i, j] += distances[dim, i - 1, j] + distances[dim, i, j - 1] - distances[dim, i - 1, j - 1];
                    distances[dim, j, i] = distances[dim, i, j];
                }
            }
        }
    }

    /// <summary>
    /// Computes the sum from the prefix sum matrix for a given segment.
    /// </summary>
    /// <param name="prefixSum">The prefix sum matrix.</param>
    /// <param name="dimension">The dimension of the data array to compute the sum for.</param>
    /// <param name="start">The start index of the segment.</param>
    /// <param name="end">The end index of the segment.</param>
    /// <returns>The computed sum for the segment.</returns>
    private static double ComputeSumFromPrefixSum(double[,,] prefixSum, int dimension, int start, int end)
    {
        var endRow = end - 1;
        var endCol = end - 1;
        var startRow = start - 1;
        var startCol = start - 1;

        var totalSum = prefixSum[dimension, endRow, endCol];
        var topSum = startRow >= 0 ? prefixSum[dimension, startRow, endCol] : 0;
        var leftSum = startCol >= 0 ? prefixSum[dimension, endRow, startCol] : 0;
        var overlapSum = (startRow >= 0 && startCol >= 0) ? prefixSum[dimension, startRow, startCol] : 0;

        return totalSum - topSum - leftSum + overlapSum;
    }

    /// <summary>
    /// Generates the Gram matrix using the RBF kernel.
    /// </summary>
    /// <param name="data">The data array.</param>
    /// <param name="gamma">The gamma parameter for the RBF kernel.</param>
    /// <returns>The Gram matrix.</returns>
    private static double[,,] GenerateGramMatrix(double[,] data, double? gamma = null)
    {
        var numDimensions = data.GetLength(0);
        var numPoints = data.GetLength(1);
        var gramMatrix = new double[numDimensions, numPoints, numPoints];

        for (var dim = 0; dim < numDimensions; dim++)
        {
            var pairwiseDistances = CalculatePairwiseDistances(data, dim);
            gamma ??= CalculateGamma(pairwiseDistances);
            var kernel = ApplyRBFKernel(pairwiseDistances, gamma.Value);
            for (var i = 0; i < numPoints; i++)
            {
                for (var j = 0; j < numPoints; j++)
                {
                    gramMatrix[dim, i, j] = kernel[i, j];
                }
            }
        }

        return gramMatrix;
    }

    /// <summary>
    /// Calculates the pairwise distances between elements in the data array.
    /// </summary>
    /// <param name="data">The data array.</param>
    /// <param name="dimension">The dimension of the data array to calculate distances for.</param>
    /// <returns>The pairwise distance matrix.</returns>
    private static double[,] CalculatePairwiseDistances(double[,] data, int dimension)
    {
        var numPoints = data.GetLength(1);
        var distances = new double[numPoints, numPoints];

        Parallel.For(
            0,
            numPoints,
            i =>
            {
                for (var j = i; j < numPoints; j++)
                {
                    var distance = SquaredDistance(data[dimension, i], data[dimension, j]);
                    distances[i, j] = distance;
                    distances[j, i] = distance;
                }
            }
        );

        return distances;
    }

    /// <summary>
    /// Applies the RBF kernel to the pairwise distances matrix.
    /// </summary>
    /// <param name="distances">The pairwise distances matrix.</param>
    /// <param name="gamma">The gamma parameter for the RBF kernel.</param>
    /// <returns>The Gram matrix after applying the RBF kernel.</returns>
    private static double[,] ApplyRBFKernel(double[,] distances, double gamma)
    {
        var numPoints = distances.GetLength(0);
        var gramMatrix = new double[numPoints, numPoints];

        Parallel.For(
            0,
            numPoints,
            i =>
            {
                for (var j = 0; j < numPoints; j++)
                {
                    if (distances[i, j] == 0)
                    {
                        gramMatrix[i, j] = 1;
                    }
                    else
                    {
                        var value = distances[i, j] * gamma;
                        value = Math.Clamp(value, 1e-2, 1e2);
                        gramMatrix[i, j] = Math.Exp(-value);
                    }
                }
            }
        );

        return gramMatrix;
    }

    /// <summary>
    /// Calculates the gamma parameter based on the pairwise distances matrix.
    /// </summary>
    /// <param name="distances">The pairwise distances matrix.</param>
    /// <returns>The calculated gamma value.</returns>
    private static double CalculateGamma(double[,] distances)
    {
        var upperTriangleValues = GetUpperTriangleValues(distances);
        var upperTriangleValuesSpan = CollectionsMarshal.AsSpan(upperTriangleValues);

        var median = upperTriangleValues.Count > 0 ? StatisticalFunctions.Median<double>(upperTriangleValuesSpan) : 1.0;

        return median != 0.0 ? 1.0 / median : 1.0;
    }

    /// <summary>
    /// Extracts the upper triangle values from a matrix.
    /// </summary>
    /// <param name="matrix">The matrix to extract values from.</param>
    /// <returns>An array of values from the upper triangle of the matrix.</returns>
    private static List<double> GetUpperTriangleValues(double[,] matrix)
    {
        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);
        var values = new List<double>(rows * (rows - 1) / 2);

        Parallel.For(
            0,
            rows,
            i =>
            {
                for (var j = i + 1; j < cols; j++)
                {
                    values.Add(matrix[i, j]);
                }
            }
        );

        return values;
    }

    /// <summary>
    /// Computes the squared distance between two values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The squared distance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SquaredDistance(double a, double b)
    {
        var diff = a - b;

        return diff * diff;
    }
}
