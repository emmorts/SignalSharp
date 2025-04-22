namespace SignalSharp.Detection.PELT;

/// <summary>
/// Represents the result of an automatic penalty selection process for PELT.
/// </summary>
public record PELTPenaltySelectionResult
{
    /// <summary>
    /// The penalty value selected by the chosen method.
    /// </summary>
    public required double SelectedPenalty { get; init; }

    /// <summary>
    /// The optimal change points detected using the <see cref="SelectedPenalty"/>.
    /// </summary>
    public required int[] OptimalBreakpoints { get; init; }

    /// <summary>
    /// The method used to select the penalty.
    /// </summary>
    public required PELTPenaltySelectionMethod SelectionMethod { get; init; }

    /// <summary>
    /// Optional diagnostic information about the selection process.
    /// Contains tuples of (Tested Penalty, Calculated Score, Number of Change Points).
    /// The interpretation of 'Score' depends on the <see cref="SelectionMethod"/> (e.g., BIC value, AIC value).
    /// May be null if diagnostics were not generated or requested.
    /// </summary>
    public IReadOnlyList<(double Penalty, double Score, int ChangePoints)>? Diagnostics { get; init; }
}