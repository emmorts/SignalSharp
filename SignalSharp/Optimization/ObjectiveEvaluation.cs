namespace SignalSharp.Optimization;

/// <summary>
/// Represents the output of evaluating the objective function at a given set of trial parameters.
/// </summary>
/// <typeparam name="TMetric">The type of the objective metric (e.g., SSE).</typeparam>
/// <param name="MetricValue">The calculated value of the objective metric.</param>
/// <param name="Gradient">
/// An optional dictionary mapping parameter names to their corresponding gradient components
/// with respect to the objective metric. Null if gradients are not provided or not applicable.
/// </param>
public record ObjectiveEvaluation<TMetric>(TMetric MetricValue, IReadOnlyDictionary<string, double>? Gradient = null);
