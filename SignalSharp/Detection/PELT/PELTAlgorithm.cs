using Microsoft.Extensions.Logging;
using SignalSharp.Common.Exceptions;
using SignalSharp.Detection.PELT.Exceptions;
using SignalSharp.Logging;

namespace SignalSharp.Detection.PELT;

/// <summary>
/// Implements the Piecewise Linear Trend Change (PELT) algorithm for segmenting time series data.
/// </summary>
/// <remarks>
/// <para>
/// The PELT algorithm is an exact and efficient dynamic programming approach for detecting multiple change points
/// in time series data. It aims to partition the data into segments where the statistical properties are
/// homogeneous within each segment but differ between segments. This implementation uses pruning to achieve
/// near-linear time complexity under certain conditions.
/// </para>
/// <para>
/// The algorithm minimizes the sum of segment costs plus a penalty term for each changepoint.
/// The cost function measures the fit or homogeneity of segments, and the penalty controls the number
/// of change points, preventing overfitting.
/// </para>
/// <para>
/// When `Jump` > 1 in `PELTOptions`, the algorithm becomes approximate by only checking potential
/// previous changepoints at intervals defined by `Jump`, significantly increasing speed for some cost functions
/// at the cost of guaranteed optimality. For exact results, use `Jump = 1`.
/// </para>
/// </remarks>
public class PELTAlgorithm : IPELTAlgorithm 
{
    public PELTOptions Options { get; }
    
    private double[,] _signal = null!;
    private int _signalLength;
    
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PELTAlgorithm"/> class with specified configuration options.
    /// </summary>
    /// <param name="options">The configuration options for the PELT algorithm. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when options are null.</exception>
    public PELTAlgorithm(PELTOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(options.CostFunction, $"{nameof(options.CostFunction)} cannot be null.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MinSize, nameof(options.MinSize));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Jump, nameof(options.Jump)); 

        Options = options;
        _logger = LoggerProvider.CreateLogger<PELTAlgorithm>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PELTAlgorithm"/> class with default options (L2 Cost, MinSize=2, Jump=5).
    /// </summary>
    public PELTAlgorithm() : this(new PELTOptions())
    {
    }

    /// <summary>
    /// Fits the PELT algorithm to the provided one-dimensional time series data.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to fit.</param>
    /// <returns>The fitted <see cref="PELTAlgorithm"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal data is null.</exception>
    /// <remarks>
    /// Prepares the algorithm by setting the internal signal representation and fitting the chosen cost function.
    /// <example>
    /// <code>
    /// double[] signal = {1.0, 2.0, 3.0, 10.0, 11.0, 12.0};
    /// var pelt = new PELTAlgorithm().Fit(signal);
    /// </code>
    /// </example>
    /// </remarks>
    public virtual IPELTAlgorithm Fit(double[] signal)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));

        var signalMatrix = new double[1, signal.Length];
        for (var i = 0; i < signal.Length; i++)
        {
            signalMatrix[0, i] = signal[i];
        }

