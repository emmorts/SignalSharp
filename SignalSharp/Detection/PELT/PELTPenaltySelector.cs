using System.Globalization;
using Microsoft.Extensions.Logging;
using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Detection.PELT.Exceptions;
using SignalSharp.Logging;
using SignalSharp.Utilities;

namespace SignalSharp.Detection.PELT;

/// <summary>
/// Provides methods to automatically select a penalty for the PELT algorithm using various criteria like BIC, AIC, or AICc.
/// </summary>
/// <remarks>
/// <para>
/// Penalty selection is crucial for controlling the number of change points detected by PELT.
/// This class implements common information criteria for model selection based on likelihood principles.
/// </para>
/// <para>
/// Note that likelihood-based criteria (BIC, AIC, AICc) require the cost function used in the PELT algorithm
/// to implement <see cref="ILikelihoodCostFunction"/> correctly and set <c>SupportsInformationCriteria</c> to true.
/// The PELT algorithm instance provided to this selector must be fitted with the signal data *before* calling
/// the `SelectPenaltyInternal` method (or implicitly via `FitAndSelect`).
/// </para>
/// </remarks>
public class PELTPenaltySelector
{
    /// <summary>
    /// Gets the PELT algorithm instance used for penalty selection.
    /// </summary>
    public IPELTAlgorithm PELTAlgorithm { get; }

    private readonly PELTOptions _peltOptions;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PELTPenaltySelector"/> class.
    /// </summary>
    /// <param name="peltAlgorithm"> The PELT algorithm instance to use. Its internal state (e.g., fitted cost function) will be used.</param>
    /// <exception cref="ArgumentNullException">Thrown if peltAlgorithm, its Options, or its CostFunction is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if MinSize or Jump in peltAlgorithm.Options are invalid.</exception>
    public PELTPenaltySelector(IPELTAlgorithm peltAlgorithm)
    {
        ArgumentNullException.ThrowIfNull(peltAlgorithm, nameof(peltAlgorithm));
        ArgumentNullException.ThrowIfNull(peltAlgorithm.Options, $"{nameof(peltAlgorithm.Options)} cannot be null.");
        ArgumentNullException.ThrowIfNull(peltAlgorithm.Options.CostFunction,
            $"{nameof(peltAlgorithm.Options.CostFunction)} cannot be null.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(peltAlgorithm.Options.MinSize,
            nameof(peltAlgorithm.Options.MinSize));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(peltAlgorithm.Options.Jump,
            nameof(peltAlgorithm.Options.Jump));

        PELTAlgorithm = peltAlgorithm;
        _peltOptions = peltAlgorithm.Options;
        _logger = LoggerProvider.CreateLogger<PELTPenaltySelector>();
    }

