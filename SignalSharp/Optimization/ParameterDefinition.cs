namespace SignalSharp.Optimization;

/// <summary>
/// Defines a parameter to be optimized, including its bounds and an optional initial guess.
/// This structure is intended to be general for various optimization algorithms.
/// </summary>
/// <remarks>
/// Instances are validated on construction: the name must be non-empty and the bounds must be finite with
/// <see cref="MinValue"/> not greater than <see cref="MaxValue"/>.
/// </remarks>
public record ParameterDefinition
{
    /// <summary>
    /// Defines a parameter to be optimized, including its bounds and an optional initial guess.
    /// </summary>
    /// <param name="Name">The unique name of the parameter (e.g., "Alpha", "Beta").</param>
    /// <param name="MinValue">The minimum allowed value for the parameter (lower bound).</param>
    /// <param name="MaxValue">The maximum allowed value for the parameter (upper bound).</param>
    /// <param name="InitialGuess">
    /// An optional initial guess for the parameter, typically within the bounds.
    /// Crucial for iterative optimizers; values outside the bounds are clamped by the optimizers.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="Name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="Name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the bounds are not finite, when <paramref name="MinValue"/> is greater than <paramref name="MaxValue"/>,
    /// or when <paramref name="InitialGuess"/> is not finite.
    /// </exception>
    public ParameterDefinition(string Name, double MinValue, double MaxValue, double? InitialGuess = null)
    {
        Validate(Name, MinValue, MaxValue, InitialGuess);

        this.Name = Name;
        this.MinValue = MinValue;
        this.MaxValue = MaxValue;
        this.InitialGuess = InitialGuess;
    }

    /// <summary>
    /// The unique name of the parameter (e.g., "Alpha", "Beta").
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The minimum allowed value for the parameter (lower bound).
    /// </summary>
    public double MinValue { get; init; }

    /// <summary>
    /// The maximum allowed value for the parameter (upper bound).
    /// </summary>
    public double MaxValue { get; init; }

    /// <summary>
    /// An optional initial guess for the parameter, typically within the bounds.
    /// Crucial for iterative optimizers.
    /// </summary>
    public double? InitialGuess { get; init; }

    /// <summary>
    /// Re-validates this definition, guarding against values changed after construction (for example through
    /// a <c>with</c> expression). Called by the optimizers before a parameter definition is used.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <see cref="Name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the bounds or initial guess are invalid.</exception>
    internal void Validate()
    {
        Validate(Name, MinValue, MaxValue, InitialGuess);
    }

    /// <summary>
    /// Validates the supplied definition values.
    /// </summary>
    private static void Validate(string name, double minValue, double maxValue, double? initialGuess)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parameter name must not be empty or whitespace.", nameof(name));
        }

        if (!double.IsFinite(minValue) || !double.IsFinite(maxValue))
        {
            throw new ArgumentOutOfRangeException(nameof(minValue), minValue, "Parameter bounds must be finite values.");
        }

        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minValue),
                minValue,
                $"Minimum value ({minValue}) must not be greater than maximum value ({maxValue})."
            );
        }

        if (initialGuess.HasValue && !double.IsFinite(initialGuess.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(initialGuess), initialGuess.Value, "Initial guess must be a finite value.");
        }
    }
}
