namespace SignalSharp.Optimization;

/// <summary>
/// Defines a parameter to be optimized, including its bounds and an optional initial guess.
/// This structure is intended to be general for various optimization algorithms.
/// </summary>
/// <param name="Name">The unique name of the parameter (e.g., "Alpha", "Beta").</param>
/// <param name="MinValue">The minimum allowed value for the parameter (lower bound).</param>
/// <param name="MaxValue">The maximum allowed value for the parameter (upper bound).</param>
/// <param name="InitialGuess">
/// An optional initial guess for the parameter, typically within the bounds.
/// Crucial for iterative optimizers.
/// </param>
public record ParameterDefinition(string Name, double MinValue, double MaxValue, double? InitialGuess = null);
