namespace SignalSharp.Optimization;

/// <summary>
/// Interface for a parameter optimization strategy.
/// Implementations of this interface will define specific optimization algorithms.
/// </summary>
/// <typeparam name="TInput">The type of the input data provided to the objective function (e.g., ReadOnlySpan<TData>).</typeparam>
/// <typeparam name="TMetric">The type of the objective metric returned by the objective function (e.g., SSE of type TData).</typeparam>
public interface IParameterOptimizer<TInput, TMetric>
{
    /// <summary>
    /// Optimizes a set of parameters for a given objective function, aiming to minimize the returned metric.
    /// </summary>
    /// <param name="inputData">The input data required by the objective function (e.g., time series signal).</param>
    /// <param name="parametersToOptimize">
    /// A collection of <see cref="ParameterDefinition"/> objects,
    /// describing each parameter's name, bounds, and optional initial guess.
    /// </param>
    /// <param name="objectiveFunction">
    /// A function delegate that, given the input data and a dictionary of current trial parameter values,
    /// computes and returns an <see cref="ObjectiveEvaluation{TMetric}"/>.
    /// The evaluation includes the metric value and, optionally, the gradient of the metric
    /// with respect to the parameters.
    /// </param>
    /// <returns>An <see cref="OptimizationResult{TMetric}"/> containing the best parameters found,
    /// the minimized metric value, and other optimization details.</returns>
    OptimizationResult<TMetric> Optimize(
        TInput inputData,
        IEnumerable<ParameterDefinition> parametersToOptimize,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction
    );

    /// <summary>
    /// Asynchronously optimizes a set of parameters for a given objective function with cancellation support.
    /// </summary>
    /// <param name="inputData">The input data required by the objective function (e.g., time series signal).</param>
    /// <param name="parametersToOptimize">
    /// A collection of <see cref="ParameterDefinition"/> objects,
    /// describing each parameter's name, bounds, and optional initial guess.
    /// </param>
    /// <param name="objectiveFunction">
    /// A function delegate that, given the input data and a dictionary of current trial parameter values,
    /// computes and returns an <see cref="ObjectiveEvaluation{TMetric}"/>.
    /// The evaluation includes the metric value and, optionally, the gradient of the metric
    /// with respect to the parameters.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the optimization process.</param>
    /// <returns>An <see cref="OptimizationResult{TMetric}"/> containing the best parameters found,
    /// the minimized metric value, and other optimization details.</returns>
    Task<OptimizationResult<TMetric>> OptimizeAsync(
        TInput inputData,
        IEnumerable<ParameterDefinition> parametersToOptimize,
        Func<TInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<TMetric>> objectiveFunction,
        CancellationToken cancellationToken
    );
}
