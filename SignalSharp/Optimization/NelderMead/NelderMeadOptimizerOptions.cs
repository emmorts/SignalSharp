namespace SignalSharp.Optimization.NelderMead;

/// <summary>
/// Configuration options for the <see cref="NelderMeadOptimizer{TInput, TMetric}"/>.
/// </summary>
public record NelderMeadOptimizerOptions
{
    /// <summary>
    /// Maximum number of iterations for each optimization run.
    /// </summary>
    public int MaxIterations { get; init; } = 1000;

    /// <summary>
    /// Maximum number of function evaluations across all optimization runs.
    /// If null, the limit is primarily controlled by MaxIterations.
    /// </summary>
    public int? MaxFunctionEvaluations { get; init; } = null;

    /// <summary>
    /// Absolute tolerance for the difference in function values between the best and worst points in the simplex.
    /// If (f(worst) - f(best)) is less than this, convergence is assumed.
    /// Must be non-negative.
    /// </summary>
    public double FunctionValueConvergenceTolerance { get; init; } = 1e-6;

    /// <summary>
    /// Whether to enable parameter-based convergence checking.
    /// When true, the optimizer will also check if parameter values across the simplex
    /// have converged to within <see cref="ParameterConvergenceTolerance"/>.
    /// </summary>
    public bool EnableParameterConvergence { get; init; } = true;

    /// <summary>
    /// Relative tolerance for parameter convergence.
    /// If the maximum normalized difference between any parameter values in the simplex
    /// is less than this value, convergence is assumed.
    /// Must be positive.
    /// </summary>
    public double ParameterConvergenceTolerance { get; init; } = 1e-4;

    /// <summary>
    /// Whether to enable multi-start optimization.
    /// When true, the optimizer will perform multiple optimization runs
    /// with different starting points to increase the chances of finding the global minimum.
    /// </summary>
    public bool EnableMultiStart { get; init; } = false;

    /// <summary>
    /// The maximum number of additional starts to attempt.
    /// Only used when <see cref="EnableMultiStart"/> is true.
    /// </summary>
    public int MaxRestarts { get; init; } = 2;

    /// <summary>
    /// Whether to enable adaptive parameter adjustment during optimization.
    /// When true, the Nelder-Mead coefficients are adjusted based on iteration count and dimension.
    /// </summary>
    public bool EnableAdaptiveParameters { get; init; } = false;

    // Standard Nelder-Mead coefficients
    /// <summary>
    /// Reflection factor (rho). Typically 1.0. Must be greater than 0.
    /// </summary>
    public double ReflectionFactor { get; init; } = 1.0;

    /// <summary>
    /// Expansion factor (chi). Typically 2.0. Must be greater than ReflectionFactor.
    /// </summary>
    public double ExpansionFactor { get; init; } = 2.0;

    /// <summary>
    /// Contraction factor (gamma). Typically 0.5. Must be between 0 (exclusive) and 1 (exclusive).
    /// </summary>
    public double ContractionFactor { get; init; } = 0.5;

    /// <summary>
    /// Shrink factor (sigma). Typically 0.5. Must be between 0 (exclusive) and 1 (exclusive).
    /// </summary>
    public double ShrinkFactor { get; init; } = 0.5;

    /// <summary>
    /// Maximum number of iterations without improvement before the optimizer is considered to be stagnating.
    /// </summary>
    public int StagnationThresholdCount { get; init; } = 10;

    /// <summary>
    /// An absolute tolerance for improvement. If the best function value does not improve
    /// by at least this amount for <see cref="StagnationThresholdCount"/> consecutive iterations,
    /// the optimization is considered to have stagnated.
    /// </summary>
    public double StagnationImprovementThreshold { get; init; } = 1e-9;

    /// <summary>
    /// Factor used to generate the initial simplex by perturbing the initial guess along each dimension,
    /// relative to the range of that parameter. E.g., 0.05 means 5% of the parameter's range.
    /// Must be positive.
    /// </summary>
    public double InitialSimplexRangeFactor { get; init; } = 0.05;

    /// <summary>
    /// Absolute step used to generate the initial simplex if a parameter's range is zero or extremely small.
    /// Must be positive.
    /// </summary>
    public double InitialSimplexAbsoluteStepForZeroRange { get; init; } = 0.001;

    public NelderMeadOptimizerOptions()
    {
        ValidateProperties();
    }

    private void ValidateProperties()
    {
        if (MaxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxIterations), $"{nameof(MaxIterations)} must be positive.");
        }

        if (MaxFunctionEvaluations is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFunctionEvaluations), $"{nameof(MaxFunctionEvaluations)} must be positive.");
        }

        if (FunctionValueConvergenceTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FunctionValueConvergenceTolerance),
                $"{nameof(FunctionValueConvergenceTolerance)} must be non-negative."
            );
        }

        if (ParameterConvergenceTolerance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ParameterConvergenceTolerance), $"{nameof(ParameterConvergenceTolerance)} must be positive.");
        }

        if (MaxRestarts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRestarts), $"{nameof(MaxRestarts)} must be non-negative.");
        }

        if (ReflectionFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ReflectionFactor), $"{nameof(ReflectionFactor)} must be positive.");
        }

        if (ExpansionFactor <= ReflectionFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(ExpansionFactor), $"{nameof(ExpansionFactor)} must be greater than {nameof(ReflectionFactor)}.");
        }

        if (ContractionFactor is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ContractionFactor), $"{nameof(ContractionFactor)} must be between 0 and 1 (exclusive).");
        }

        if (ShrinkFactor is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ShrinkFactor), $"{nameof(ShrinkFactor)} must be between 0 and 1 (exclusive).");
        }

        if (InitialSimplexRangeFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialSimplexRangeFactor), $"{nameof(InitialSimplexRangeFactor)} must be positive.");
        }

        if (InitialSimplexAbsoluteStepForZeroRange <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InitialSimplexAbsoluteStepForZeroRange),
                $"{nameof(InitialSimplexAbsoluteStepForZeroRange)} must be positive."
            );
        }
    }
}
