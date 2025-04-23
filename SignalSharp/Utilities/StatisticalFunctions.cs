using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SignalSharp.Utilities;

/// <summary>
/// Provides a set of statistical functions for numerical data processing,
/// automatically leveraging SIMD acceleration for supported types where available.
/// </summary>
/// <remarks>
/// This class offers optimized implementations for common statistical calculations.
/// It primarily exposes generic methods working with <see cref="INumber{T}"/>.
/// Internally, it dispatches to SIMD-optimized code for types like <see cref="double"/>
/// when hardware acceleration is enabled, falling back to generic implementations otherwise.
/// </remarks>
public static partial class StatisticalFunctions
{
    /// <summary>
    /// Calculates the mean (average) of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The mean of the values. Returns <see cref="T.Zero"/> if the input span is empty.</returns>
    /// <example>
    /// <code>
    /// var doubleValues = new[] { 1.0, 2.0, 3.0 };
    /// var doubleMean = StatisticalFunctions.Mean(doubleValues.AsSpan()); // Uses SIMD if available
    /// Console.WriteLine(doubleMean); // Output: 2.0
    ///
    /// var floatValues = new[] { 1.0f, 2.0f, 3.0f };
    /// var floatMean = StatisticalFunctions.Mean(floatValues.AsSpan()); // Uses SIMD if available
    /// Console.WriteLine(floatMean); // Output: 2.0f
    ///
    /// var intValues = new[] { 1, 2, 3 };
    /// var intMean = StatisticalFunctions.Mean(intValues.AsSpan()); // Uses generic implementation
    /// Console.WriteLine(intMean); // Output: 2 (integer division)
    /// </code>
    /// </example>
    public static T Mean<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>
    {
        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleSpan = MemoryMarshal.Cast<T, double>(values);
            double result = MeanDoubleSimd(doubleSpan);
            return Unsafe.As<double, T>(ref result);
        }

