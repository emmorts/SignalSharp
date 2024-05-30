using SignalSharp.Detection.PELT.Cost;

namespace SignalSharp.Detection.PELT.Models;

/// <summary>
/// Represents the configuration options for the Piecewise Linear Trend Change (PELT) algorithm.
/// </summary>
/// <remarks>
/// <para>
/// These options allow customization of the PELT algorithm's behavior, including the cost function used to 
/// evaluate segment quality, the minimum segment size, and the jump parameter for candidate change point evaluation.
/// </para>
///
/// <para>
/// Consider adjusting these options based on the characteristics of your data and the desired sensitivity 
/// of the change point detection.
/// </para>
/// </remarks>
public record PELTOptions
{
    /// <summary>
    /// The cost function to be used. The default is <see cref="L2CostFunction"/>.
    /// </summary>
    public IPELTCostFunction CostFunction { get; init; } = new L2CostFunction();
    
    /// <summary>
    /// The minimum number of data points required in a segment. The default value is 2.
    /// </summary>
    public int MinSize { get; init; } = 2;
    
    /// <summary>
    /// The step size for evaluating candidate change points. The default value is 5.
    /// </summary>
    /// <remarks>
    /// A larger jump value reduces the number of candidate change points, potentially speeding up the algorithm 
    /// at the cost of reduced sensitivity.
    /// </remarks>
    public int Jump { get; init; } = 5;
}