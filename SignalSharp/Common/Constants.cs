namespace SignalSharp.Common;

/// <summary>
/// Contains common constants used throughout the SignalSharp library.
/// </summary>
/// <remarks>
/// This class centralizes numeric tolerance values and other constants to ensure
/// consistency across the library. Different epsilon values are provided for
/// various use cases and numeric types to balance precision and performance.
/// </remarks>
public static class Constants
{
    /// <summary>
    /// Default tolerance for double-precision floating-point comparisons.
    /// </summary>
    /// <remarks>
    /// Used for general numeric stability checks and equality comparisons.
    /// This is a good balance between precision and numerical stability for most cases.
    /// </remarks>
    public const double DefaultEpsilon = 1e-9;

    /// <summary>
    /// Stricter tolerance for double-precision calculations requiring higher precision.
    /// </summary>
    /// <remarks>
    /// Used for model fitting, matrix operations, and statistical calculations
    /// where higher precision is critical. Recommended for AR model fitting and
    /// other sensitive numerical procedures.
    /// </remarks>
    public const double StrictEpsilon = 1e-12;

    /// <summary>
    /// Default relative tolerance for double-precision floating-point comparisons.
    /// </summary>
    /// <remarks>
    /// Used in relative comparison checks: `abs(a - b) <= absoluteTolerance + relativeTolerance * abs(b)`.
    /// A typical value balances precision needs without being overly strict.
    /// </remarks>
    public const double DefaultRelativeEpsilon = 1e-7;

    /// <summary>
    /// Tolerance for variance checks in statistical calculations.
    /// </summary>
    /// <remarks>
    /// Used to prevent division by zero and log(0) issues in statistical models.
    /// This value helps avoid numerical instability when dealing with very small variances.
    /// </remarks>
    public const double VarianceEpsilon = 1e-10;

    /// <summary>
    /// Default tolerance for single-precision floating-point comparisons.
    /// </summary>
    /// <remarks>
    /// Used when working with single-precision (float) data.
    /// Less strict than double precision values due to the inherent lower precision of float.
    /// </remarks>
    public const float FloatDefaultEpsilon = 1e-6f;

    /// <summary>
    /// Stricter tolerance for single-precision calculations requiring higher precision.
    /// </summary>
    /// <remarks>
    /// Used for sensitive operations with single-precision (float) values.
    /// Still accounts for the lower precision of float compared to double.
    /// </remarks>
    public const float FloatStrictEpsilon = 1e-7f;

    /// <summary>
    /// Default relative tolerance for single-precision floating-point comparisons.
    /// </summary>
    /// <remarks>
    /// Used in relative comparison checks for float types. Adjusted for lower precision.
    /// </remarks>
    public const float FloatDefaultRelativeEpsilon = 1e-5f;

    /// <summary>
    /// Default tolerance for decimal comparisons.
    /// </summary>
    /// <remarks>
    /// Used when working with decimal data, which has higher precision for
    /// representing base-10 numbers than double but may have different
    /// performance characteristics.
    /// </remarks>
    public const decimal DecimalDefaultEpsilon = 1e-9m;

    /// <summary>
    /// Stricter tolerance for decimal calculations requiring higher precision.
    /// </summary>
    /// <remarks>
    /// Used for sensitive operations with decimal values where
    /// maximum precision is required.
    /// </remarks>
    public const decimal DecimalStrictEpsilon = 1e-12m;

    /// <summary>
    /// Default relative tolerance for decimal comparisons.
    /// </summary>
    /// <remarks>
    /// Used in relative comparison checks for decimal types.
    /// </remarks>
    public const decimal DecimalDefaultRelativeEpsilon = 1e-8m;
}
