namespace SignalSharp.Detection.PELT;

/// <summary>
/// Configuration options for the automatic penalty selection process in PELT.
/// </summary>
/// <param name="Method">The core selection method to use (e.g., BIC, AIC).</param>
public record PELTPenaltySelectionOptions(PELTPenaltySelectionMethod Method)
{
    /// <summary>
    /// The minimum penalty value to consider during the search.
    /// If null, the selector might attempt to infer a reasonable minimum.
    /// Used primarily by grid-search based methods (like AIC/BIC checks).
    /// </summary>
    public double? MinPenalty { get; init; }

    /// <summary>
    /// The maximum penalty value to consider during the search.
    /// If null, the selector might attempt to infer a reasonable maximum.
    /// Used primarily by grid-search based methods (like AIC/BIC checks).
    /// </summary>
    public double? MaxPenalty { get; init; }

    /// <summary>
    /// The approximate number of penalty values to test between MinPenalty and MaxPenalty.
    /// Used primarily by grid-search based methods (like AIC/BIC checks). Ignored by CROPS.
    /// Defaults to 50.
    /// </summary>
    public int NumPenaltySteps { get; init; } = 50;
}
