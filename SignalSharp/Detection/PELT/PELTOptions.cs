using SignalSharp.CostFunctions.Cost;

namespace SignalSharp.Detection.PELT;

/// <summary>
/// Represents the configuration options for the Piecewise Linear Trend Change (PELT) algorithm.
/// </summary>
/// <remarks>
/// <para>
/// These options allow customization of the PELT algorithm's behavior, including the cost function used to
/// evaluate segment quality, the minimum segment size, and the jump parameter for candidate change point evaluation.
/// </para>
/// <para>
/// Adjust these options based on the characteristics of your data, the expected nature of the changes,
/// and the desired sensitivity versus computational speed trade-off.
/// </para>
/// </remarks>
public record PELTOptions
{
    /// <summary>
    /// The cost function used to measure the goodness-of-fit or homogeneity of a segment.
    /// Must implement <see cref="IPELTCostFunction"/>. The choice of cost function is critical
    /// and depends on the type of change being detected (e.g., change in mean, variance, rate).
    /// <para>Defaults to <see cref="L2CostFunction"/> (sensitive to changes in mean).</para>
    /// </summary>
    public IPELTCostFunction CostFunction { get; init; } = new L2CostFunction();

    /// <summary>
    /// The minimum number of data points required in any valid segment. Must be >= 1.
    /// <para>
    /// This prevents the detection of overly short segments. It should be chosen based on the
    /// minimum meaningful duration of a stable state in the data. A larger <c>MinSize</c>
    /// reduces sensitivity to very short-lived changes but improves robustness against noise.
    /// </para>
    /// <para>Defaults to 1.</para>
    /// </summary>
    public int MinSize { get; init; } = 1;

    /// <summary>
    /// The step size (or jump interval) for evaluating candidate change points. Must be >= 1.
    /// <para>
    /// <c>Jump = 1</c> corresponds to the exact PELT algorithm, evaluating all valid previous change points.
    /// </para>
    /// <para>
    /// <c>Jump > 1</c> introduces an approximation by only checking potential previous change points
    /// at intervals of <c>Jump</c> (within the admissible set). This can significantly speed up the algorithm,
    /// especially for long signals or complex cost functions, but may miss the true optimal segmentation.
    /// It's a trade-off between speed and exactness.
    /// </para>
    /// <para>Defaults to 1 (exact PELT).</para>
    /// </summary>
    public int Jump { get; init; } = 1;
}