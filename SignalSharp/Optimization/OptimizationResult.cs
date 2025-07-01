namespace SignalSharp.Optimization;

/// <summary>
/// Represents the result of an optimization process.
/// </summary>
/// <typeparam name="TMetric">The type of the objective metric (e.g., SSE).</typeparam>
/// <param name="BestParameters">A dictionary of the best parameter names and their optimized values.</param>
/// <param name="MinimizedMetric">The value of the objective metric at the best parameters.</param>
/// <param name="Success">Indicates if the optimization was successful in finding a valid solution.</param>
/// <param name="Message">An optional message providing more details about the optimization outcome or errors.</param>
/// <param name="Iterations">Optional: The number of iterations performed by the optimizer (if applicable).</param>
/// <param name="FunctionEvaluations">Optional: The total number of times the objective function was evaluated (if applicable).</param>
public record OptimizationResult<TMetric>(
    IReadOnlyDictionary<string, double> BestParameters,
    TMetric MinimizedMetric,
    bool Success = true,
    string? Message = null,
    int? Iterations = null,
    int? FunctionEvaluations = null
);