        return Fit(signalMatrix);
    }

    /// <summary>
    /// Fits the PELT algorithm to the provided multi-dimensional time series data.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional time series data to fit, where each row represents a dimension and each column a time point.</param>
    /// <returns>The fitted <see cref="PELTAlgorithm"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal data is null.</exception>
    /// <remarks>
    /// Prepares the algorithm by setting the internal signal representation and fitting the chosen cost function.
    /// <example>
    /// <code>
    /// double[,] signal = { { 1.0, 2.0, 3.0 }, { 10.0, 11.0, 12.0 } };
    /// var pelt = new PELTAlgorithm().Fit(signal);
    /// </code>
    /// </example>
    /// </remarks>
    public virtual IPELTAlgorithm Fit(double[,] signalMatrix)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        ArgumentNullException.ThrowIfNull(Options.CostFunction, "Cost function must be initialized.");

        _signal = signalMatrix;
        _signalLength = _signal.GetLength(1);
        Options.CostFunction.Fit(signalMatrix);

        return this;
    }

    /// <summary>
    /// Detects the change points in the fitted signal using the specified penalty value.
    /// </summary>
    /// <param name="penalty">The penalty value to control the number of change points. Must be non-negative.</param>
    /// <returns>An array of indices representing the change points in the signal. The indices correspond to the first point *after* the change.</returns>
    /// <exception cref="UninitializedDataException">Thrown when Fit method has not been called before Detect.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the penalty is negative.</exception>
    /// <remarks>
    /// This method executes the core PELT algorithm to find the optimal segmentation based on the fitted data, cost function, and penalty.
    /// If `Jump > 1` was specified in options, this may return an approximate solution.
    /// <example>
    /// <code>
    /// var pelt = new PELTAlgorithm().Fit(signal);
    /// int[] changePoints = pelt.Detect(10.0);
    /// </code>
    /// </example>
    /// </remarks>
    public virtual int[] Detect(double penalty)
    {
        UninitializedDataException.ThrowIfUninitialized(_signal, "Fit() must be called before Detect().");
        ArgumentOutOfRangeException.ThrowIfNegative(penalty, nameof(penalty));

        if (_signalLength < Options.MinSize)
        {
            _logger.LogWarning("Signal length ({SignalLength}) is less than MinSize ({MinSize}). No changepoints possible.", _signalLength, Options.MinSize);
            return []; // not enough data even for one segment
        }
        if (_signalLength < Options.MinSize * 2)
        {
            _logger.LogInformation("Signal length ({SignalLength}) is less than MinSize*2 ({MinSizeX2}). Cannot have any changepoints.", _signalLength, Options.MinSize*2);
            return []; // not enough data for a changepoint
        }

        var lastChangePoints = Segment(penalty);
        return ExtractBreakpoints(lastChangePoints);
    }

    /// <summary>
    /// Fits the PELT algorithm to the provided one-dimensional signal data and detects the change points using the specified penalty value.
    /// </summary>
    /// <param name="signal">The one-dimensional time series data to be segmented.</param>
    /// <param name="penalty">The penalty value to control the number of change points. Must be non-negative.</param>
    /// <returns>An array of indices representing the change points in the signal.</returns>
    /// <example>
    /// <code>
    /// double[] signal = {1, 1, 1, 10, 10, 10, 1, 1, 1};
    /// var pelt = new PELTAlgorithm(); // Uses default options
    /// int[] changePoints = pelt.FitAndDetect(signal, 5.0); // Example penalty
    /// // changePoints might be [3, 6] depending on cost function and penalty
    /// </code>
    /// </example>
    public virtual int[] FitAndDetect(double[] signal, double penalty)
    {
        return Fit(signal).Detect(penalty);
    }

    /// <summary>
    /// Fits the PELT algorithm to the provided multi-dimensional signal data and detects the change points using the specified penalty value.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional time series data to be segmented, where each row represents a dimension.</param>
    /// <param name="penalty">The penalty value to control the number of change points. Must be non-negative.</param>
    /// <returns>An array of indices representing the change points in the signal.</returns>
    /// <example>
    /// <code>
    /// double[,] signal = { { 1, 1, 10, 10 }, { 5, 5, 20, 20 } };
    /// var pelt = new PELTAlgorithm(); // Uses default options
    /// int[] changePoints = pelt.FitAndDetect(signal, 8.0); // Example penalty
    /// // changePoints might be [2]
    /// </code>
    /// </example>
    public virtual int[] FitAndDetect(double[,] signalMatrix, double penalty)
    {
        return Fit(signalMatrix).Detect(penalty);
    }

    /// <summary>
    /// Performs the core PELT segmentation algorithm.
    /// </summary>
    /// <param name="penalty">The penalty value for adding a changepoint.</param>
    /// <returns>An array where the value at index `t` indicates the optimal last changepoint before `t`.</returns>
    private int[] Segment(double penalty)
    {
        // F[t] stores the minimum cost for the signal segment 0...t
        var F = new double[_signalLength + 1];
        // CP[t] stores the optimal last changepoint index for the minimum cost F[t]
        var CP = new int[_signalLength + 1];

        F[0] = -penalty; // offset penalty for the first segment
        for (var i = 1; i <= _signalLength; i++)
        {
            F[i] = double.PositiveInfinity;
        }

        // R_t stores the set of admissible last changepoint candidates for endpoint t
        var admissible = new HashSet<int> { 0 }; 

        for (var currentEndPoint = Options.MinSize; currentEndPoint <= _signalLength; currentEndPoint++)
        {
            var currentMinCost = double.PositiveInfinity;
            var currentOptimalLastCp = 0;

            // --- Find the best cost partitioning ending at currentEndPoint ---
            // iterate backwards from potential start points, stepping by Jump
            // only consider points that are still in the admissible set
            var startCheck = currentEndPoint - Options.MinSize;
            for (var prevCpCandidate = startCheck; prevCpCandidate >= 0; prevCpCandidate -= Options.Jump)
            {
                if (!admissible.Contains(prevCpCandidate))
                {
                    // if Jump > 1, we might need to check points between the jumps
                    // if the exact point wasn't admissible. A simpler approach for
                    // approximate PELT is often to just check the points at the jump intervals
                    // that ARE admissible. Let's stick to that for now.
                    // if Jump = 1, this check ensures we only process admissible points efficiently.
                    continue; 
                }

                try
                {
                    var segmentCost = Options.CostFunction.ComputeCost(prevCpCandidate, currentEndPoint);
                    var costWithCandidate = F[prevCpCandidate] + segmentCost + penalty;

                    if (costWithCandidate < currentMinCost)
                    {
                        currentMinCost = costWithCandidate;
                        currentOptimalLastCp = prevCpCandidate;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error computing cost for segment ({prevCpCandidate}, {currentEndPoint}): {Message}", prevCpCandidate, currentEndPoint, ex.Message);
                    // todo: should we throw?
                    // throw new PELTAlgorithmException($"Error computing cost for segment ({prevCpCandidate}, {currentEndPoint}): {ex.Message}", ex);
                }

                // if Jump=1, need to check the next point. If Jump>1, we jump.
                if (Options.Jump <= 1 || prevCpCandidate <= 0 || prevCpCandidate - Options.Jump >= 0) continue;
                    
                // ensure we check index 0 if it's admissible and reachable by the jump logic
                if (admissible.Contains(0) && currentEndPoint - 0 >= Options.MinSize)
                {
                    prevCpCandidate = Options.Jump;
                }
            }
            // --- End Cost Calculation Loop ---

            if (double.IsPositiveInfinity(currentMinCost) && currentEndPoint >= Options.MinSize)
            {
                // this might happen if the only admissible point is 0, and cost(0, currentEndPoint) failed or if no admissible point led to a valid segment.
                // indicate this segment is unreachable or problematic
                // assigning infinity ensures it won't be part of a valid path
                F[currentEndPoint] = double.PositiveInfinity;
                // CP[currentEndPoint] remains 0 (or its default) - might need careful backtracking later
                // let's default CP to -1 to indicate no valid predecessor found? Or stick with 0.
                CP[currentEndPoint] = -1;
            }
            else
            {
                F[currentEndPoint] = currentMinCost;
                CP[currentEndPoint] = currentOptimalLastCp;
            }
                

            // --- pruning step: update the admissible set for the next iteration ---
            var nextAdmissible = new HashSet<int>(); // Use HashSet here too
            foreach (var s in admissible)
            {
                var segmentIsValidForCost = currentEndPoint - s >= Options.MinSize;
                if (segmentIsValidForCost)
                {
                    try
                    {
                        var segmentCost = Options.CostFunction.ComputeCost(s, currentEndPoint);
                        // standard PELT pruning condition: keep s if it's still competitive
                        // use F[currentEndPoint] calculated above (might be approximate if Jump > 1)
                        if (F[s] + segmentCost <= F[currentEndPoint])
                        {
                            nextAdmissible.Add(s);
                        }
                        // else: Prune s, do not add to nextAdmissible
                    }
                    catch (Exception)
                    {
                        // if cost calculation fails during pruning check, we stay conservative (as the main loop
                        // handles optimal path errors) and keep 's' in the admissible set, but only
                        // if F[s] was reachable.
                        if (!double.IsPositiveInfinity(F[s]))
                        {
                            nextAdmissible.Add(s); 
                        }
                        
                        _logger.LogWarning("Cost calculation failed during pruning check for segment ({s}, {currentEndPoint}). Keeping s in admissible set.", s, currentEndPoint);
                    }
                }
                else
                {
                    // if segment s->currentEndPoint is not yet MinSize, 's' cannot be pruned based on cost yet.
                    // it must remain admissible *if it was reachable* (F[s] is finite).
                    if (!double.IsPositiveInfinity(F[s]))
                    {
                        nextAdmissible.Add(s);
                    }
                }
            }
            // current endpoint becomes a potential starting point for the next segment
            // only add if it was actually reachable (F is not infinity)
            if (!double.IsPositiveInfinity(F[currentEndPoint]))
            {
                nextAdmissible.Add(currentEndPoint);
            }
            admissible = nextAdmissible;
        }

        return CP;
    }


    /// <summary>
    /// Extracts the breakpoints from the array of last changepoint indices.
    /// </summary>
    /// <param name="lastChangePoints">The CP array computed by the Segment method.</param>
    /// <returns>An array of indices representing the breakpoints.</returns>
    private int[] ExtractBreakpoints(int[] lastChangePoints)
    {
        var breakpoints = new List<int>();
        var currentIndex = _signalLength;

        while (currentIndex > 0)
        {
            var prevCp = lastChangePoints[currentIndex];
            if (prevCp <= 0)
            {
                // if prevCp is -1, it means F[currentIndex] was +inf, path is broken.
                if (prevCp == -1)
                {
                    _logger.LogWarning("Breakpoint reconstruction encountered an unreachable point at index {currentIndex}. Results may be incomplete.", currentIndex);
                }
                break;
            }

            // add the breakpoint (the start index of the segment ending at currentIndex)
            breakpoints.Insert(0, prevCp);

            currentIndex = prevCp;

            // safety break for potential infinite loops
            if (breakpoints.Count <= _signalLength) continue;
                
            throw new PELTAlgorithmException("Breakpoint reconstruction failed due to potential loop.");
        }

        return breakpoints.ToArray();
    }
}