    /// <summary>
    /// Fits the internal PELT algorithm to the 1D signal and selects the optimal penalty based on the specified options.
    /// </summary>
    /// <param name="signal">The 1D signal data used to fit the PELT algorithm.</param>
    /// <param name="selectionOptions">Configuration for the penalty selection process (method, range, etc.).</param>
    /// <returns>A <see cref="PELTPenaltySelectionResult"/> containing the selected penalty and corresponding breakpoints.</returns>
    /// <exception cref="ArgumentNullException">Thrown if signal or selectionOptions are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a likelihood-based selection method is requested but the cost function does not support it.</exception>
    /// <exception cref="PELTAlgorithmException">If an error occurs during the selection process or if no suitable penalty is found.</exception>
    public PELTPenaltySelectionResult FitAndSelect(double[] signal, PELTPenaltySelectionOptions selectionOptions)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        _logger.LogInformation("Fitting PELT algorithm to 1D signal of length {Length} for penalty selection.",
            signal.Length);
        PELTAlgorithm.Fit(signal);
        return SelectPenaltyInternal(signal.Length, selectionOptions);
    }

    /// <summary>
    /// Fits the internal PELT algorithm to the multi-dimensional signal matrix and selects the optimal penalty based on the specified options.
    /// </summary>
    /// <param name="signalMatrix">The multi-dimensional signal data (rows=dimensions, cols=time) used to fit the PELT algorithm.</param>
    /// <param name="selectionOptions">Configuration for the penalty selection process (method, range, etc.).</param>
    /// <returns>A <see cref="PELTPenaltySelectionResult"/> containing the selected penalty and corresponding breakpoints.</returns>
    /// <exception cref="ArgumentNullException">Thrown if signalMatrix or selectionOptions are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a likelihood-based selection method is requested but the cost function does not support it.</exception>
    /// <exception cref="PELTAlgorithmException">If an error occurs during the selection process or if no suitable penalty is found.</exception>
    public PELTPenaltySelectionResult FitAndSelect(double[,] signalMatrix, PELTPenaltySelectionOptions selectionOptions)
    {
        ArgumentNullException.ThrowIfNull(signalMatrix, nameof(signalMatrix));
        _logger.LogInformation("Fitting PELT algorithm to {Rows}D signal of length {Length} for penalty selection.",
            signalMatrix.GetLength(0), signalMatrix.GetLength(1));
        PELTAlgorithm.Fit(signalMatrix);
        return SelectPenaltyInternal(signalMatrix.GetLength(1), selectionOptions);
    }

    /// <summary>
    /// Selects the optimal penalty based on the specified options, assuming the PELTAlgorithm has already been fitted.
    /// This is the core penalty selection logic.
    /// </summary>
    /// <param name="signalLength">The length of the signal the PELT algorithm was fitted on.</param>
    /// <param name="selectionOptions">Configuration for the penalty selection process.</param>
    /// <returns>A <see cref="PELTPenaltySelectionResult"/> containing the selected penalty and associated information.</returns>
    /// <exception cref="ArgumentNullException">Thrown if selectionOptions is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a likelihood-based selection method is requested but the cost function does not support it.</exception>
    /// <exception cref="PELTAlgorithmException">If no suitable penalty is found within the tested range and options.</exception>
    private PELTPenaltySelectionResult SelectPenaltyInternal(int signalLength, PELTPenaltySelectionOptions selectionOptions)
    {
        ArgumentNullException.ThrowIfNull(selectionOptions, nameof(selectionOptions));
        _logger.LogInformation("Starting penalty selection using method: {Method}.", selectionOptions.Method);

        var likelihoodNeeded = selectionOptions.Method is PELTPenaltySelectionMethod.AIC or PELTPenaltySelectionMethod.BIC
            or PELTPenaltySelectionMethod.AICc;
        ILikelihoodCostFunction? likelihoodCostFn =
            ValidateAndGetLikelihoodFunction(likelihoodNeeded, selectionOptions.Method);

        var penaltiesToTest = DeterminePenaltiesToTest(signalLength, selectionOptions).ToList();
        if (!penaltiesToTest.Any())
        {
            throw new PELTAlgorithmException(
                "Penalty range resulted in zero penalties to test. Adjust PenaltySelectionOptions.");
        }

        _logger.LogDebug("Testing {Count} penalties in range [{MinPenalty:F2} - {MaxPenalty:F2}].",
            penaltiesToTest.Count, penaltiesToTest.First(), penaltiesToTest.Last());

        var bestScore = double.PositiveInfinity;
        var bestPenalty = -1.0;
        int[] bestBreakpoints = [];
        var diagnostics = new List<(double Penalty, double Score, int ChangePoints)>();

        foreach (var penalty in penaltiesToTest)
        {
            var (success, score, breakpoints) =
                TestSinglePenalty(penalty, signalLength, selectionOptions.Method, likelihoodCostFn);

            diagnostics.Add((penalty, success ? score : double.NaN, success ? breakpoints.Length : -1));

            if (!success || double.IsNaN(score) || double.IsInfinity(score))
            {
                if (success)
                {
                    _logger.LogDebug(
                        "Penalty {Penalty:F4} resulted in unusable score ({Score}). Skipping as candidate.", penalty,
                        score);
                }

                continue;
            }

            var numChangePoints = breakpoints.Length;
            if (score < bestScore)
            {
                _logger.LogDebug(
                    "New best: Penalty={Penalty:F4}, Score={Score:F4}, ChangePoints={NumChangePoints} (Prev Score: {PrevScore:F4})",
                    penalty, score, numChangePoints, bestScore);
                bestScore = score;
                bestPenalty = penalty;
                bestBreakpoints = breakpoints;
            }
            else if (NumericUtils.AreApproximatelyEqual(score, bestScore) && numChangePoints < bestBreakpoints.Length)
            {
                _logger.LogDebug(
                    "Tie-break: Preferring simpler model. Penalty={Penalty:F4}, Score={Score:F4}, ChangePoints={NumChangePoints} (Prev CPs: {PrevCPs})",
                    penalty, score, numChangePoints, bestBreakpoints.Length);
                bestPenalty = penalty;
                bestBreakpoints = breakpoints;
            }
        }

        if (bestPenalty < 0)
        {
            const string message = "Could not find a suitable penalty. All tested penalties resulted in errors, invalid segmentations, or infinite/NaN scores. " +
                                   "Consider adjusting the penalty range (Min/MaxPenalty, NumSteps), checking the cost function, data validity, and PELT MinSize.";
            _logger.LogError(message);
            _logger.LogError(
                "Final Diagnostics Summary: {NumInvalid} penalties resulted in unusable scores or failures out of {TotalTested}.",
                diagnostics.Count(d =>
                    double.IsNaN(d.Score) || double.IsPositiveInfinity(d.Score) || d.ChangePoints == -1),
                diagnostics.Count);

            throw new PELTAlgorithmException(message);
        }

        _logger.LogInformation(
            "Penalty selection complete. Selected Penalty: {SelectedPenalty:F4}, Optimal Score: {OptimalScore:F4}, Optimal Change Points: {NumChangePoints} at [{Breakpoints}]",
            bestPenalty, bestScore, bestBreakpoints.Length, string.Join(",", bestBreakpoints));

        return new PELTPenaltySelectionResult
        {
            SelectedPenalty = bestPenalty,
            OptimalBreakpoints = bestBreakpoints,
            SelectionMethod = selectionOptions.Method,
            Diagnostics = diagnostics.AsReadOnly()
        };
    }

    /// <summary>
    /// Validates that the cost function supports likelihood calculations if required by the selection method.
    /// </summary>
    /// <param name="likelihoodNeeded">Whether the selection method requires likelihood.</param>
    /// <param name="method">The penalty selection method being used (for logging/error messages).</param>
    /// <returns>The cast <see cref="ILikelihoodCostFunction"/> if valid and needed, otherwise null.</returns>
    /// <exception cref="InvalidOperationException">Thrown if likelihood is needed but not supported by the cost function.</exception>
    private ILikelihoodCostFunction? ValidateAndGetLikelihoodFunction(bool likelihoodNeeded,
        PELTPenaltySelectionMethod method)
    {
        if (!likelihoodNeeded) return null;

        if (_peltOptions.CostFunction is ILikelihoodCostFunction { SupportsInformationCriteria: true } castedLc)
        {
            _logger.LogDebug(
                "Likelihood cost function '{CostFunctionName}' available and supports information criteria.",
                castedLc.GetType().Name);
            return castedLc;
        }
        else
        {
            var message = $"Penalty selection method '{method}' requires the configured cost function " +
                          $"to implement ILikelihoodCostFunction and have SupportsInformationCriteria=true. " +
                          $"The provided cost function '{_peltOptions.CostFunction.GetType().Name}' does not meet these requirements.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Tests a single penalty value by running PELT detection and calculating the selection score.
    /// </summary>
    /// <param name="penalty">The penalty value to test.</param>
    /// <param name="signalLength">The length of the signal.</param>
    /// <param name="method">The selection method used (BIC, AIC, etc.).</param>
    /// <param name="likelihoodCostFn">The likelihood cost function instance (nullable).</param>
    /// <returns>
    /// A tuple: (Success boolean, Score double, Breakpoints array).
    /// 'Success' is true if detection and scoring ran without throwing uncaught exceptions.
    /// 'Score' can be <see cref="double.PositiveInfinity"/> or <see cref="double.NaN"/> if the scoring logic determined the result was invalid (e.g., invalid segment, undefined AICc).
    /// 'Breakpoints' contains the detected changepoints for this penalty.
    /// </returns>
    /// <exception cref="PELTAlgorithmException">Wraps unexpected errors during PELT detection.</exception>
    private (bool Success, double Score, int[] Breakpoints) TestSinglePenalty(
        double penalty,
        int signalLength,
        PELTPenaltySelectionMethod method,
        ILikelihoodCostFunction? likelihoodCostFn)
    {
        if (penalty < 0)
        {
            _logger.LogDebug("Skipping negative penalty value: {PenaltyValue}", penalty);
            return (false, double.NaN, []);
        }

        _logger.LogDebug("--- Testing Penalty: {Penalty:F4} ---", penalty);

        int[] currentBreakpoints;
        try
        {
            currentBreakpoints = PELTAlgorithm.Detect(penalty);
            _logger.LogDebug("Penalty {Penalty:F4} -> {NumChangePoints} changepoints: [{Breakpoints}]",
                penalty, currentBreakpoints.Length, string.Join(",", currentBreakpoints));
        }
        catch (Exception ex) when (ex is SegmentLengthException or UninitializedDataException
                                       or ArgumentOutOfRangeException or CostFunctionException
                                       or PELTAlgorithmException)
        {
            _logger.LogWarning(ex, "PELT detection failed for penalty {Penalty:F4}. Skipping.", penalty);
            return (false, double.NaN, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PELT detection for penalty {Penalty:F4}.", penalty);
            throw new PELTAlgorithmException(
                $"Unexpected error during PELT detection for penalty {penalty}. See inner exception.", ex);
        }

        try
        {
            var currentScore = CalculateScore(method, penalty, currentBreakpoints, signalLength, likelihoodCostFn);

            var scoreStr = double.IsNaN(currentScore) ? "NaN" :
                double.IsPositiveInfinity(currentScore) ? "Infinity" :
                currentScore.ToString("F4", CultureInfo.InvariantCulture);
            _logger.LogDebug("Score calculation for Penalty {Penalty:F4} = {Score} (Method: {Method})",
                penalty, scoreStr, method);

            return (true, currentScore, currentBreakpoints);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to calculate score for penalty {Penalty:F4} with {NumChangePoints} changepoints.",
                penalty, currentBreakpoints.Length);
            return (false, double.NaN, currentBreakpoints);
        }
    }

    /// <summary>
    /// Calculates the selection criterion score (BIC, AIC, AICc) for a given segmentation.
    /// </summary>
    /// <param name="method">The selection method (BIC, AIC, AICc).</param>
    /// <param name="penalty">The penalty value used (for logging purposes).</param>
    /// <param name="breakpoints">The detected breakpoints for this penalty.</param>
    /// <param name="signalLength">The total length of the signal.</param>
    /// <param name="likelihoodCostFn">The likelihood cost function instance.</param>
    /// <returns>The calculated score (e.g., BIC value). Returns <see cref="double.PositiveInfinity"/> if the calculation fails or results in an invalid state (e.g., invalid segment, undefined AICc).</returns>
    /// <exception cref="InvalidOperationException">Thrown if the method requires likelihood but the cost function is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an unsupported selection method is provided.</exception>
    private double CalculateScore(
        PELTPenaltySelectionMethod method,
        double penalty,
        int[] breakpoints,
        int signalLength,
        ILikelihoodCostFunction? likelihoodCostFn)
    {
        switch (method)
        {
            case PELTPenaltySelectionMethod.BIC:
            case PELTPenaltySelectionMethod.AIC:
            case PELTPenaltySelectionMethod.AICc:
                if (likelihoodCostFn == null)
                {
                    throw new InvalidOperationException(
                        $"Internal Error: Likelihood function required for {method} but was null.");
                }

                var (success, totalLikelihoodMetric, totalEffectiveParams) =
                    CalculateLikelihoodScoreComponents(breakpoints, signalLength, likelihoodCostFn, penalty);

                if (!success)
                {
                    return double.PositiveInfinity;
                }

                var n = (double)signalLength;
                if (n <= 0)
                {
                    _logger.LogError("Signal length ({SignalLength}) is non-positive. Cannot calculate score.",
                        signalLength);
                    return double.PositiveInfinity;
                }

                if (method == PELTPenaltySelectionMethod.BIC)
                {
                    return totalLikelihoodMetric + totalEffectiveParams * Math.Log(n);
                }

                var aicScore = totalLikelihoodMetric + 2.0 * totalEffectiveParams;
                if (method != PELTPenaltySelectionMethod.AICc)
                {
                    return aicScore;
                }
                    
                var correction = CalculateAiccCorrection(n, totalEffectiveParams, penalty);
                if (double.IsNaN(correction))
                {
                    return double.PositiveInfinity;
                }

                return aicScore + correction;

            default:
                throw new ArgumentOutOfRangeException(nameof(method),
                    $"Unsupported penalty selection method: {method}");
        }
    }

    /// <summary>
    /// Calculates the sum of likelihood metrics and effective parameters for all segments defined by the breakpoints.
    /// </summary>
    /// <param name="breakpoints">The array of breakpoint indices.</param>
    /// <param name="signalLength">The total length of the signal.</param>
    /// <param name="likelihoodCostFn">The likelihood cost function implementation.</param>
    /// <param name="penaltyForLogging">The penalty value being evaluated (used for context in log messages).</param>
    /// <returns>
    /// A tuple: (Success flag, Total Likelihood Metric, Total Effective Parameters).
    /// 'Success' is false if any segment is invalid (length &lt; MinSize) or if likelihood/parameter calculation fails for any segment.
    /// 'LikelihoodMetric' is the sum of <see cref="ILikelihoodCostFunction.ComputeLikelihoodMetric"/> over valid segments.
    /// 'EffectiveParams' includes the sum of model parameters per segment plus K parameters for the changepoint locations.
    /// Returns (<c>false</c>, <see cref="double.PositiveInfinity"/>, <see cref="double.PositiveInfinity"/>) on failure.
    /// </returns>
    /// <exception cref="PELTAlgorithmException">Wraps unexpected errors during cost function calls.</exception>
    private (bool Success, double LikelihoodMetric, double EffectiveParams) CalculateLikelihoodScoreComponents(
        int[] breakpoints,
        int signalLength,
        ILikelihoodCostFunction likelihoodCostFn,
        double penaltyForLogging)
    {
        double totalLikelihoodMetric = 0;
        double totalSegmentParams = 0;
        var K = breakpoints.Length;
        var numSegments = K + 1;
        var lastCp = 0;

        for (var i = 0; i < numSegments; i++)
        {
            var currentCp = (i == K) ? signalLength : breakpoints[i];
            var segmentLength = currentCp - lastCp;

            if (segmentLength < _peltOptions.MinSize)
            {
                _logger.LogWarning(
                    "Penalty {Penalty:F4} resulted in segment [{StartCp}, {EndCp}) with length {SegmentLength} < MinSize {MinSize}. Invalid segmentation.",
                    penaltyForLogging, lastCp, currentCp, segmentLength, _peltOptions.MinSize);
                return (false, double.PositiveInfinity, double.PositiveInfinity);
            }

            try
            {
                var segmentLikelihood = likelihoodCostFn.ComputeLikelihoodMetric(lastCp, currentCp);
                if (double.IsNaN(segmentLikelihood) || double.IsInfinity(segmentLikelihood))
                {
                    _logger.LogWarning(
                        "Likelihood metric is NaN/Infinity for segment [{StartCp}, {EndCp}) with penalty {Penalty:F4}. Invalid.",
                        lastCp, currentCp, penaltyForLogging);
                    return (false, double.PositiveInfinity, double.PositiveInfinity);
                }

                totalLikelihoodMetric += segmentLikelihood;

                totalSegmentParams += likelihoodCostFn.GetSegmentParameterCount(segmentLength);
            }
            catch (Exception ex) when (ex is SegmentLengthException
                                           or CostFunctionException
                                           or ArgumentOutOfRangeException
                                           or UninitializedDataException)
            {
                _logger.LogWarning(ex,
                    "Failed to compute likelihood/params for segment [{StartCp}, {EndCp}) with penalty {Penalty:F4}. Invalid.",
                    lastCp, currentCp, penaltyForLogging);
                return (false, double.PositiveInfinity, double.PositiveInfinity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calculating score components for segment [{StartCp}, {EndCp})",
                    lastCp, currentCp);
                throw new PELTAlgorithmException(
                    $"Unexpected error calculating score components for segment [{lastCp}, {currentCp}). See inner exception.",
                    ex);
            }

            lastCp = currentCp;
        }

        var totalEffectiveParams = totalSegmentParams + K;

        _logger.LogDebug(
            "Likelihood components for Penalty {Penalty:F4}: TotalLikelihoodMetric={Metric:F4}, TotalEffectiveParams={Params} (SegmentParams={SegParams}, K={K})",
            penaltyForLogging, totalLikelihoodMetric, totalEffectiveParams, totalSegmentParams, K);

        if (!double.IsNaN(totalLikelihoodMetric) && !double.IsPositiveInfinity(totalLikelihoodMetric))
        {
            return (true, totalLikelihoodMetric, totalEffectiveParams);
        }

        _logger.LogWarning("Accumulated likelihood metric is NaN/Infinity for penalty {Penalty:F4}. Invalid.",
            penaltyForLogging);

        return (false, double.PositiveInfinity, double.PositiveInfinity);
    }

    /// <summary>
    /// Calculates the AICc correction term: (2 * P * (P + 1)) / (N - P - 1).
    /// </summary>
    /// <param name="n">The total number of data points (signal length).</param>
    /// <param name="totalEffectiveParams">The total number of effective parameters (segment parameters + K changepoints).</param>
    /// <param name="penaltyForLogging">The penalty value being evaluated (for context in log messages).</param>
    /// <returns>The calculated correction term, or <see cref="double.NaN"/> if the correction is undefined or unstable (i.e., if N <= P + 1).</returns>
    private double CalculateAiccCorrection(double n, double totalEffectiveParams, double penaltyForLogging)
    {
        if (n > totalEffectiveParams + 1.0)
        {
            var correction = (2.0 * totalEffectiveParams * (totalEffectiveParams + 1.0)) /
                             (n - totalEffectiveParams - 1.0);

            if (!double.IsNaN(correction) && !double.IsInfinity(correction)) return correction;

            _logger.LogWarning(
                "AICc correction calculation resulted in NaN/Infinity for penalty {Penalty:F4} (N={N}, P={P}). AICc undefined.",
                penaltyForLogging, n, totalEffectiveParams);
        }
        else
        {
            _logger.LogWarning(
                "AICc correction not applicable for penalty {Penalty:F4} because N <= TotalParams + 1 ({N} <= {NumParams} + 1). AICc undefined.",
                penaltyForLogging, n, totalEffectiveParams);
        }

        return double.NaN;
    }

    /// <summary>
    /// Determines the range and set of penalty values to test based on heuristics or provided options.
    /// </summary>
    /// <param name="signalLength">The length of the signal.</param>
    /// <param name="options">The penalty selection options providing potential min/max/steps.</param>
    /// <returns>An enumerable sequence of penalty values to test.</returns>
    private IEnumerable<double> DeterminePenaltiesToTest(int signalLength, PELTPenaltySelectionOptions options)
    {
        var minP = options.MinPenalty ?? EstimateMinPenaltyHeuristic(signalLength);
        var maxP = options.MaxPenalty ?? EstimateMaxPenaltyHeuristic(signalLength, minP);

        if (minP < 0) minP = 0;
        if (maxP <= minP)
        {
            _logger.LogWarning(
                "MaxPenalty ({MaxP:F2}) must be greater than MinPenalty ({MinP:F2}). Adjusting MaxPenalty slightly.",
                maxP, minP);
            maxP = minP + Math.Max(1.0, Math.Abs(minP * 0.1) + 0.1);
        }

        var steps = Math.Max(2, options.NumPenaltySteps);

        _logger.LogDebug("Generating {NumSteps} penalties in range [{MinPenalty:F2}, {MaxPenalty:F2}]", steps, minP,
            maxP);

        return GenerateLogSpacedPenalties(minP, maxP, steps);
    }

    /// <summary>
    /// Generates logarithmically spaced penalty values between a minimum and maximum.
    /// Handles the special case where the minimum penalty is zero.
    /// </summary>
    /// <param name="minPenalty">The minimum penalty value (non-negative).</param>
    /// <param name="maxPenalty">The maximum penalty value (must be > minPenalty unless count=1).</param>
    /// <param name="count">The desired number of penalty values (must be >= 1).</param>
    /// <returns>An enumerable sequence of log-spaced penalty values.</returns>
    private static IEnumerable<double> GenerateLogSpacedPenalties(double minPenalty, double maxPenalty, int count)
    {
        if (count <= 0) yield break;
        if (count == 1)
        {
            yield return minPenalty;
            yield break;
        }

        if (minPenalty >= maxPenalty)
        {
            yield return minPenalty;
            yield break;
        }

        if (NumericUtils.IsEffectivelyZero(minPenalty))
        {
            yield return 0.0;
            if (count == 1) yield break;

            var effectiveMin = Math.Max(1e-9, maxPenalty * 1e-6);
            if (effectiveMin >= maxPenalty)
            {
                if (!NumericUtils.IsEffectivelyZero(maxPenalty)) yield return maxPenalty;
                yield break;
            }

            var logMin = Math.Log(effectiveMin);
            var logMax = Math.Log(maxPenalty);

            if (count == 2 || logMin >= logMax)
            {
                yield return maxPenalty;
                yield break;
            }

            var stepSize = (logMax - logMin) / (count - 2);

            for (var i = 0; i < count - 1; i++)
            {
                var logPenalty = logMin + stepSize * i;
                var penalty = Math.Exp(logPenalty);

                if (i == count - 2 || penalty >= maxPenalty)
                {
                    yield return maxPenalty;
                    yield break;
                }

                yield return penalty;
            }
        }
        else
        {
            var logMin = Math.Log(minPenalty);
            var logMax = Math.Log(maxPenalty);
            var stepSize = (logMax - logMin) / (count - 1);

            for (var i = 0; i < count; i++)
            {
                var logPenalty = logMin + stepSize * i;
                var penalty = Math.Exp(logPenalty);

                if (i == count - 1 || penalty >= maxPenalty)
                {
                    yield return maxPenalty;
                    yield break;
                }

                yield return penalty;
            }
        }
    }

    /// <summary>
    /// Estimates a reasonable minimum penalty based on signal length and cost function parameters using a BIC-like heuristic.
    /// </summary>
    /// <param name="signalLength">The length of the signal.</param>
    /// <returns>An estimated minimum penalty value (non-negative).</returns>
    private double EstimateMinPenaltyHeuristic(int signalLength)
    {
        var n = (double)Math.Max(2, signalLength);
        var typicalParams = 2.0;

        if (_peltOptions.CostFunction is ILikelihoodCostFunction { SupportsInformationCriteria: true } lc)
        {
            try
            {
                var sampleSegmentLength = Math.Max(_peltOptions.MinSize, Math.Min(signalLength, 10));
                if (signalLength >= sampleSegmentLength)
                {
                    typicalParams = Math.Max(1.0, lc.GetSegmentParameterCount(sampleSegmentLength));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to get parameter count from cost function for min penalty heuristic. Using default {DefaultParams}.",
                    typicalParams);
            }
        }

        var minPenalty = typicalParams * Math.Log(n);
        return Math.Max(0.1, minPenalty);
    }

    /// <summary>
    /// Estimates a reasonable maximum penalty, likely large enough to prevent any splits.
    /// </summary>
    /// <param name="signalLength">The length of the signal.</param>
    /// <param name="minPenaltyEstimate">The estimated minimum penalty (used as a baseline).</param>
    /// <returns>An estimated maximum penalty value.</returns>
    private static double EstimateMaxPenaltyHeuristic(int signalLength, double minPenaltyEstimate)
    {
        const double scaleFactor = 20.0;

        var n = (double)Math.Max(2, signalLength);
        var maxPenaltyFromN = n * Math.Log(n);
        var maxPenaltyFromMin = minPenaltyEstimate * scaleFactor;

        var maxPenalty = Math.Max(maxPenaltyFromN, maxPenaltyFromMin);
        return Math.Max(maxPenalty, Math.Max(1.0, minPenaltyEstimate * 1.1 + 1.0));
    }
}