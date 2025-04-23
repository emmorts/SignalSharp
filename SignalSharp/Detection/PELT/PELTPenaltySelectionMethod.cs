namespace SignalSharp.Detection.PELT;

/// <summary>
/// Specifies the method used for automatic penalty selection in PELT.
/// </summary>
public enum PELTPenaltySelectionMethod
{
    /// <summary>
    /// Bayesian Information Criterion (BIC) / Schwarz Information Criterion (SIC).
    /// Tends to favor simpler models. Requires a likelihood-based cost function.
    /// </summary>
    BIC,

    /// <summary>
    /// Akaike Information Criterion (AIC).
    /// May select slightly more complex models than BIC. Requires a likelihood-based cost function.
    /// </summary>
    AIC,

    /// <summary>
    /// Corrected Akaike Information Criterion (AICc).
    /// A correction for AIC, recommended for smaller sample sizes. Requires a likelihood-based cost function.
    /// </summary>
    AICc,
}
