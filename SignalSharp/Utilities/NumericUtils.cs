using System.Numerics;
using SignalSharp.Common;

namespace SignalSharp.Utilities;

/// <summary>
/// Provides utility methods for common numerical operations with generic number support.
/// </summary>
/// <remarks>
/// This utility class implements type-safe numerical operations using the generic math capabilities
/// provided by <see cref="INumber{T}"/>. It offers consistent ways to handle various numeric
/// comparisons, equality checks, and rounding operations across different numeric types.
/// </remarks>
public static class NumericUtils
{
    #region Absolute Comparison

    /// <summary>
    /// Checks if two numeric values are approximately equal using an absolute tolerance (epsilon).
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <param name="absoluteEpsilon">The maximum absolute difference allowed while considering the values equal (must be non-negative).</param>
    /// <returns>True if the absolute difference between <paramref name="a"/> and <paramref name="b"/> is less than or equal to <paramref name="absoluteEpsilon"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="absoluteEpsilon"/> is negative.</exception>
    /// <remarks>
    /// Compares using `Abs(a - b) <= absoluteEpsilon`.
    /// This method uses a fixed absolute tolerance. It may not be suitable for comparing numbers of vastly different magnitudes
    /// or numbers very close to zero if the epsilon is relatively large.
    /// Consider using <see cref="AreApproximatelyEqualRelative{T}(T, T, T, T)"/> for a more robust comparison, especially for floating-point types.
    /// </remarks>
    public static bool AreApproximatelyEqual<T>(T a, T b, T absoluteEpsilon)
        where T : INumber<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(absoluteEpsilon, nameof(absoluteEpsilon));
        return T.Abs(a - b) <= absoluteEpsilon; // Use <= for inclusivity at the boundary
    }

    /// <summary>
    /// Checks if two numeric values are approximately equal using the default absolute tolerance (epsilon) for the type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>True if the absolute difference between <paramref name="a"/> and <paramref name="b"/> is less than or equal to the default absolute epsilon.</returns>
    /// <remarks>
    /// Uses a type-specific default absolute epsilon value obtained via <see cref="GetDefaultEpsilon{T}"/>.
    /// See the remarks on <see cref="AreApproximatelyEqual{T}(T, T, T)"/> regarding the limitations of absolute tolerance.
    /// Consider using <see cref="AreApproximatelyEqualRelative{T}(T, T)"/> for potentially more robust comparisons.
    /// </remarks>
    public static bool AreApproximatelyEqual<T>(T a, T b)
        where T : INumber<T>
    {
        return AreApproximatelyEqual(a, b, GetDefaultEpsilon<T>());
    }

    /// <summary>
    /// Checks if a value is effectively zero using an absolute tolerance (epsilon).
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="absoluteEpsilon">The maximum absolute value to consider as effectively zero (must be non-negative).</param>
    /// <returns>True if the absolute value is less than or equal to <paramref name="absoluteEpsilon"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="absoluteEpsilon"/> is negative.</exception>
    /// <remarks>
    /// Compares using `Abs(value) <= absoluteEpsilon`.
    /// This method is useful for handling values that should be zero but might have
    /// small non-zero values due to arithmetic imprecisions or intentional thresholds.
    /// This performs an absolute tolerance check against zero.
    /// </remarks>
    public static bool IsEffectivelyZero<T>(T value, T absoluteEpsilon)
        where T : INumber<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(absoluteEpsilon, nameof(absoluteEpsilon));
        return T.Abs(value) <= absoluteEpsilon; // Use <= for inclusivity
    }

    /// <summary>
    /// Checks if a value is effectively zero using the default absolute tolerance (epsilon) for the type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the absolute value is less than or equal to the default absolute epsilon.</returns>
    /// <remarks>
    /// Uses a type-specific default absolute epsilon value obtained via <see cref="GetDefaultEpsilon{T}"/>.
    /// See remarks on <see cref="IsEffectivelyZero{T}(T, T)"/>.
    /// </remarks>
    public static bool IsEffectivelyZero<T>(T value)
        where T : INumber<T>
    {
        return IsEffectivelyZero(value, GetDefaultEpsilon<T>());
    }

    #endregion


    #region Relative Comparison (Recommended for Floating Point)

    /// <summary>
    /// Checks if two floating-point values are approximately equal using a combined relative and absolute tolerance.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IFloatingPoint{T}"/>.</typeparam>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <param name="relativeTolerance">The maximum allowed relative difference (must be non-negative).</param>
    /// <param name="absoluteTolerance">The maximum allowed absolute difference (must be non-negative).</param>
    /// <returns>True if the values are close enough based on the specified tolerances.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="relativeTolerance"/> or <paramref name="absoluteTolerance"/> is negative.</exception>
    /// <remarks>
    /// Implements the check `Abs(a - b) <= absoluteTolerance + relativeTolerance * Abs(b)`.
    /// This approach is generally more robust than pure absolute or pure relative tolerance checks,
    /// handling both comparisons near zero (where absolute tolerance dominates) and comparisons
    /// of large numbers (where relative tolerance dominates).
    /// This is often the preferred method for comparing floating-point numbers for equality.
    /// Handles NaN correctly (comparison returns false). Handles Infinity correctly (Infinity == Infinity).
    /// </remarks>
    public static bool AreApproximatelyEqualRelative<T>(T a, T b, T relativeTolerance, T absoluteTolerance)
        where T : INumber<T>, IFloatingPoint<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(relativeTolerance, nameof(relativeTolerance));
        ArgumentOutOfRangeException.ThrowIfNegative(absoluteTolerance, nameof(absoluteTolerance));

        if (a == b)
        {
            return true;
        }

        if (T.IsNaN(a) || T.IsNaN(b))
        {
            return false;
        }

        return T.Abs(a - b) <= absoluteTolerance + relativeTolerance * T.Abs(b);
    }

    /// <summary>
    /// Checks if two floating-point values are approximately equal using default relative and absolute tolerances for the type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IFloatingPointIeee754{T}"/>.</typeparam>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>True if the values are close enough based on default tolerances.</returns>
    /// <remarks>
    /// Uses default tolerances obtained from <see cref="GetDefaultRelativeEpsilon{T}"/> and <see cref="GetDefaultEpsilon{T}"/>.
    /// This is often the preferred method for comparing floating-point numbers for equality.
    /// See remarks on <see cref="AreApproximatelyEqualRelative{T}(T, T, T, T)"/>.
    /// </remarks>
    public static bool AreApproximatelyEqualRelative<T>(T a, T b)
        where T : INumber<T>, IFloatingPointIeee754<T>
    {
        return AreApproximatelyEqualRelative(a, b, GetDefaultRelativeEpsilon<T>(), GetDefaultEpsilon<T>());
    }

    #endregion

    /// <summary>
    /// Checks if a floating-point value is effectively an integer within the specified epsilon.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements both <see cref="INumber{T}"/> and <see cref="IFloatingPoint{T}"/>.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="epsilon">The maximum allowed difference from a whole number.</param>
    /// <returns>True if the value is within <paramref name="epsilon"/> of an integer.</returns>
    /// <remarks>
    /// This is useful for determining if a floating-point value should be treated as an integer,
    /// accounting for potential rounding errors in floating-point arithmetic.
    /// </remarks>
    public static bool IsEffectivelyInteger<T>(T value, T epsilon)
        where T : INumber<T>, IFloatingPoint<T>
    {
        return T.Abs(value - T.Round(value)) < epsilon;
    }

    /// <summary>
    /// Checks if a floating-point value is effectively an integer using the default epsilon for the type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements both <see cref="INumber{T}"/> and <see cref="IFloatingPoint{T}"/>.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is within the default epsilon of an integer.</returns>
    /// <remarks>
    /// Uses a type-specific default epsilon value appropriate for the numeric type T.
    /// </remarks>
    public static bool IsEffectivelyInteger<T>(T value)
        where T : INumber<T>, IFloatingPoint<T>
    {
        return IsEffectivelyInteger(value, GetDefaultEpsilon<T>());
    }

    /// <summary>
    /// Gets the appropriate default epsilon value for the numeric type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <returns>A type-appropriate default epsilon value.</returns>
    /// <remarks>
    /// Provides type-specific epsilon values optimized for each numeric type's precision characteristics.
    /// Falls back to a reasonable default for other numeric types.
    /// </remarks>
    public static T GetDefaultEpsilon<T>()
        where T : INumber<T>
    {
        if (typeof(T) == typeof(double))
        {
            return (T)Convert.ChangeType(Constants.DefaultEpsilon, typeof(T));
        }

        if (typeof(T) == typeof(float))
        {
            return (T)Convert.ChangeType(Constants.FloatDefaultEpsilon, typeof(T));
        }

        if (typeof(T) == typeof(decimal))
        {
            return (T)Convert.ChangeType(Constants.DecimalDefaultEpsilon, typeof(T));
        }

        return T.CreateSaturating(0.00001);
    }

    /// <summary>
    /// Gets the appropriate strict epsilon value for the numeric type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <returns>A type-appropriate strict epsilon value for high-precision operations.</returns>
    /// <remarks>
    /// Provides stricter tolerance values for operations requiring higher precision.
    /// Use this for numerically sensitive calculations like model fitting or matrix operations.
    /// </remarks>
    public static T GetStrictEpsilon<T>()
        where T : INumber<T>
    {
        if (typeof(T) == typeof(double))
        {
            return (T)Convert.ChangeType(Constants.StrictEpsilon, typeof(T));
        }

        if (typeof(T) == typeof(float))
        {
            return (T)Convert.ChangeType(Constants.FloatStrictEpsilon, typeof(T));
        }

        if (typeof(T) == typeof(decimal))
        {
            return (T)Convert.ChangeType(Constants.DecimalStrictEpsilon, typeof(T));
        }

        return T.CreateSaturating(0.000001);
    }

    /// <summary>
    /// Gets the appropriate variance epsilon value for the numeric type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <returns>A type-appropriate epsilon value for variance calculations.</returns>
    /// <remarks>
    /// Provides epsilon values specifically calibrated for variance calculations
    /// to prevent division by zero and logarithm of zero errors in statistical models.
    /// </remarks>
    public static T GetVarianceEpsilon<T>()
        where T : INumber<T>
    {
        return (T)Convert.ChangeType(Constants.VarianceEpsilon, typeof(T));
    }

    /// <summary>
    /// Gets the appropriate default relative epsilon value for floating-point types.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IFloatingPointIeee754{T}"/>.</typeparam>
    /// <returns>A type-appropriate default relative epsilon value.</returns>
    /// <remarks>
    /// Provides type-specific relative epsilon values for use in relative comparisons.
    /// Returns T.Zero for non-floating point types or if relative tolerance is not applicable.
    /// </remarks>
    public static T GetDefaultRelativeEpsilon<T>()
        where T : INumber<T>, IFloatingPointIeee754<T>
    {
        if (typeof(T) == typeof(double))
        {
            return (T)Convert.ChangeType(Constants.DefaultRelativeEpsilon, typeof(T));
        }

        if (typeof(T) == typeof(float))
        {
            return (T)Convert.ChangeType(Constants.FloatDefaultRelativeEpsilon, typeof(T));
        }

        if (typeof(T) == typeof(decimal))
        {
            return (T)Convert.ChangeType(Constants.DecimalDefaultRelativeEpsilon, typeof(T));
        }

        return T.Epsilon;
    }
}
