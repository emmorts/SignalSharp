namespace SignalSharp.Common.Models;

/// <summary>
/// Enumeration representing different padding modes for signal processing.
/// </summary>
public enum Padding
{
    /// <summary>
    /// The signal is not padded. 
    /// No additional values are added to the signal at the boundaries.
    /// </summary>
    None,

    /// <summary>
    /// The signal is padded by a specified constant value.
    /// This mode adds a constant value to the boundaries, useful for maintaining a fixed boundary condition.
    /// </summary>
    Constant,

    /// <summary>
    /// The signal is mirrored around the boundary points.
    /// Values at the boundary are reflected to the other side, creating a symmetric padding.
    /// This helps to minimize edge artifacts in some signal processing operations.
    /// </summary>
    Mirror,

    /// <summary>
    /// The signal is padded by replicating the first value at the lower boundary and the last value at the upper boundary.
    /// This creates a step-like extension at the boundaries, maintaining the edge value.
    /// </summary>
    Nearest,

    /// <summary>
    /// The signal is treated as periodic.
    /// The end of the signal wraps around to the start, making the signal repeat itself.
    /// This is useful for signals that are inherently periodic, ensuring continuity.
    /// </summary>
    Periodic
}
