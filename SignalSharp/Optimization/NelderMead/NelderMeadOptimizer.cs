using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SignalSharp.Utilities;

namespace SignalSharp.Optimization.NelderMead;

/// <summary>
/// Implements the Nelder-Mead simplex optimization algorithm.
/// It is a direct search method suitable for non-differentiable objective functions and
/// does not use gradient information. This implementation handles parameter bounds by clamping.
/// </summary>
/// <typeparam name="TInput">The type of the input data provided to the objective function.</typeparam>
/// <typeparam name="TMetric">The type of the objective metric (e.g., SSE), which must be a floating-point type.</typeparam>
public class NelderMeadOptimizer<TInput, TMetric>(
    NelderMeadOptimizerOptions? options = null,
    ILogger<NelderMeadOptimizer<TInput, TMetric>>? logger = null,
    Random? random = null
) : IParameterOptimizer<TInput, TMetric>
    where TMetric : IFloatingPointIeee754<TMetric>
{
    private readonly NelderMeadOptimizerOptions _options = options ?? new NelderMeadOptimizerOptions();
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;

    private int _functionEvaluations;
    private readonly Random _random = random ?? new Random();
    private readonly Dictionary<string, double> _parameterPool = new();

    public OptimizationResult<TMetric> Optimize(
        TInput inputData,
        IEnumerable<ParameterDefinition> parametersToOptimize,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction
    )
    {
        _logger.LogInformation("Starting Nelder-Mead optimization...");
        _functionEvaluations = 0;

        var optimizableParamsList = parametersToOptimize.ToList();
        int dimensionCount = optimizableParamsList.Count;

        if (dimensionCount == 0)
        {
            _logger.LogWarning("No parameters provided for optimization.");

            return new OptimizationResult<TMetric>(
                new Dictionary<string, double>(),
                TMetric.NaN,
                false,
                "No parameters to optimize.",
                Iterations: 0,
                FunctionEvaluations: 0
            );
        }

        var paramNames = optimizableParamsList.Select(p => p.Name).ToList();

        return RunMultiStartOptimization(inputData, optimizableParamsList, paramNames, objectiveFunction, CancellationToken.None);
    }

    public Task<OptimizationResult<TMetric>> OptimizeAsync(
        TInput inputData,
        IEnumerable<ParameterDefinition> parametersToOptimize,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Starting Nelder-Mead optimization...");
        _functionEvaluations = 0;

        var optimizableParamsList = parametersToOptimize.ToList();
        int dimensionCount = optimizableParamsList.Count;

        if (dimensionCount == 0)
        {
            _logger.LogWarning("No parameters provided for optimization.");

            return Task.FromResult(
                new OptimizationResult<TMetric>(
                    new Dictionary<string, double>(),
                    TMetric.NaN,
                    false,
                    "No parameters to optimize.",
                    Iterations: 0,
                    FunctionEvaluations: 0
                )
            );
        }

        var paramNames = optimizableParamsList.Select(p => p.Name).ToList();

        return Task.FromResult(RunMultiStartOptimization(inputData, optimizableParamsList, paramNames, objectiveFunction, cancellationToken));
    }

    private OptimizationResult<TMetric> RunMultiStartOptimization(
        TInput inputData,
        IReadOnlyList<ParameterDefinition> paramDefinitions,
        IReadOnlyList<string> paramNames,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        CancellationToken cancellationToken
    )
    {
        int maxRestarts = _options.EnableMultiStart ? _options.MaxRestarts : 0;
        TMetric bestMetricAcrossStarts = TMetric.PositiveInfinity;
        IReadOnlyDictionary<string, double>? bestParamsAcrossStarts = null;
        int totalIterations = 0;
        bool overallSuccess = false;

        for (int restartIdx = 0; restartIdx <= maxRestarts; restartIdx++)
        {
            var initialGuess = GenerateInitialGuess(paramDefinitions, restartIdx > 0);

            _logger.LogInformation(
                "Optimization run {CurrentRun}/{TotalRuns}. " + "Initial guess: [{InitialGuess}]",
                restartIdx + 1,
                maxRestarts + 1,
                string.Join(", ", initialGuess.Select((v, i) => $"{paramNames[i]}={v:F4}"))
            );

            var (result, iterations, stagnationDetected) = RunSingleOptimization(
                inputData,
                paramDefinitions,
                paramNames,
                objectiveFunction,
                initialGuess,
                cancellationToken
            );

            totalIterations += iterations;

            if (result.Success)
            {
                overallSuccess = true;

                if (result.MinimizedMetric < bestMetricAcrossStarts)
                {
                    bestMetricAcrossStarts = result.MinimizedMetric;
                    bestParamsAcrossStarts = result.BestParameters;

                    _logger.LogInformation(
                        "New best solution found on run {RunIndex}. Metric: {Metric:G6}",
                        restartIdx + 1,
                        Convert.ToDouble(bestMetricAcrossStarts)
                    );
                }
            }

            // if stagnation was detected, or we hit max iterations, attempt a new start (if enabled)
            // also restart if convergence was declared very early, which can indicate premature convergence (e.g., on a flat objective function)
            bool prematureConvergence = result.Success && iterations < _options.StagnationThresholdCount;

            if (restartIdx < maxRestarts && (stagnationDetected || iterations >= _options.MaxIterations || prematureConvergence))
            {
                _logger.LogInformation("Attempting a new start ({NextStart}/{MaxStarts}).", restartIdx + 2, maxRestarts + 1);
                continue;
            }

            // if we found a good solution or exhausted all restart attempts, we're done
            if (result.Success || restartIdx >= maxRestarts)
            {
                break;
            }
        }

        return CreateFinalResult(bestParamsAcrossStarts, bestMetricAcrossStarts, paramDefinitions, totalIterations, maxRestarts, overallSuccess);
    }

    private (OptimizationResult<TMetric> Result, int Iterations, bool StagnationDetected) RunSingleOptimization(
        TInput inputData,
        IReadOnlyList<ParameterDefinition> paramDefinitions,
        IReadOnlyList<string> paramNames,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        double[] initialGuess,
        CancellationToken cancellationToken
    )
    {
        var simplex = InitializeSimplex(inputData, paramDefinitions, paramNames, objectiveFunction, initialGuess, cancellationToken);

        int dimensionCount = paramDefinitions.Count;

        if (simplex.Count != dimensionCount + 1)
        {
            _logger.LogError("Failed to initialize simplex properly. Expected {ExpectedCount} vertices, got {ActualCount}.", dimensionCount + 1, simplex.Count);

            return (
                new OptimizationResult<TMetric>(
                    new Dictionary<string, double>(),
                    TMetric.NaN,
                    false,
                    "Failed to initialize simplex.",
                    FunctionEvaluations: _functionEvaluations
                ),
                0,
                false
            );
        }

        if (IsSimplexDegenerate(simplex))
        {
            _logger.LogWarning("Initial simplex is degenerate. Using alternative initialization.");
            simplex = InitializeSimplexWithRandomPerturbationsAsync(inputData, paramDefinitions, paramNames, objectiveFunction, cancellationToken);

            if (IsSimplexDegenerate(simplex))
            {
                _logger.LogWarning("Simplex remains degenerate after randomization attempt.");
            }
        }

        // main optimization loop
        int iterations = 0;
        bool stagnationDetected = false;
        TMetric previousBestValue = TMetric.PositiveInfinity;
        int stagnationCounter = 0;

        for (iterations = 0; iterations < _options.MaxIterations; iterations++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            simplex.Sort(); // sort from best to worst

            var bestVertex = simplex[0];
            var worstVertex = simplex[dimensionCount];
            var secondWorstVertex = simplex[dimensionCount - 1];

            if (HasConverged(simplex, iterations))
            {
                _logger.LogInformation("Convergence criteria met after {Iterations} iterations.", iterations);
                break;
            }

            // allow at least one iteration to attempt to move from a flat simplex
            if (iterations > 0 && IsSimplexFlat(simplex, TMetric.CreateChecked(_options.FunctionValueConvergenceTolerance)))
            {
                _logger.LogInformation("All vertices have essentially the same value. Optimization complete.");
                break;
            }

            // stagnation detection
            (stagnationDetected, stagnationCounter) = CheckStagnation(bestVertex.Value, previousBestValue, stagnationCounter);

            if (stagnationDetected)
            {
                _logger.LogInformation("Optimization appears to be stagnating after {Iterations} iterations.", iterations);
                break;
            }

            previousBestValue = bestVertex.Value;

            _logger.LogTrace(
                "Iteration {Iteration}: Best Value={BestVal:G6} (Params: {BestParams}), Worst Value={WorstVal:G6}",
                iterations,
                Convert.ToDouble(bestVertex.Value),
                FormatParams(bestVertex.ParametersDict),
                Convert.ToDouble(worstVertex.Value)
            );

            // get adaptive parameters based on iteration number and dimension count
            var (reflectionFactor, expansionFactor, contractionFactor, shrinkFactor) = GetAlgorithmParameters(iterations, dimensionCount);

            // calculate centroid of the N best points (all except worstVertex)
            var centroidParamsArray = ComputeCentroid(simplex, dimensionCount);

            // reflection step
            var reflectedVertex = PerformReflection(
                inputData,
                paramNames,
                paramDefinitions,
                objectiveFunction,
                centroidParamsArray,
                worstVertex,
                reflectionFactor,
                cancellationToken
            );

            _logger.LogTrace("Reflection point evaluated. Metric: {Metric:G6}", Convert.ToDouble(reflectedVertex.Value));

            if (reflectedVertex.Value < bestVertex.Value)
            {
                // reflected point is better than the current best, let's try expansion
                HandleReflectionBetterThanBest(
                    simplex,
                    inputData,
                    paramNames,
                    paramDefinitions,
                    objectiveFunction,
                    centroidParamsArray,
                    reflectedVertex,
                    expansionFactor,
                    dimensionCount,
                    cancellationToken
                );
            }
            else if (reflectedVertex.Value < secondWorstVertex.Value)
            {
                // reflected point is better than the second worst - accept reflection
                simplex[dimensionCount] = reflectedVertex;
                _logger.LogTrace("Accepted reflected point.");
            }
            else
            {
                // reflected point is not better than second worst - try contraction
                HandleContraction(
                    simplex,
                    inputData,
                    paramNames,
                    paramDefinitions,
                    objectiveFunction,
                    centroidParamsArray,
                    reflectedVertex,
                    worstVertex,
                    bestVertex,
                    contractionFactor,
                    shrinkFactor,
                    dimensionCount,
                    cancellationToken
                );
            }
        }

        simplex.Sort();
        var finalBestVertex = simplex[0];

        bool success = !TMetric.IsNaN(finalBestVertex.Value) && !TMetric.IsPositiveInfinity(finalBestVertex.Value);

        return (
            new OptimizationResult<TMetric>(
                finalBestVertex.ParametersDict,
                finalBestVertex.Value,
                success,
                "Single optimization run completed.",
                Iterations: iterations,
                FunctionEvaluations: _functionEvaluations
            ),
            iterations,
            stagnationDetected
        );
    }

    private bool ShouldStopForMaxFunctionEvaluations()
    {
        if (_options.MaxFunctionEvaluations.HasValue && _functionEvaluations >= _options.MaxFunctionEvaluations.Value)
        {
            _logger.LogInformation("Reached maximum function evaluations ({MaxEvaluations}). Stopping.", _options.MaxFunctionEvaluations.Value);
            return true;
        }

        return false;
    }

    private (bool Detected, int Counter) CheckStagnation(TMetric currentBestValue, TMetric previousBestValue, int currentStagnationCounter)
    {
        if (TMetric.Abs(currentBestValue - previousBestValue) < TMetric.CreateChecked(_options.StagnationImprovementThreshold))
        {
            currentStagnationCounter++;
            if (currentStagnationCounter >= _options.StagnationThresholdCount)
            {
                return (true, currentStagnationCounter);
            }
        }
        else
        {
            currentStagnationCounter = 0;
        }

        return (false, currentStagnationCounter);
    }

    private void HandleReflectionBetterThanBest(
        List<SimplexVertex<TMetric>> simplex,
        TInput inputData,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> paramDefinitions,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        double[] centroidParamsArray,
        SimplexVertex<TMetric> reflectedVertex,
        double expansionFactor,
        int dimensionCount,
        CancellationToken cancellationToken
    )
    {
        var expandedParamsArray = LinearCombination(centroidParamsArray, reflectedVertex.ParametersArray, 1.0 - expansionFactor, expansionFactor);

        var expandedVertex = EvaluateNewVertex(expandedParamsArray, inputData, paramNames, paramDefinitions, objectiveFunction, cancellationToken);

        _logger.LogTrace("Expansion point evaluated. Metric: {Metric:G6}", Convert.ToDouble(expandedVertex.Value));

        if (expandedVertex.Value < reflectedVertex.Value)
        {
            simplex[dimensionCount] = expandedVertex; // accept expanded
            _logger.LogTrace("Accepted expanded point.");
        }
        else
        {
            simplex[dimensionCount] = reflectedVertex; // accept reflected (expansion didn't improve)
            _logger.LogTrace("Accepted reflected point (expansion was not better).");
        }
    }

    private void HandleContraction(
        List<SimplexVertex<TMetric>> simplex,
        TInput inputData,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> paramDefinitions,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        double[] centroidParamsArray,
        SimplexVertex<TMetric> reflectedVertex,
        SimplexVertex<TMetric> worstVertex,
        SimplexVertex<TMetric> bestVertex,
        double contractionFactor,
        double shrinkFactor,
        int dimensionCount,
        CancellationToken cancellationToken
    )
    {
        SimplexVertex<TMetric> contractedVertex;

        if (reflectedVertex.Value < worstVertex.Value)
        {
            var outsideContractedParamsArray = LinearCombination(
                centroidParamsArray,
                reflectedVertex.ParametersArray,
                1.0 - contractionFactor,
                contractionFactor
            );

            contractedVertex = EvaluateNewVertex(outsideContractedParamsArray, inputData, paramNames, paramDefinitions, objectiveFunction, cancellationToken);

            _logger.LogTrace("Outside contraction point evaluated. Metric: {Metric:G6}", Convert.ToDouble(contractedVertex.Value));

            if (contractedVertex.Value <= reflectedVertex.Value)
            {
                simplex[dimensionCount] = contractedVertex;
                _logger.LogTrace("Accepted outside contracted point.");
            }
            else
            {
                _logger.LogTrace("Outside contraction failed. Performing shrink.");

                PerformShrink(simplex, bestVertex, inputData, paramNames, paramDefinitions, objectiveFunction, shrinkFactor, cancellationToken);
            }
        }
        else
        {
            var insideContractedParamsArray = LinearCombination(centroidParamsArray, worstVertex.ParametersArray, 1.0 - contractionFactor, contractionFactor);

            contractedVertex = EvaluateNewVertex(insideContractedParamsArray, inputData, paramNames, paramDefinitions, objectiveFunction, cancellationToken);

            _logger.LogTrace("Inside contraction point evaluated. Metric: {Metric:G6}", Convert.ToDouble(contractedVertex.Value));

            if (contractedVertex.Value < worstVertex.Value)
            {
                simplex[dimensionCount] = contractedVertex;
                _logger.LogTrace("Accepted inside contracted point.");
            }
            else
            {
                _logger.LogTrace("Inside contraction failed. Performing shrink.");

                PerformShrink(simplex, bestVertex, inputData, paramNames, paramDefinitions, objectiveFunction, shrinkFactor, cancellationToken);
            }
        }
    }

    private SimplexVertex<TMetric> PerformReflection(
        TInput inputData,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> paramDefinitions,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        double[] centroidParamsArray,
        SimplexVertex<TMetric> worstVertex,
        double reflectionFactor,
        CancellationToken cancellationToken
    )
    {
        // reflection: xr = xo + rho * (xo - x_worst) = (1 + rho) * xo - rho * x_worst
        var reflectedParamsArray = LinearCombination(centroidParamsArray, worstVertex.ParametersArray, 1.0 + reflectionFactor, -reflectionFactor);

        return EvaluateNewVertex(reflectedParamsArray, inputData, paramNames, paramDefinitions, objectiveFunction, cancellationToken);
    }

    private OptimizationResult<TMetric> CreateFinalResult(
        IReadOnlyDictionary<string, double>? bestParamsAcrossStarts,
        TMetric bestMetricAcrossStarts,
        IReadOnlyList<ParameterDefinition> paramDefinitions,
        int totalIterations,
        int maxRestarts,
        bool overallSuccess
    )
    {
        string message = DetermineResultMessage(totalIterations, maxRestarts);

        var finalParams = bestParamsAcrossStarts ?? new Dictionary<string, double>();
        var finalMetric = bestParamsAcrossStarts != null ? bestMetricAcrossStarts : TMetric.PositiveInfinity;

        var boundaryAnalysis = AnalyzeBoundaryProximity(finalParams, paramDefinitions);
        if (boundaryAnalysis.Count != 0)
        {
            message += " Warning: The following parameters are at or near their bounds: " + string.Join(", ", boundaryAnalysis);
        }

        _logger.LogInformation(
            "Nelder-Mead optimization finished. Iterations: {Iterations}, Evals: {Evals}. " + "Best Metric: {Metric:G6}. Message: {Msg}",
            totalIterations,
            _functionEvaluations,
            Convert.ToDouble(finalMetric),
            message
        );

        if (bestParamsAcrossStarts != null)
        {
            _logger.LogDebug("Best parameters: [{Params}]", FormatParams(finalParams));
        }

        return new OptimizationResult<TMetric>(
            finalParams,
            finalMetric,
            overallSuccess,
            message,
            Iterations: totalIterations,
            FunctionEvaluations: _functionEvaluations
        );
    }

    private string DetermineResultMessage(int totalIterations, int maxRestarts)
    {
        if (_options.MaxFunctionEvaluations.HasValue && _functionEvaluations >= _options.MaxFunctionEvaluations.Value)
        {
            return "Reached maximum function evaluations.";
        }

        if (totalIterations >= _options.MaxIterations * (maxRestarts + 1))
        {
            return "Reached maximum iterations.";
        }

        return "Converged successfully.";
    }

    private double[] GenerateInitialGuess(IReadOnlyList<ParameterDefinition> paramDefs, bool useRandomization)
    {
        int n = paramDefs.Count;
        var initialGuessArray = new double[n];

        for (int i = 0; i < n; i++)
        {
            var pDef = paramDefs[i];

            if (useRandomization)
            {
                // random value within bounds for restarts
                double range = pDef.MaxValue - pDef.MinValue;
                initialGuessArray[i] = pDef.MinValue + _random.NextDouble() * range;
            }
            else
            {
                // use initial guess if provided, otherwise use midpoint
                initialGuessArray[i] = pDef.InitialGuess ?? (pDef.MinValue + pDef.MaxValue) / 2.0;
            }
        }

        return initialGuessArray;
    }

    private static bool IsSimplexDegenerate(List<SimplexVertex<TMetric>> simplex, double degeneracyThreshold = 1e-10)
    {
        if (simplex.Count < 2)
        {
            return false;
        }

        if (simplex.Any(t => TMetric.IsNaN(t.Value) || TMetric.IsInfinity(t.Value)))
        {
            return true;
        }

        int n = simplex[0].ParametersArray.Length;

        // check if any two vertices are too close
        for (int i = 0; i < simplex.Count; i++)
        {
            for (int j = i + 1; j < simplex.Count; j++)
            {
                double distanceSquared = 0;
                for (int k = 0; k < n; k++)
                {
                    double diff = simplex[i].ParametersArray[k] - simplex[j].ParametersArray[k];
                    distanceSquared += diff * diff;
                }

                if (distanceSquared < degeneracyThreshold * degeneracyThreshold)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSimplexFlat(List<SimplexVertex<TMetric>> simplex, TMetric tolerance)
    {
        if (simplex.Count <= 1)
            return true;

        var firstValue = simplex[0].Value;
        for (int i = 1; i < simplex.Count; i++)
        {
            if (TMetric.Abs(simplex[i].Value - firstValue) > tolerance)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasConverged(List<SimplexVertex<TMetric>> simplex, int iterations)
    {
        var bestVertex = simplex[0];
        var worstVertex = simplex[^1];

        // if the best point is NaN, all points are NaN. We're stuck.
        if (TMetric.IsNaN(bestVertex.Value))
        {
            return true;
        }

        // if the worst point is NaN (but best is not), we have not converged; let the algorithm try to replace it
        if (TMetric.IsNaN(worstVertex.Value))
        {
            return false;
        }

        // value-based convergence
        TMetric valueDifference = worstVertex.Value - bestVertex.Value;
        if (valueDifference < TMetric.Zero)
        {
            _logger.LogWarning("Convergence check encountered invalid function value range: {Range:G6}", Convert.ToDouble(valueDifference));
            return true; // consider converged if we can't make a proper comparison
        }

        // don't declare convergence on the first iteration just because the simplex is flat
        if (iterations > 0 && valueDifference < TMetric.CreateChecked(_options.FunctionValueConvergenceTolerance))
        {
            _logger.LogInformation(
                "Converged: Function value range ({Range:G6}) is below tolerance ({Tolerance:G6}).",
                Convert.ToDouble(valueDifference),
                _options.FunctionValueConvergenceTolerance
            );
            return true;
        }

        // parameter-based convergence - only check after a few iterations
        if (iterations > 5 && _options.EnableParameterConvergence)
        {
            int n = simplex[0].ParametersArray.Length;
            double maxParameterDifference = 0;

            for (int i = 0; i < n; i++)
            {
                double minValue = double.MaxValue;
                double maxValue = double.MinValue;

                foreach (var vertex in simplex)
                {
                    minValue = Math.Min(minValue, vertex.ParametersArray[i]);
                    maxValue = Math.Max(maxValue, vertex.ParametersArray[i]);
                }

                double normalizedDiff = (maxValue - minValue) / Math.Max(1e-10, Math.Abs(simplex[0].ParametersArray[i]));
                maxParameterDifference = Math.Max(maxParameterDifference, normalizedDiff);
            }

            if (maxParameterDifference < _options.ParameterConvergenceTolerance)
            {
                _logger.LogInformation(
                    "Converged based on parameter tolerance: {Diff:G6} < {Tolerance:G6}",
                    maxParameterDifference,
                    _options.ParameterConvergenceTolerance
                );
                return true;
            }
        }

        return false;
    }

    private (double reflectionFactor, double expansionFactor, double contractionFactor, double shrinkFactor) GetAlgorithmParameters(
        int iteration,
        int dimensionCount
    )
    {
        if (!_options.EnableAdaptiveParameters)
        {
            return (_options.ReflectionFactor, _options.ExpansionFactor, _options.ContractionFactor, _options.ShrinkFactor);
        }

        double reflectionFactor = _options.ReflectionFactor;
        double expansionFactor = _options.ExpansionFactor;
        double contractionFactor = _options.ContractionFactor;
        double shrinkFactor = _options.ShrinkFactor;

        // in higher dimensions, more aggressive reflection and expansion can help early on
        if (dimensionCount > 5 && iteration < 20)
        {
            reflectionFactor *= 1.1;
            expansionFactor *= 1.2;
        }

        // in later iterations, more conservative steps can help with convergence
        if (iteration > 50)
        {
            reflectionFactor *= 0.95;
            expansionFactor *= 0.9;
            contractionFactor *= 1.05;
            // keep shrink factor the same, as modifying it can lead to instability
        }

        expansionFactor = Math.Max(expansionFactor, reflectionFactor + 0.1); // ensure expansion > reflection
        contractionFactor = Math.Min(Math.Max(contractionFactor, 0.1), 0.9); // keep in (0,1) range

        return (reflectionFactor, expansionFactor, contractionFactor, shrinkFactor);
    }

    private List<SimplexVertex<TMetric>> InitializeSimplex(
        TInput inputData,
        IReadOnlyList<ParameterDefinition> paramDefs,
        IReadOnlyList<string> paramNames,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        double[] initialGuessArray,
        CancellationToken cancellationToken
    )
    {
        int parameterCount = paramDefs.Count;
        var simplex = new List<SimplexVertex<TMetric>>(parameterCount + 1);

        _logger.LogDebug("Initializing simplex with initial guess: [{InitialParams}]", FormatParamsArray(initialGuessArray, paramNames));

        simplex.Add(EvaluateNewVertex(initialGuessArray, inputData, paramNames, paramDefs, objectiveFunction, cancellationToken));

        for (int i = 0; i < parameterCount; i++)
        {
            var perturbedVertexArray = (double[])initialGuessArray.Clone();

            double perturbation = CalculatePerturbation(paramDefs[i]);

            perturbedVertexArray[i] += perturbation;

            var (clampedAfterPositivePerturb, _) = ConvertAndClampParameters(perturbedVertexArray, paramNames, paramDefs);

            // if the clamped value is essentially the same as the clamped initial guess for this dimension,
            // it means the positive perturbation likely hit a boundary or was too small - try negative
            if (Math.Abs(clampedAfterPositivePerturb[i] - simplex[0].ParametersArray[i]) < 1e-9 * Math.Max(1.0, Math.Abs(simplex[0].ParametersArray[i])))
            {
                _logger.LogTrace(
                    "Initial simplex perturbation for param '{ParamName}' with positive step was ineffective (clamped to base or boundary). Trying negative step.",
                    paramNames[i]
                );
                perturbedVertexArray[i] = initialGuessArray[i] - perturbation; // perturb from original initial guess in opposite direction

                if (Math.Abs(perturbedVertexArray[i] - initialGuessArray[i]) < 1e-9)
                {
                    // let's try a different dimension or random perturbation
                    _logger.LogTrace("Neither positive nor negative perturbation was effective for param '{ParamName}'. Using random approach.", paramNames[i]);
                    perturbedVertexArray[i] =
                        initialGuessArray[i] + (_random.NextDouble() * 2 - 1) * Math.Abs(paramDefs[i].MaxValue - paramDefs[i].MinValue) * 0.25;
                }
            }

            simplex.Add(EvaluateNewVertex(perturbedVertexArray, inputData, paramNames, paramDefs, objectiveFunction, cancellationToken));
        }

        _logger.LogInformation(
            "Initial simplex generated with {NumVertices} vertices. Total function evaluations so far: {Evals}",
            simplex.Count,
            _functionEvaluations
        );

        foreach (var vtx in simplex)
        {
            _logger.LogDebug("Simplex vertex: Value={Metric:G6}, Params=[{Params}]", Convert.ToDouble(vtx.Value), FormatParams(vtx.ParametersDict));
        }

        return simplex;
    }

    private double CalculatePerturbation(ParameterDefinition paramDef)
    {
        double paramRange = paramDef.MaxValue - paramDef.MinValue;
        double perturbation;

        if (paramRange > 1e-9)
        {
            double baseValue = Math.Abs(paramDef.InitialGuess ?? (paramDef.MinValue + paramDef.MaxValue) / 2.0);
            perturbation = Math.Max(baseValue * 0.1, paramRange * _options.InitialSimplexRangeFactor);

            perturbation = Math.Min(perturbation, paramRange * 0.5);
        }
        else
        {
            perturbation = _options.InitialSimplexAbsoluteStepForZeroRange;
        }

        return perturbation;
    }

    private List<SimplexVertex<TMetric>> InitializeSimplexWithRandomPerturbationsAsync(
        TInput inputData,
        IReadOnlyList<ParameterDefinition> paramDefs,
        IReadOnlyList<string> paramNames,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        CancellationToken cancellationToken
    )
    {
        int n = paramDefs.Count;
        var simplex = new List<SimplexVertex<TMetric>>(n + 1);

        var baseParams = new double[n];
        for (int i = 0; i < n; i++)
        {
            baseParams[i] = (paramDefs[i].MinValue + paramDefs[i].MaxValue) / 2;
        }

        simplex.Add(EvaluateNewVertex(baseParams, inputData, paramNames, paramDefs, objectiveFunction, cancellationToken));

        for (int i = 0; i < n; i++)
        {
            var randomParams = new double[n];
            for (int j = 0; j < n; j++)
            {
                // generate random point within the bounds, but biased away from boundaries
                double range = paramDefs[j].MaxValue - paramDefs[j].MinValue;
                double buffer = range * 0.1; // 10% buffer from boundaries
                randomParams[j] = paramDefs[j].MinValue + buffer + _random.NextDouble() * (range - 2 * buffer);
            }

            simplex.Add(EvaluateNewVertex(randomParams, inputData, paramNames, paramDefs, objectiveFunction, cancellationToken));
        }

        _logger.LogInformation("Generated alternative simplex with random perturbations. Total function evaluations: {Evaluations}", _functionEvaluations);

        return simplex;
    }

    private SimplexVertex<TMetric> EvaluateNewVertex(
        double[] trialParamsArray,
        TInput inputData,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> paramDefs,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldStopForMaxFunctionEvaluations())
        {
            var (stoppedParamsArray, stoppedParamsDict) = ConvertAndClampParameters(trialParamsArray, paramNames, paramDefs);
            return new SimplexVertex<TMetric>(stoppedParamsArray, stoppedParamsDict, TMetric.PositiveInfinity);
        }

        var (clampedParamsArray, paramsDict) = ConvertAndClampParameters(trialParamsArray, paramNames, paramDefs);

        TMetric metricValue;
        try
        {
            var evalResult = objectiveFunction(inputData, paramsDict);
            metricValue = evalResult.MetricValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Objective function threw an exception for parameters: [{Params}]. Assigning PositiveInfinity.", FormatParams(paramsDict));
            metricValue = TMetric.PositiveInfinity;
        }

        Interlocked.Increment(ref _functionEvaluations);

        return new SimplexVertex<TMetric>(clampedParamsArray, paramsDict, metricValue);
    }

    private (double[] ClampedParamsArray, IReadOnlyDictionary<string, double> ParametersDict) ConvertAndClampParameters(
        double[] paramsArray,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> paramDefs
    )
    {
        _parameterPool.Clear();

        var clampedArray = new double[paramsArray.Length];

        for (int i = 0; i < paramsArray.Length; i++)
        {
            clampedArray[i] = Math.Clamp(paramsArray[i], paramDefs[i].MinValue, paramDefs[i].MaxValue);
            _parameterPool[paramNames[i]] = clampedArray[i];
        }

        return (clampedArray, _parameterPool);
    }

    private static double[] ComputeCentroid(List<SimplexVertex<TMetric>> simplex, int dimensionCount)
    {
        var centroidParamsArray = new double[dimensionCount];

        // summing dimensionCount best points (simplex[0] to simplex[dimensionCount-1])
        // the simplex has dimensionCount + 1 points, we exclude the worst point (simplex[dimensionCount])
        for (int i = 0; i < dimensionCount; i++)
        {
            for (int j = 0; j < dimensionCount; j++)
            {
                centroidParamsArray[j] += simplex[i].ParametersArray[j];
            }
        }

        for (int j = 0; j < dimensionCount; j++)
        {
            centroidParamsArray[j] /= dimensionCount;
        }

        return centroidParamsArray;
    }

    private static double[] LinearCombination(double[] v1, double[] v2, double c1, double c2)
    {
        int n = v1.Length;
        var result = new double[n];

        for (int i = 0; i < n; i++)
        {
            result[i] = c1 * v1[i] + c2 * v2[i];
        }

        return result;
    }

    private void PerformShrink(
        List<SimplexVertex<TMetric>> simplex,
        SimplexVertex<TMetric> bestVertex,
        TInput inputData,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> paramDefs,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        double shrinkFactor,
        CancellationToken cancellationToken
    )
    {
        int dimensionCount = paramDefs.Count;

        // shrink all points (except the best, simplex[0]) towards the best point
        for (int i = 1; i <= dimensionCount; i++)
        {
            // shrink: xi_new = x_best + sigma * (xi_old - x_best) = (1-sigma)*x_best + sigma*xi_old
            var shrunkParamsArray = LinearCombination(bestVertex.ParametersArray, simplex[i].ParametersArray, 1.0 - shrinkFactor, shrinkFactor);
            simplex[i] = EvaluateNewVertex(shrunkParamsArray, inputData, paramNames, paramDefs, objectiveFunction, cancellationToken);
        }

        _logger.LogTrace("Shrink operation completed for {NumPoints} points.", dimensionCount);
    }

    private static List<string> AnalyzeBoundaryProximity(IReadOnlyDictionary<string, double> bestParams, IReadOnlyList<ParameterDefinition> parameters)
    {
        const double boundaryThresholdFactor = 0.01;
        var boundaryParams = new List<string>();

        foreach (var paramDef in parameters)
        {
            if (bestParams.TryGetValue(paramDef.Name, out double value))
            {
                double paramRange = paramDef.MaxValue - paramDef.MinValue;
                if (paramRange <= 1e-9)
                {
                    var isAtMin = NumericUtils.AreApproximatelyEqual(value, paramDef.MinValue);
                    var isAtMax = NumericUtils.AreApproximatelyEqual(value, paramDef.MaxValue);

                    if (isAtMin || isAtMax)
                    {
                        boundaryParams.Add($"{paramDef.Name} (at bound of zero-range definition)");
                    }

                    continue;
                }

                double thresholdDistance = paramRange * boundaryThresholdFactor;

                if (Math.Abs(value - paramDef.MinValue) < thresholdDistance)
                {
                    boundaryParams.Add($"{paramDef.Name} (near lower bound, val:{value:F4}, min:{paramDef.MinValue:F4})");
                }
                else if (Math.Abs(value - paramDef.MaxValue) < thresholdDistance)
                {
                    boundaryParams.Add($"{paramDef.Name} (near upper bound, val:{value:F4}, max:{paramDef.MaxValue:F4})");
                }
            }
        }

        return boundaryParams;
    }

    private static string FormatParams(IReadOnlyDictionary<string, double> paramsDict)
    {
        return string.Join(", ", paramsDict.Select(kvp => $"{kvp.Key}={kvp.Value:F4}"));
    }

    private static string FormatParamsArray(double[] paramsArray, IReadOnlyList<string> paramNames)
    {
        return string.Join(", ", paramsArray.Select((val, idx) => $"{paramNames[idx]}={val:F4}"));
    }
}