        if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<T, float>(values);
            float result = MeanFloatSimd(floatSpan);
            return Unsafe.As<float, T>(ref result);
        }

        return MeanGeneric(values);
    }

    /// <summary>
    /// Calculates the median of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <param name="useQuickSelect">A flag indicating whether to use the QuickSelect algorithm (potentially faster, modifies input array copy) or sort-based method.</param>
    /// <returns>The median of the values.</returns>
    /// <exception cref="ArgumentException">Thrown when the values span is empty.</exception>
    /// <example>
    /// <code>
    /// var values = new[] { 1.0, 5.0, 2.0, 4.0, 3.0 };
    /// var median = StatisticalFunctions.Median(values.AsSpan());
    /// Console.WriteLine(median); // Output: 3.0
    /// </code>
    /// </example>
    public static T Median<T>(ReadOnlySpan<T> values, bool useQuickSelect = false)
        where T : INumber<T>
    {
        return MedianImplementation(values, useQuickSelect);
    }

    /// <summary>
    /// Calculates the population variance of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The population variance of the values. Returns <see cref="T.Zero"/> if the input span has 0 or 1 elements.</returns>
    /// <remarks>
    /// Population variance uses division by N (number of elements).
    /// Variance measures the dispersion of a set of values from their mean.
    /// </remarks>
    public static T Variance<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>
    {
        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleSpan = MemoryMarshal.Cast<T, double>(values);
            double result = VarianceDoubleSimd(doubleSpan);
            return Unsafe.As<double, T>(ref result);
        }

        if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<T, float>(values);
            float result = VarianceFloatSimd(floatSpan);
            return Unsafe.As<float, T>(ref result);
        }

        return VarianceGeneric(values);
    }

    /// <summary>
    /// Calculates the population standard deviation of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/> and <see cref="IRootFunctions{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The population standard deviation of the values. Returns <see cref="T.Zero"/> if the input span has 0 or 1 elements.</returns>
    /// <remarks>
    /// Population standard deviation is the square root of the population variance.
    /// Standard deviation provides a measure of the spread of values.
    /// </remarks>
    public static T StandardDeviation<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>, IRootFunctions<T>
    {
        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleSpan = MemoryMarshal.Cast<T, double>(values);
            double result = StandardDeviationDoubleSimd(doubleSpan);
            return Unsafe.As<double, T>(ref result);
        }

        if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<T, float>(values);
            float result = StandardDeviationFloatSimd(floatSpan);
            return Unsafe.As<float, T>(ref result);
        }

        return StandardDeviationGeneric(values);
    }

    /// <summary>
    /// Calculates the minimum value in a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The minimum value in the set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input sequence is empty.</exception>
    public static T Min<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        // min/max typically don't benefit hugely from SIMD in the same way as arithmetic loops
        // unless the dataset is extremely large and branch misprediction becomes significant.
        // keeping it generic for simplicity unless profiling shows a bottleneck.
        return MinGeneric(values);
    }

    /// <summary>
    /// Calculates the maximum value in a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The maximum value in the set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input sequence is empty.</exception>
    public static T Max<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        // see comment in Min<T>
        return MaxGeneric(values);
    }

    /// <summary>
    /// Normalizes a set of values to the range [0, 1].
    /// Allocates and returns a new array.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/>.</typeparam>
    /// <param name="values">The source values to normalize.</param>
    /// <returns>A new array of type T containing the normalized values. If all input values are the same, returns an array filled with <see cref="T.Zero"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input sequence is empty.</exception>
    /// <remarks>
    /// Normalization scales the values such that the minimum value becomes 0 and the maximum value becomes 1.
    /// </remarks>
    public static T[] Normalize<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>
    {
        var count = values.Length;
        if (count == 0)
        {
            return [];
        }

        var result = new T[count];
        Normalize(values, result.AsSpan());
        return result;
    }

    /// <summary>
    /// Normalizes a set of values to the range [0, 1], writing the result into a pre-allocated destination span.
    /// This method avoids internal allocations.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/>.</typeparam>
    /// <param name="values">The source values to normalize.</param>
    /// <param name="destination">The span to write the normalized values into. Must have the same length as <paramref name="values"/>.</param>
    /// <exception cref="ArgumentException">Thrown if destination length does not match values length.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the input sequence <paramref name="values"/> is empty.</exception>
    /// <remarks>
    /// Normalization scales the values such that the minimum value becomes 0 and the maximum value becomes 1.
    /// If all input values are the same, the destination span is filled with <see cref="T.Zero"/>.
    /// Requires values to be non-empty.
    /// </remarks>
    public static void Normalize<T>(ReadOnlySpan<T> values, Span<T> destination)
        where T : struct, INumber<T>
    {
        int count = values.Length;
        if (destination.Length != count)
        {
            throw new ArgumentException("Destination span must have the same length as the values span.", nameof(destination));
        }

        if (count == 0)
        {
            return;
        }

        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleValues = MemoryMarshal.Cast<T, double>(values);
            Span<double> doubleDestination = MemoryMarshal.Cast<T, double>(destination);
            NormalizeDoubleSimd(doubleValues, doubleDestination);
        }
        else if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatValues = MemoryMarshal.Cast<T, float>(values);
            Span<float> floatDestination = MemoryMarshal.Cast<T, float>(destination);
            NormalizeFloatSimd(floatValues, floatDestination);
        }
        else
        {
            NormalizeGeneric(values, destination);
        }
    }

    /// <summary>
    /// Performs Z-score normalization on a set of values.
    /// Allocates and returns a new array.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/> and <see cref="IRootFunctions{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>A new array containing the Z-score normalized values. If the standard deviation is effectively zero, returns an array filled with <see cref="T.Zero"/>.</returns>
    /// <remarks>
    /// Z-score normalization transforms the data such that it has a mean of 0 and a population standard deviation of 1.
    /// Z = (value - mean) / stdDev
    /// </remarks>
    public static T[] ZScoreNormalization<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>, IRootFunctions<T>
    {
        var count = values.Length;
        if (count == 0)
        {
            return [];
        }

        var result = new T[count];
        ZScoreNormalization(values, result.AsSpan());
        return result;
    }

    /// <summary>
    /// Performs Z-score normalization on a set of values, writing the result into a pre-allocated destination span.
    /// This method avoids internal allocations.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/> and <see cref="IRootFunctions{T}"/>.</typeparam>
    /// <param name="values">The source values to normalize.</param>
    /// <param name="destination">The span to write the normalized values into. Must have the same length as <paramref name="values"/>.</param>
    /// <exception cref="ArgumentException">Thrown if destination length does not match values length.</exception>
    /// <remarks>
    /// Z-score normalization transforms the data such that it has a mean of 0 and a population standard deviation of 1.
    /// Z = (value - mean) / stdDev. If the standard deviation is effectively zero, the destination span is filled with <see cref="T.Zero"/>.
    /// </remarks>
    public static void ZScoreNormalization<T>(ReadOnlySpan<T> values, Span<T> destination)
        where T : struct, INumber<T>, IRootFunctions<T>
    {
        int count = values.Length;
        if (destination.Length != count)
        {
            throw new ArgumentException("Destination span must have the same length as the values span.", nameof(destination));
        }

        if (count == 0)
        {
            return;
        }

        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleValues = MemoryMarshal.Cast<T, double>(values);
            Span<double> doubleDestination = MemoryMarshal.Cast<T, double>(destination);
            ZScoreNormalizationDoubleSimd(doubleValues, doubleDestination);
        }
        else if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatValues = MemoryMarshal.Cast<T, float>(values);
            Span<float> floatDestination = MemoryMarshal.Cast<T, float>(destination);
            ZScoreNormalizationFloatSimd(floatValues, floatDestination);
        }
        else
        {
            ZScoreNormalizationGeneric(values, destination);
        }
    }

    /// <summary>
    /// Calculates a sample skewness estimator (unbiased for normal distribution, G1).
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/> and <see cref="IRootFunctions{T}"/>.</typeparam>
    /// <param name="values">The sample set of values.</param>
    /// <returns>The sample skewness (G1) of the values. Returns 0 if variance is effectively zero.</returns>
    /// <exception cref="ArgumentException">Thrown when the number of values is less than 3.</exception>
    /// <remarks>
    /// Skewness measures the asymmetry of the value distribution relative to the mean.
    /// Uses the adjusted Fisher-Pearson standardized moment coefficient (G1).
    /// Formula: `[n / ((n-1)*(n-2))] * Sum[(x_i - mean) / stdDev]^3`
    /// </remarks>
    public static T Skewness<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>, IRootFunctions<T>
    {
        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleSpan = MemoryMarshal.Cast<T, double>(values);
            double result = SkewnessDoubleSimd(doubleSpan);
            return Unsafe.As<double, T>(ref result);
        }

        if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<T, float>(values);
            float result = SkewnessFloatSimd(floatSpan);
            return Unsafe.As<float, T>(ref result);
        }

        return SkewnessGeneric(values);
    }

    /// <summary>
    /// Calculates the population excess kurtosis.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/> and <see cref="IRootFunctions{T}"/>.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The population excess kurtosis of the values.</returns>
    /// <exception cref="ArgumentException">Thrown when the number of values is less than 4 or if the population variance is effectively zero.</exception>
    /// <remarks>
    /// Population excess kurtosis measures the "tailedness" relative to a normal distribution (Kurtosis - 3).
    /// Formula: `(Sum[(x_i - mean)^4] / n) / variance^2 - 3`.
    /// </remarks>
    public static T PopulationExcessKurtosis<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>, IRootFunctions<T>
    {
        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleSpan = MemoryMarshal.Cast<T, double>(values);
            double result = PopulationExcessKurtosisDoubleSimd(doubleSpan);
            return Unsafe.As<double, T>(ref result);
        }

        if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<T, float>(values);
            float result = PopulationExcessKurtosisFloatSimd(floatSpan);
            return Unsafe.As<float, T>(ref result);
        }

        return PopulationExcessKurtosisGeneric(values);
    }

    /// <summary>
    /// Calculates the sample excess kurtosis (G2), an unbiased estimator for normal distributions.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values, implementing <see cref="INumber{T}"/> and <see cref="IRootFunctions{T}"/>.</typeparam>
    /// <param name="values">The sample set of values.</param>
    /// <returns>The sample excess kurtosis (G2) of the values.</returns>
    /// <exception cref="ArgumentException">Thrown when the number of values is less than 4 or if the population standard deviation is effectively zero.</exception>
    /// <remarks>
    /// Sample excess kurtosis (G2) is an unbiased estimator for data from a normal distribution.
    /// Formula involves terms like `n(n+1)/((n-1)(n-2)(n-3)) * Sum[((x_i-mean)/s)^4]` and `3(n-1)^2/((n-2)(n-3))`, where `s` is the *sample* standard deviation.
    /// The implementation uses population standard deviation internally and applies adjustment factors. Check implementation for details.
    /// </remarks>
    public static T SampleKurtosisG2<T>(ReadOnlySpan<T> values)
        where T : struct, INumber<T>, IRootFunctions<T>
    {
        if (typeof(T) == typeof(double) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<double> doubleSpan = MemoryMarshal.Cast<T, double>(values);
            double result = SampleKurtosisG2DoubleSimd(doubleSpan);
            return Unsafe.As<double, T>(ref result);
        }

        if (typeof(T) == typeof(float) && Vector.IsHardwareAccelerated)
        {
            ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<T, float>(values);
            float result = SampleKurtosisG2FloatSimd(floatSpan);
            return Unsafe.As<float, T>(ref result);
        }

        return SampleKurtosisG2Generic(values);
    }
}
