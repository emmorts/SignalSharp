namespace SignalSharp.Optimization.GridSearch;

/// <summary>
/// Configuration options for the <see cref="GridSearchOptimizer{TInput, TMetric}"/>.
/// </summary>
public record GridSearchOptimizerOptions
{
    /// <summary>
    /// Default number of grid steps to use for each parameter if not specified otherwise.
    /// Must be at least 2.
    /// </summary>
    public int DefaultGridSteps { get; init; } = 10;

    /// <summary>
    /// Allows overriding the number of grid steps for specific parameters.
    /// Keys are parameter names (matching <see cref="ParameterDefinition.Name"/>).
    /// Values must be at least 2.
    /// </summary>
    public IReadOnlyDictionary<string, int>? PerParameterGridSteps { get; init; } = null;

    /// <summary>
    /// Maximum number of function evaluations to perform.
    /// If null, no limit is applied.
    /// </summary>
    /// <remarks>
    /// This is particularly useful for higher-dimensional parameter spaces where the total number
    /// of grid points would be prohibitively large.
    /// </remarks>
    public int? MaxFunctionEvaluations { get; init; } = null;

    /// <summary>
    /// Whether to enable parallel processing of parameter combinations.
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;

    /// <summary>
    /// Maximum degree of parallelism when <see cref="EnableParallelProcessing"/> is true.
    /// If null, defaults to <see cref="Environment.ProcessorCount"/>.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; init; } = null;

    /// <summary>
    /// Early stopping threshold. If the objective metric reaches this value or lower,
    /// the optimization stops early. If null, no early stopping is used.
    /// </summary>
    public double? EarlyStoppingThreshold { get; init; } = null;

    /// <summary>
    /// Whether to use logarithmic spacing instead of linear spacing for certain parameters.
    /// Particularly useful for parameters like alpha, beta, gamma in exponential smoothing
    /// which often work better with logarithmic spacing in the 0-1 range.
    /// </summary>
    public IReadOnlySet<string>? UseLogarithmicScaleFor { get; init; } = null;

    /// <summary>
    /// Whether to enable adaptive grid refinement around the best solution.
    /// When true, after the initial grid search, a finer grid is created around
    /// the best solution to improve precision.
    /// </summary>
    public bool EnableAdaptiveRefinement { get; init; } = false;

    /// <summary>
    /// The factor that determines the range of the refined grid relative to the original range.
    /// For example, 0.2 means the refined grid spans 20% of the original range around the best point.
    /// </summary>
    public double RefinementRangeFactor { get; init; } = 0.2;

    /// <summary>
    /// Number of grid steps to use for each parameter in the refinement phase.
    /// </summary>
    public int RefinementGridSteps { get; init; } = 5;

    public GridSearchOptimizerOptions()
    {
        ValidateProperties();
    }

    private void ValidateProperties()
    {
        if (DefaultGridSteps < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(DefaultGridSteps), $"{nameof(DefaultGridSteps)} must be at least 2.");
        }

        if (PerParameterGridSteps != null)
        {
            foreach (var kvp in PerParameterGridSteps)
            {
                if (kvp.Value < 2)
                {
                    throw new ArgumentOutOfRangeException(nameof(PerParameterGridSteps), $"Grid steps for parameter '{kvp.Key}' must be at least 2.");
                }
            }
        }

        if (MaxFunctionEvaluations is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFunctionEvaluations), "Maximum function evaluations must be positive.");
        }

        if (MaxDegreeOfParallelism is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "Maximum degree of parallelism must be positive.");
        }

        if (RefinementRangeFactor is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(RefinementRangeFactor), "Refinement range factor must be between 0 and 1 exclusive.");
        }

        if (RefinementGridSteps < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(RefinementGridSteps), "Refinement grid steps must be at least 2.");
        }
    }
}
