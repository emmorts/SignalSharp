using SignalSharp.Detection.Pelt.Models;

namespace SignalSharp.Detection.Pelt;

/// <summary>
/// Implements the Piecewise Linear Trend Change (PELT) algorithm for segmenting time series data.
/// </summary>
/// <remarks>
/// <para>
/// The PELT algorithm is a dynamic programming approach for detecting multiple change points in time series data.
/// It aims to partition the data into segments where the statistical properties are homogenous within each segment
/// but differ between segments. This algorithm is efficient and scales well with the size of the data.
/// </para>
///
/// <para>
/// The PELT algorithm uses a cost function to measure the fit of the segments and a penalty term to control the 
/// number of change points. The penalty term helps prevent overfitting by discouraging too many segments.
/// </para>
/// </remarks>
public class PELTAlgorithm
{
    private readonly PELTOptions _options;
    private double[] _signal = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="PELTAlgorithm"/> class with optional configuration settings.
    /// </summary>
    /// <param name="options">The configuration options for the PELT algorithm. If null, default options are used.</param>
    public PELTAlgorithm(PELTOptions? options)
    {
        _options = options ?? new PELTOptions();
    }

    /// <summary>
    /// Fits the PELT algorithm to the provided signal data.
    /// </summary>
    /// <param name="signal">The time series data to be segmented.</param>
    /// <returns>The fitted <see cref="PELTAlgorithm"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal data is null.</exception>
    /// <remarks>
    /// This method initializes the internal structures and cost function needed to compute the change points.
    ///
    /// <example>
    /// For example, to fit the algorithm to a signal:
    /// <code>
    /// double[] signal = {1.0, 2.0, 3.0, 4.0};
    /// var pelt = new PELTAlgorithm().Fit(signal);
    /// </code>
    /// This initializes the algorithm with the provided signal data.
    /// </example>
    /// </remarks>
    public PELTAlgorithm Fit(double[] signal)
    {
        _signal = signal ?? throw new ArgumentNullException(nameof(signal), "Data must not be null.");

        _options.CostFunction.Fit(signal);
        
        return this;
    }

    /// <summary>
    /// Predicts the change points in the fitted signal using the specified penalty value.
    /// </summary>
    /// <param name="pen">The penalty value to control the number of change points.</param>
    /// <returns>An array of indices representing the change points in the signal.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Fit method has not been called before Predict.</exception>
    /// <remarks>
    /// This method uses the PELT algorithm to identify the change points in the time series data.
    ///
    /// <example>
    /// For example, given a fitted PELTAlgorithm instance:
    /// <code>
    /// var pelt = new PELTAlgorithm().Fit(signal);
    /// int[] changePoints = pelt.Predict(10.0);
    /// </code>
    /// This computes the change points in the signal with a penalty value of 10.0.
    /// </example>
    /// </remarks>
    public int[] Predict(double pen)
    {
        if (_signal is null)
        {
            throw new InvalidOperationException("Fit must be called before Predict.");
        }

        var partition = Segment(pen);
        
        return ExtractBreakpoints(partition);
    }

    /// <summary>
    /// Fits the PELT algorithm to the provided signal data and predicts the change points using the specified penalty value.
    /// </summary>
    /// <param name="signal">The time series data to be segmented.</param>
    /// <param name="pen">The penalty value to control the number of change points.</param>
    /// <returns>An array of indices representing the change points in the signal.</returns>
    /// <example>
    /// For example, to fit and predict the change points in one step:
    /// <code>
    /// double[] signal = {1.0, 2.0, 3.0, 4.0};
    /// var pelt = new PELTAlgorithm();
    /// int[] changePoints = pelt.FitPredict(signal, 10.0);
    /// </code>
    /// This fits the algorithm to the signal and computes the change points with a penalty value of 10.0.
    /// </example>
    public int[] FitPredict(double[] signal, double pen)
    {
        return Fit(signal).Predict(pen);
    }

    /// <summary>
    /// Segments the signal using the PELT algorithm with the given penalty.
    /// </summary>
    /// <param name="pen">The penalty value to control the number of change points.</param>
    /// <returns>A dictionary representing the partitioned segments and their associated costs.</returns>
    private Dictionary<(int, int), double> Segment(double pen)
    {
        var partitions = InitializePartitions();
        var admissible = new List<int>();
        var indices = GenerateIndices();

        foreach (var bkp in indices)
        {
            admissible = UpdateAdmissibleList(admissible, bkp);
            var candidatePartitions = GenerateCandidatePartitions(admissible, bkp, pen, partitions);

            partitions[bkp] = candidatePartitions.MinBy(d => d.Values.Sum())!;
            admissible = FilterAdmissible(admissible, candidatePartitions, partitions, bkp, pen);
        }

        return CleanPartition(partitions[_signal.Length]);
    }
    
