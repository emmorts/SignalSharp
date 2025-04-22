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
    /// <summary>
    /// Checks if two numeric values are approximately equal within the specified epsilon.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <param name="epsilon">The maximum difference allowed while considering the values equal.</param>
    /// <returns>True if the absolute difference between <paramref name="a"/> and <paramref name="b"/> is less than <paramref name="epsilon"/>.</returns>
    /// <remarks>
    /// This method accounts for floating-point imprecision by considering two values equal
    /// if their difference is smaller than the specified tolerance.
    /// </remarks>
    public static bool AreApproximatelyEqual<T>(T a, T b, T epsilon) where T : INumber<T>
    {
        return T.Abs(a - b) < epsilon;
    }
    
    /// <summary>
    /// Checks if two numeric values are approximately equal within the default epsilon for the type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>True if the absolute difference between <paramref name="a"/> and <paramref name="b"/> is less than the default epsilon.</returns>
    /// <remarks>
    /// Uses a type-specific default epsilon value appropriate for the numeric type T.
    /// </remarks>
    public static bool AreApproximatelyEqual<T>(T a, T b) where T : INumber<T>
    {
        return AreApproximatelyEqual(a, b, GetDefaultEpsilon<T>());
    }
    
    /// <summary>
    /// Checks if a value is effectively zero within the specified epsilon.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="epsilon">The maximum absolute value to consider as effectively zero.</param>
    /// <returns>True if the absolute value is less than <paramref name="epsilon"/>.</returns>
    /// <remarks>
    /// This method is useful for handling values that should be zero but might have
    /// small non-zero values due to floating-point arithmetic imprecisions.
    /// </remarks>
    public static bool IsEffectivelyZero<T>(T value, T epsilon) where T : INumber<T>
    {
        return T.Abs(value) < epsilon;
    }
    
    /// <summary>
    /// Checks if a value is effectively zero within the default epsilon for the type.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref="INumber{T}"/>.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the absolute value is less than the default epsilon.</returns>
    /// <remarks>
    /// Uses a type-specific default epsilon value appropriate for the numeric type T.
    /// </remarks>
    public static bool IsEffectivelyZero<T>(T value) where T : INumber<T>
    {
        return IsEffectivelyZero(value, GetDefaultEpsilon<T>());
    }
    
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
    public static bool IsEffectivelyInteger<T>(T value, T epsilon) where T : INumber<T>, IFloatingPoint<T>
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
    public static bool IsEffectivelyInteger<T>(T value) where T : INumber<T>, IFloatingPoint<T>
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
    public static T GetDefaultEpsilon<T>() where T : INumber<T>
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
    public static T GetStrictEpsilon<T>() where T : INumber<T>
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
    public static T GetVarianceEpsilon<T>() where T : INumber<T>
    {
        return (T)Convert.ChangeType(Constants.VarianceEpsilon, typeof(T));
    }
}