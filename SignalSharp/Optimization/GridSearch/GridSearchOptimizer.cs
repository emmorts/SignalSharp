using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SignalSharp.Optimization.GridSearch;

/// <summary>
/// Implements a grid search optimization strategy.
/// It systematically evaluates the objective function at points on a grid defined by the parameter ranges and step counts.
/// This optimizer does not use gradient information.
/// </summary>
/// <typeparam name="TInput">The type of the input data provided to the objective function.</typeparam>
/// <typeparam name="TMetric">The type of the objective metric (e.g., SSE), which must be a floating-point type.</typeparam>
public class GridSearchOptimizer<TInput, TMetric> : IParameterOptimizer<TInput, TMetric>
    where TMetric : IFloatingPointIeee754<TMetric>
{
    /// <summary>
    /// Maximum number of grid combinations materialized in a single search. Larger grids are systematically
    /// sampled down to this limit, and a grid larger than this without an evaluation budget is rejected rather
    /// than exhausting memory. The limit bounds the (quadratic-ish) dictionary overhead of the combination list.
    /// </summary>
    private const long MaxMaterializedCombinations = 1_000_000;

    private readonly GridSearchOptimizerOptions _options;
    private readonly ILogger _logger;
    private readonly ILogger<GridSearchOptimizer<TInput, TMetric>> _typedLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridSearchOptimizer{TInput, TMetric}"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the grid search.</param>
    /// <param name="logger">Optional logger instance.</param>
    public GridSearchOptimizer(GridSearchOptimizerOptions? options = null, ILogger<GridSearchOptimizer<TInput, TMetric>>? logger = null)
    {
        _options = options ?? new GridSearchOptimizerOptions();
        _options.Validate();
        _typedLogger = logger ?? NullLogger<GridSearchOptimizer<TInput, TMetric>>.Instance;
        _logger = _typedLogger;
    }

    /// <summary>
    /// Optimizes the objective function synchronously by running the asynchronous search and waiting for it.
    /// </summary>
    /// <param name="inputData">The input data passed to the objective function.</param>
    /// <param name="parametersToOptimize">The parameters to optimize, with unique names.</param>
    /// <param name="objectiveFunction">The objective function to minimize.</param>
    /// <returns>The optimization result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the parameters or objective function are null.</exception>
    /// <exception cref="ArgumentException">Thrown when parameter names are duplicated.</exception>
    public OptimizationResult<TMetric> Optimize(
        TInput inputData,
        IEnumerable<ParameterDefinition> parametersToOptimize,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction
    )
    {
        return OptimizeAsync(inputData, parametersToOptimize, objectiveFunction, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<OptimizationResult<TMetric>> OptimizeAsync(
        TInput inputData,
        IEnumerable<ParameterDefinition> parametersToOptimize,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Starting grid search optimization...");

        ArgumentNullException.ThrowIfNull(parametersToOptimize, nameof(parametersToOptimize));
        ArgumentNullException.ThrowIfNull(objectiveFunction, nameof(objectiveFunction));

        var optimizableParams = parametersToOptimize.ToList();
        if (optimizableParams.Any(p => p is null))
        {
            throw new ArgumentException("Parameter definitions must not contain null entries.", nameof(parametersToOptimize));
        }

        foreach (var parameter in optimizableParams)
        {
            parameter.Validate();
        }

        var duplicateName = optimizableParams.GroupBy(p => p.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateName is not null)
        {
            throw new ArgumentException($"Duplicate parameter name '{duplicateName}'. Parameter names must be unique.", nameof(parametersToOptimize));
        }

        if (optimizableParams.Count == 0)
        {
            return CreateEmptyParametersResult();
        }

        var paramValueRanges = GenerateParameterRanges(optimizableParams);
        var paramNames = optimizableParams.Select(p => p.Name).ToList();

        var bestResult = await ExecuteGridSearchAsync(inputData, paramNames, paramValueRanges, objectiveFunction, cancellationToken).ConfigureAwait(false);

        // only try refinement if there's enough budget for a meaningful refinement
        if (ShouldPerformAdaptiveRefinement(bestResult))
        {
            int remainingEvaluations = CalculateRemainingEvaluations(bestResult);
            _logger.LogDebug(
                "Refinement check: budget available {Available}, minimum needed {Minimum}",
                remainingEvaluations,
                _options.RefinementGridSteps * 2
            );

            bestResult = await RefineAroundBestSolutionAsync(
                    inputData,
                    paramNames,
                    optimizableParams,
                    bestResult,
                    objectiveFunction,
                    remainingEvaluations,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        var resultWithBoundaryAnalysis = AddBoundaryAnalysisToResult(bestResult, optimizableParams);
        LogOptimizationResults(resultWithBoundaryAnalysis);

        return resultWithBoundaryAnalysis;
    }

    private OptimizationResult<TMetric> CreateEmptyParametersResult()
    {
        _logger.LogWarning("No parameters provided for optimization.");

        return new OptimizationResult<TMetric>(
            BestParameters: new Dictionary<string, double>(),
            MinimizedMetric: TMetric.NaN,
            Success: false,
            Message: "No parameters to optimize.",
            FunctionEvaluations: 0
        );
    }

    private bool ShouldPerformAdaptiveRefinement(OptimizationResult<TMetric> result)
    {
        if (!_options.EnableAdaptiveRefinement || !result.Success)
        {
            return false;
        }

        int remainingEvaluations = CalculateRemainingEvaluations(result);

        // need at least enough evaluations for a minimum viable refinement grid
        // (optimizableParams.Count * 3 in the original code)
        return remainingEvaluations > 0 && remainingEvaluations >= _options.RefinementGridSteps * 2;
    }

    private int CalculateRemainingEvaluations(OptimizationResult<TMetric> result)
    {
        return (_options.MaxFunctionEvaluations ?? int.MaxValue) - (result.FunctionEvaluations ?? 0);
    }

    private void LogOptimizationResults(OptimizationResult<TMetric> result)
    {
        _logger.LogInformation(
            "Grid search optimization complete. Best metric: {Metric:G6}. " + "Total evaluations: {Evals}. Best parameters: [{Params}]",
            Convert.ToDouble(result.MinimizedMetric),
            result.FunctionEvaluations,
            string.Join(", ", result.BestParameters.Select(kvp => $"{kvp.Key}={kvp.Value:F4}"))
        );
    }

    /// <summary>
    /// Generates parameter ranges based on the optimization settings and parameter definitions.
    /// </summary>
    private List<double[]> GenerateParameterRanges(IReadOnlyList<ParameterDefinition> parameters)
    {
        var paramValueRanges = new List<double[]>(parameters.Count);

        foreach (var paramDef in parameters)
        {
            int gridSteps = GetStepsForParameter(paramDef.Name);
            ValidateGridSteps(gridSteps, paramDef.Name);

            var values = new double[gridSteps];
            FillParameterValues(values, paramDef, gridSteps);
            paramValueRanges.Add(values);
        }

        return paramValueRanges;
    }

    private int GetStepsForParameter(string paramName)
    {
        return _options.PerParameterGridSteps?.TryGetValue(paramName, out int specificSteps) == true ? specificSteps : _options.DefaultGridSteps;
    }

    private static void ValidateGridSteps(int gridSteps, string paramName)
    {
        if (gridSteps < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(gridSteps), $"GridSteps for parameter '{paramName}' must be at least 2. Effective steps: {gridSteps}");
        }
    }

    private void FillParameterValues(double[] values, ParameterDefinition paramDef, int gridSteps)
    {
        if (ShouldUseLogarithmicScale(paramDef))
        {
            FillLogarithmicValues(values, paramDef, gridSteps);
        }
        else
        {
            FillLinearValues(values, paramDef, gridSteps);
        }
    }

    private bool ShouldUseLogarithmicScale(ParameterDefinition paramDef)
    {
        return _options.UseLogarithmicScaleFor?.Contains(paramDef.Name) == true && paramDef is { MinValue: > 0, MaxValue: > 0 };
    }

    private static void FillLogarithmicValues(double[] values, ParameterDefinition paramDef, int gridSteps)
    {
        double logMin = Math.Log(paramDef.MinValue);
        double logMax = Math.Log(paramDef.MaxValue);

        for (int i = 0; i < gridSteps; i++)
        {
            double logValue = logMin + i * (logMax - logMin) / (gridSteps - 1);
            values[i] = Math.Clamp(Math.Exp(logValue), paramDef.MinValue, paramDef.MaxValue);
        }
    }

    private static void FillLinearValues(double[] values, ParameterDefinition paramDef, int gridSteps)
    {
        for (int i = 0; i < gridSteps; i++)
        {
            values[i] = paramDef.MinValue + i * (paramDef.MaxValue - paramDef.MinValue) / (gridSteps - 1);
            values[i] = Math.Clamp(values[i], paramDef.MinValue, paramDef.MaxValue);
        }
    }

    /// <summary>
    /// Executes the main grid search algorithm, optionally in parallel.
    /// </summary>
    private async Task<OptimizationResult<TMetric>> ExecuteGridSearchAsync(
        TInput inputData,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<double[]> paramValueRanges,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        CancellationToken cancellationToken
    )
    {
        long totalCombinations = ComputeTotalCombinationCount(paramValueRanges);
        int effectiveMaxEvaluations = _options.MaxFunctionEvaluations ?? int.MaxValue;

        if (_options.MaxFunctionEvaluations is null && totalCombinations > MaxMaterializedCombinations)
        {
            throw new InvalidOperationException(
                $"The grid contains {totalCombinations} combinations, which exceeds the supported maximum of "
                    + $"{MaxMaterializedCombinations} without an evaluation budget. Set "
                    + $"{nameof(GridSearchOptimizerOptions.MaxFunctionEvaluations)} to sample the grid or reduce the grid steps."
            );
        }

        _logger.LogDebug(
            "Grid search: {ParamCount} parameters, {TotalCombinations} total combinations. " + "Default steps: {DefaultSteps}, Max evaluations: {MaxEvals}",
            paramNames.Count,
            totalCombinations,
            _options.DefaultGridSteps,
            _options.MaxFunctionEvaluations?.ToString() ?? "unlimited"
        );

        var paramCombinations = GenerateParameterCombinations(paramNames, paramValueRanges, totalCombinations, effectiveMaxEvaluations);

        var searchState = new GridSearchState<TMetric>();
        await ProcessCombinationsAsync(inputData, paramCombinations, objectiveFunction, searchState, cancellationToken).ConfigureAwait(false);

        return CreateSearchResult(searchState);
    }

    private OptimizationResult<TMetric> CreateSearchResult(GridSearchState<TMetric> searchState)
    {
        // Only NaN and +Infinity signal a failed search: a minimization objective may legitimately return
        // -Infinity, and the evaluation loop accepts any non-NaN metric as the current best.
        if (TMetric.IsNaN(searchState.MinMetric) || TMetric.IsPositiveInfinity(searchState.MinMetric) || searchState.BestParams.Count == 0)
        {
            const string errorMsg =
                "Grid search optimization failed to find any valid parameters where the metric "
                + "was not infinite or NaN. Check parameter ranges, model stability, or data quality.";
            _logger.LogError(errorMsg);
            return new OptimizationResult<TMetric>(
                searchState.BestParams,
                searchState.MinMetric,
                false,
                errorMsg,
                FunctionEvaluations: searchState.FunctionEvaluations
            );
        }

        string successMessage = searchState.EarlyStop ? "Grid search completed early due to reaching threshold." : "Grid search completed successfully.";

        return new OptimizationResult<TMetric>(
            searchState.BestParams,
            searchState.MinMetric,
            true,
            successMessage,
            FunctionEvaluations: searchState.FunctionEvaluations
        );
    }

    /// <summary>
    /// Process all parameter combinations, either sequentially or in parallel
    /// </summary>
    private async Task ProcessCombinationsAsync(
        TInput inputData,
        List<Dictionary<string, double>> paramCombinations,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        GridSearchState<TMetric> state,
        CancellationToken cancellationToken
    )
    {
        var progress = CreateProgressReporter(paramCombinations.Count);

        if (_options.EnableParallelProcessing)
        {
            await ProcessCombinationsInParallelAsync(inputData, paramCombinations, objectiveFunction, state, progress, cancellationToken);
        }
        else
        {
            ProcessCombinationsSequentially(inputData, paramCombinations, objectiveFunction, state, progress, cancellationToken);
        }
    }

    private Progress<int> CreateProgressReporter(int totalCombinations)
    {
        return new Progress<int>(count =>
        {
            if (count % Math.Max(1, totalCombinations / 100) == 0)
            {
                _logger.LogTrace("Grid search progress: {Progress:F1}% ({Current}/{Total})", 100.0 * count / totalCombinations, count, totalCombinations);
            }
        });
    }

    private async Task ProcessCombinationsInParallelAsync(
        TInput inputData,
        List<Dictionary<string, double>> paramCombinations,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        GridSearchState<TMetric> state,
        IProgress<int> progress,
        CancellationToken cancellationToken
    )
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism ?? Environment.ProcessorCount,
            CancellationToken = cancellationToken,
        };

        int processedCount = 0;
        var lockObject = new object();

        try
        {
            await Task.Run(
                () =>
                {
                    Parallel.ForEach(
                        paramCombinations,
                        parallelOptions,
                        (combination, loopState) =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var result = EvaluateCombination(inputData, combination, objectiveFunction);

                            lock (lockObject)
                            {
                                state.FunctionEvaluations++;

                                if (!TMetric.IsNaN(result.metricValue) && result.metricValue < state.MinMetric)
                                {
                                    UpdateBestResult(state, result.metricValue, combination);

                                    if (ShouldStopEarly(state.MinMetric))
                                    {
                                        state.EarlyStop = true;
                                        loopState.Break();
                                    }
                                }

                                Interlocked.Increment(ref processedCount);
                                progress.Report(processedCount);
                            }
                        }
                    );
                },
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Grid search optimization was cancelled.");
            throw;
        }
    }

    private void ProcessCombinationsSequentially(
        TInput inputData,
        List<Dictionary<string, double>> paramCombinations,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        GridSearchState<TMetric> state,
        IProgress<int> progress,
        CancellationToken cancellationToken
    )
    {
        for (int i = 0; i < paramCombinations.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Grid search optimization was cancelled.");
                throw new OperationCanceledException(cancellationToken);
            }

            var combination = paramCombinations[i];
            var result = EvaluateCombination(inputData, combination, objectiveFunction);

            state.FunctionEvaluations++;

            if (!TMetric.IsNaN(result.metricValue) && result.metricValue < state.MinMetric)
            {
                UpdateBestResult(state, result.metricValue, combination);

                if (ShouldStopEarly(state.MinMetric))
                {
                    state.EarlyStop = true;
                    break;
                }
            }

            progress.Report(i + 1);
        }
    }

    private void UpdateBestResult(GridSearchState<TMetric> state, TMetric metricValue, Dictionary<string, double> parameters)
    {
        state.MinMetric = metricValue;
        state.BestParams = new Dictionary<string, double>(parameters);

        _logger.LogTrace(
            "New best metric found: {Metric:G6}. Parameters: [{Params}]",
            Convert.ToDouble(state.MinMetric),
            string.Join(", ", state.BestParams.Select(kvp => $"{kvp.Key}={kvp.Value:F4}"))
        );
    }

    private bool ShouldStopEarly(TMetric currentBestMetric)
    {
        if (_options.EarlyStoppingThreshold != null && Convert.ToDouble(currentBestMetric) <= _options.EarlyStoppingThreshold.Value)
        {
            _logger.LogInformation(
                "Early stopping threshold reached: {Metric:G6} <= {Threshold}",
                Convert.ToDouble(currentBestMetric),
                _options.EarlyStoppingThreshold.Value
            );
            return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates a single parameter combination using the objective function.
    /// </summary>
    private (TMetric metricValue, IReadOnlyDictionary<string, double>? gradient) EvaluateCombination(
        TInput inputData,
        IReadOnlyDictionary<string, double> parameters,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction
    )
    {
        try
        {
            var evaluationResult = objectiveFunction(inputData, parameters);
            return (evaluationResult.MetricValue, evaluationResult.Gradient);
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation raised by the objective instead of treating it as a failed evaluation.
            throw;
        }
        catch (Exception ex)
        {
            string paramsString = string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value:F4}"));
            _logger.LogWarning(
                ex,
                "Objective function threw an exception for parameters: [{Params}]. " + "Assigning positive infinity to metric.",
                paramsString
            );
            return (TMetric.PositiveInfinity, null);
        }
    }

    /// <summary>
    /// Computes the total number of grid combinations, detecting overflow of the Cartesian product.
    /// </summary>
    /// <exception cref="OverflowException">Thrown when the number of combinations exceeds the range of a 64-bit integer.</exception>
    private static long ComputeTotalCombinationCount(IReadOnlyList<double[]> paramValueRanges)
    {
        long total = 1;

        foreach (var values in paramValueRanges)
        {
            total = checked(total * values.Length);
        }

        return total;
    }

    /// <summary>
    /// Generates the parameter combinations to evaluate.
    /// </summary>
    /// <remarks>
    /// Each sampled combination is decoded directly from its flat mixed-radix index, so the number of generated
    /// combinations is bounded by the evaluation budget (and by <see cref="MaxMaterializedCombinations"/>) even
    /// when the full grid is astronomically large. When the grid is larger than the budget, combinations are
    /// stride-sampled for systematic coverage instead of materializing the whole product.
    /// </remarks>
    private List<Dictionary<string, double>> GenerateParameterCombinations(
        IReadOnlyList<string> paramNames,
        IReadOnlyList<double[]> paramValueRanges,
        long totalCombinations,
        int effectiveMaxEvaluations
    )
    {
        // Never generate more than the budget allows, and never more than the hard materialization cap:
        // an evaluation budget is an upper bound, so exceeding the cap only means fewer sampled combinations.
        var limit = Math.Min(Math.Min(totalCombinations, effectiveMaxEvaluations), MaxMaterializedCombinations);
        var stride = Math.Max(1L, totalCombinations / Math.Max(1L, limit));
        var combinations = new List<Dictionary<string, double>>((int)limit);

        var indices = new int[paramNames.Count];

        for (long sample = 0; sample < limit; sample++)
        {
            // Decode the flat index: parameter 0 varies fastest, the last parameter slowest.
            var remainder = sample * stride;
            for (var i = 0; i < paramNames.Count; i++)
            {
                var radix = paramValueRanges[i].Length;
                indices[i] = (int)(remainder % radix);
                remainder /= radix;
            }

            var combination = new Dictionary<string, double>(paramNames.Count);
            for (int i = 0; i < paramNames.Count; i++)
            {
                combination[paramNames[i]] = paramValueRanges[i][indices[i]];
            }

            combinations.Add(combination);
        }

        if (stride > 1)
        {
            _logger.LogInformation(
                "Grid search limited to {Selected} systematically sampled combinations out of {TotalCombinations}.",
                combinations.Count,
                totalCombinations
            );
        }

        return combinations;
    }

    /// <summary>
    /// Refines the search around the best solution found so far with a finer grid.
    /// </summary>
    private async Task<OptimizationResult<TMetric>> RefineAroundBestSolutionAsync(
        TInput inputData,
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> originalParams,
        OptimizationResult<TMetric> initialResult,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        int remainingEvaluations,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Starting adaptive refinement around best solution...");

        var refinedParams = CreateRefinedParameters(paramNames, originalParams, initialResult);
        var refinedOptions = CreateRefinementOptions(remainingEvaluations);
        var refinedOptimizer = new GridSearchOptimizer<TInput, TMetric>(refinedOptions, _typedLogger);

        var refinementResult = await refinedOptimizer.OptimizeAsync(inputData, refinedParams, objectiveFunction, cancellationToken);

        int totalEvaluations = (initialResult.FunctionEvaluations ?? 0) + (refinementResult.FunctionEvaluations ?? 0);

        return CreateRefinementResult(initialResult, refinementResult, totalEvaluations);
    }

    private List<ParameterDefinition> CreateRefinedParameters(
        IReadOnlyList<string> paramNames,
        IReadOnlyList<ParameterDefinition> originalParams,
        OptimizationResult<TMetric> initialResult
    )
    {
        var refinedParams = new List<ParameterDefinition>();

        foreach (var paramName in paramNames)
        {
            var originalParam = originalParams.First(p => p.Name == paramName);
            var bestValue = initialResult.BestParameters[paramName];

            double originalRange = originalParam.MaxValue - originalParam.MinValue;
            double refinedRange = originalRange * _options.RefinementRangeFactor;

            double refinedMin = Math.Max(originalParam.MinValue, bestValue - refinedRange / 2);
            double refinedMax = Math.Min(originalParam.MaxValue, bestValue + refinedRange / 2);

            refinedParams.Add(new ParameterDefinition(paramName, refinedMin, refinedMax, bestValue));
        }

        return refinedParams;
    }

    private GridSearchOptimizerOptions CreateRefinementOptions(int remainingEvaluations)
    {
        return new GridSearchOptimizerOptions
        {
            DefaultGridSteps = _options.RefinementGridSteps,
            EnableParallelProcessing = _options.EnableParallelProcessing,
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            MaxFunctionEvaluations = remainingEvaluations,
            EarlyStoppingThreshold = _options.EarlyStoppingThreshold,
            EnableAdaptiveRefinement = false, // prevent recursion
        };
    }

    private OptimizationResult<TMetric> CreateRefinementResult(
        OptimizationResult<TMetric> initialResult,
        OptimizationResult<TMetric> refinementResult,
        int totalEvaluations
    )
    {
        if (refinementResult.Success && refinementResult.MinimizedMetric < initialResult.MinimizedMetric)
        {
            double previousMetric = Convert.ToDouble(initialResult.MinimizedMetric);
            double newMetric = Convert.ToDouble(refinementResult.MinimizedMetric);

            if (previousMetric == 0.0)
            {
                _logger.LogInformation("Refinement improved solution. New metric: {Metric:G6}, Previous: {PreviousMetric:G6}", newMetric, previousMetric);
            }
            else
            {
                _logger.LogInformation(
                    "Refinement improved solution. New metric: {Metric:G6}, " + "Previous: {PreviousMetric:G6}, Improvement: {Improvement:P2}",
                    newMetric,
                    previousMetric,
                    1 - newMetric / previousMetric
                );
            }

            return refinementResult with
            {
                Message = "Grid search with adaptive refinement completed successfully.",
                FunctionEvaluations = totalEvaluations,
            };
        }

        _logger.LogInformation("Refinement did not improve solution, keeping original result.");
        return initialResult with { FunctionEvaluations = totalEvaluations };
    }

    /// <summary>
    /// Adds boundary analysis information to the optimization result
    /// </summary>
    private static OptimizationResult<TMetric> AddBoundaryAnalysisToResult(OptimizationResult<TMetric> result, IReadOnlyList<ParameterDefinition> parameters)
    {
        var boundaryAnalysis = AnalyzeBoundaryProximity(result.BestParameters, parameters);
        string resultMessage = result.Message ?? "Grid search completed successfully.";

        if (boundaryAnalysis.Any())
        {
            resultMessage +=
                " Warning: The following parameters are at or near their bounds, which may indicate "
                + "that the optimal value lies outside the search space: "
                + string.Join(", ", boundaryAnalysis);
        }

        return result with
        {
            Message = resultMessage,
        };
    }

    /// <summary>
    /// Analyzes if parameters are at or near boundaries, which might indicate the optimal solution is outside the search space.
    /// </summary>
    private static List<string> AnalyzeBoundaryProximity(IReadOnlyDictionary<string, double> bestParams, IReadOnlyList<ParameterDefinition> parameters)
    {
        const double boundaryThreshold = 0.01; // 1% of parameter range
        var boundaryParams = new List<string>();

        foreach (var param in parameters)
        {
            if (bestParams.TryGetValue(param.Name, out double value))
            {
                double paramRange = param.MaxValue - param.MinValue;
                double normalizedDistance;

                if (Math.Abs(value - param.MinValue) < paramRange * boundaryThreshold)
                {
                    normalizedDistance = Math.Abs(value - param.MinValue) / paramRange;
                    boundaryParams.Add($"{param.Name} (at lower bound, distance: {normalizedDistance:P2})");
                }
                else if (Math.Abs(value - param.MaxValue) < paramRange * boundaryThreshold)
                {
                    normalizedDistance = Math.Abs(value - param.MaxValue) / paramRange;
                    boundaryParams.Add($"{param.Name} (at upper bound, distance: {normalizedDistance:P2})");
                }
            }
        }

        return boundaryParams;
    }
}