    /// <summary>
    /// Initializes the partitions dictionary with the starting point.
    /// </summary>
    /// <returns>A dictionary with the initial partition at index 0.</returns>
    private static Dictionary<int, Dictionary<(int, int), double>> InitializePartitions()
    {
        return new Dictionary<int, Dictionary<(int, int), double>>
        {
            { 0, new Dictionary<(int, int), double> { { (0, 0), 0 } } }
        };
    }

    /// <summary>
    /// Generates the indices to consider for potential change points.
    /// </summary>
    /// <returns>A list of indices to evaluate as change points.</returns>
    private List<int> GenerateIndices()
    {
        return Enumerable.Range(0, _signal.Length)
            .Where(k => k % _options.Jump == 0 && k >= _options.MinSize)
            .Concat(new[] { _signal.Length })
            .ToList();
    }
    
    /// <summary>
    /// Updates the admissible list with a new potential change point.
    /// </summary>
    /// <param name="admissible">The current list of admissible change points.</param>
    /// <param name="breakpoint">The new breakpoint to be added.</param>
    /// <returns>The updated list of admissible change points.</returns>
    private List<int> UpdateAdmissibleList(List<int> admissible, int breakpoint)
    {
        var newAdmPt = (int)Math.Floor((breakpoint - _options.MinSize) / (double)_options.Jump) * _options.Jump;
        admissible.Add(newAdmPt);
        return admissible;
    }
    
    /// <summary>
    /// Generates candidate partitions for a given breakpoint and penalty.
    /// </summary>
    /// <param name="admissible">The list of admissible change points.</param>
    /// <param name="breakpoint">The current breakpoint being evaluated.</param>
    /// <param name="penalty">The penalty value to control the number of change points.</param>
    /// <param name="partitions">The current partitions dictionary.</param>
    /// <returns>A list of candidate partitions for the given breakpoint.</returns>
    private List<Dictionary<(int, int), double>> GenerateCandidatePartitions(List<int> admissible, int breakpoint, 
        double penalty, Dictionary<int, Dictionary<(int, int), double>> partitions)
    {
        var candidatePartitions = new List<Dictionary<(int, int), double>>();
        
        foreach (var t in admissible)
        {
            if (!partitions.TryGetValue(t, out var tmpPartition)) continue;

            var newPartition = new Dictionary<(int, int), double>(tmpPartition)
            {
                [(t, breakpoint)] = _options.CostFunction.ComputeCost(t, breakpoint) + penalty
            };

            candidatePartitions.Add(newPartition);
        }

        return candidatePartitions;
    }

    /// <summary>
    /// Filters the list of admissible change points based on the candidate partitions and current partitions.
    /// </summary>
    /// <param name="admissible">The current list of admissible change points.</param>
    /// <param name="candidatePartitions">The list of candidate partitions for the current breakpoint.</param>
    /// <param name="partitions">The current partitions dictionary.</param>
    /// <param name="breakpoint">The current breakpoint being evaluated.</param>
    /// <param name="penalty">The penalty value to control the number of change points.</param>
    /// <returns>The filtered list of admissible change points.</returns>
    private static List<int> FilterAdmissible(List<int> admissible, List<Dictionary<(int, int), double>> candidatePartitions, 
        Dictionary<int, Dictionary<(int, int), double>> partitions, int breakpoint, double penalty)
    {
        return admissible
            .Zip(candidatePartitions, (t, partition) => new { t, partition })
            .Where(x => x.partition.Values.Sum() <= partitions[breakpoint].Values.Sum() + penalty)
            .Select(x => x.t)
            .ToList();
    }

    /// <summary>
    /// Cleans the final partition dictionary by removing the initial dummy partition.
    /// </summary>
    /// <param name="partition">The partition dictionary to clean.</param>
    /// <returns>The cleaned partition dictionary.</returns>
    private static Dictionary<(int, int), double> CleanPartition(Dictionary<(int, int), double> partition)
    {
        var bestPartition = new Dictionary<(int, int), double>(partition);
        bestPartition.Remove((0, 0));
        return bestPartition;
    }

    /// <summary>
    /// Extracts the breakpoints from the final partition dictionary.
    /// </summary>
    /// <param name="partition">The final partition dictionary.</param>
    /// <returns>An array of indices representing the breakpoints.</returns>
    private int[] ExtractBreakpoints(Dictionary<(int, int), double> partition)
    {
        return partition.Keys
            .Select(tuple => tuple.Item2)
            .Where(x => x != _signal.Length)
            .OrderBy(x => x)
            .ToArray();
    }
}