using System.Numerics;

namespace SignalSharp.Statistics;

using System.Collections.Generic;

/// <summary>
/// Provides a set of statistical functions for numerical data processing.
/// </summary>
public static class StatisticalFunctions
{
    /// <summary>
    /// Calculates the mean (average) of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The mean of the values.</returns>
    /// <example>
    /// <code>
    /// var values = new[] { 1.0, 2.0, 3.0 };
    /// var mean = StatisticalFunctions.Mean(values);
    /// Console.WriteLine(mean); // Output: 2.0
    /// </code>
    /// </example>
    public static T Mean<T>(ReadOnlySpan<T> values) where T : INumber<T>
    {
        var sum = T.Zero;
        var count = values.Length;

        for (var i = 0; i < count; i++)
        {
            sum += values[i];
        }

        return sum / T.CreateChecked(count);
    }

    /// <summary>
    /// Calculates the variance of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The variance of the values.</returns>
    /// <remarks>
    /// Variance measures the dispersion of a set of values from their mean.
    /// </remarks>
    public static T Variance<T>(ReadOnlySpan<T> values) where T : INumber<T>
    {
        var mean = Mean(values);
        var varianceSum = T.Zero;
        var count = values.Length;

        for (var i = 0; i < count; i++)
        {
            var diff = values[i] - mean;
            varianceSum += diff * diff;
        }

        return varianceSum / T.CreateChecked(count);
    }

    /// <summary>
    /// Calculates the standard deviation of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The standard deviation of the values.</returns>
    /// <remarks>
    /// Standard deviation is the square root of the variance and provides a measure of the spread of values.
    /// </remarks>
    public static T StandardDeviation<T>(ReadOnlySpan<T> values) where T : INumber<T>, IRootFunctions<T>
    {
        return T.Sqrt(Variance(values));
    }

    /// <summary>
    /// Normalizes a set of values to the range [0, 1].
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The normalized values.</returns>
    /// <remarks>
    /// Normalization scales the values such that the minimum value becomes 0 and the maximum value becomes 1.
    /// </remarks>
    public static IEnumerable<T> Normalize<T>(ReadOnlySpan<T> values) where T : INumber<T>, IRootFunctions<T>
    {
        var (min, max) = MinMax(values);
        var count = values.Length;
    
        var result = new T[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = (values[i] - min) / (max - min);
        }

        return result;
    }

    /// <summary>
    /// Performs Z-score normalization on a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The Z-score normalized values.</returns>
    /// <remarks>
    /// Z-score normalization transforms the data such that it has a mean of 0 and a standard deviation of 1.
    /// </remarks>
    public static IEnumerable<T> ZScoreNormalization<T>(ReadOnlySpan<T> values) where T : INumber<T>, IRootFunctions<T>
    {
        var mean = Mean(values);
        var stdDev = StandardDeviation(values);
        var count = values.Length;
        
        var result = new T[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = (values[i] - mean) / stdDev;
        }

        return result;
    }

    /// <summary>
    /// Scales a set of values to the range [0, 1] using min-max scaling.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The min-max scaled values.</returns>
    /// <remarks>
    /// Min-max scaling linearly transforms the data such that the minimum value becomes 0 and the maximum value becomes 1.
    /// </remarks>
    public static IEnumerable<T> MinMaxScaling<T>(ReadOnlySpan<T> values) where T : INumber<T>
    {
        var (min, max) = MinMax(values);
        var range = max - min;
        var count = values.Length;
        
        var result = new T[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = (values[i] - min) / range;
        }

        return result;
    }
    
    /// <summary>
    /// Calculates the skewness of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The skewness of the values.</returns>
    /// <remarks>
    /// Skewness measures the asymmetry of the value distribution relative to the mean. A positive skewness indicates a distribution with a tail on the right side, while a negative skewness indicates a distribution with a tail on the left side.
    /// </remarks>
    /// <example>
    /// <code>
    /// var values = new[] { 1.0, 2.0, 2.0, 3.0, 4.0 };
    /// var skewness = StatisticalFunctions.Skewness(values);
    /// Console.WriteLine(skewness); // Output: 0.5657196
    /// </code>
    /// </example>
    public static T Skewness<T>(ReadOnlySpan<T> values) where T : INumber<T>, IRootFunctions<T>
    {
        var mean = Mean(values);
        var stdDev = StandardDeviation(values);
        var n = values.Length;
        var skewnessSum = T.Zero;

        for (var i = 0; i < n; i++)
        {
            var diff = (values[i] - mean) / stdDev;
            
            skewnessSum += diff * diff * diff;
        }

        return T.CreateChecked(n) * skewnessSum / (T.CreateChecked(n - 1) * T.CreateChecked(n - 2));
    }

    /// <summary>
    /// Calculates the kurtosis of a set of values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The kurtosis of the values.</returns>
    /// <remarks>
    /// Kurtosis measures the tailedness of the value distribution. The returned value is excess kurtosis, which is kurtosis minus 3. A high kurtosis indicates a distribution with heavy tails and a sharp peak, while a low kurtosis indicates a distribution with light tails and a flat peak.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the number of values is less than 4 or the standard deviation is zero.</exception>
    /// <example>
    /// <code>
    /// var values = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
    /// var kurtosis = StatisticalFunctions.Kurtosis(values);
    /// Console.WriteLine(kurtosis); // Output: -1.3
    /// </code>
    /// </example>
    public static T Kurtosis<T>(ReadOnlySpan<T> values) where T : INumber<T>, IRootFunctions<T>, IPowerFunctions<T>
    {
        if (values.Length < 4)
        {
            throw new ArgumentException("Excess kurtosis requires at least four data points.", nameof(values));
        }

        var mean = Mean(values);
        var stdDev = StandardDeviation(values);
        if (stdDev == T.Zero)
        {
            throw new ArgumentException("Standard deviation is zero, cannot compute kurtosis.", nameof(values));
        }

        var n = T.CreateChecked(values.Length);
        var sum = T.Zero;
        
        foreach (var value in values)
        {
            var diff = value - mean;
            sum += diff * diff * diff * diff;
        }

        var kurtosis = sum / (n * stdDev * stdDev * stdDev * stdDev);
        
        return T.CreateChecked(kurtosis) - T.CreateChecked(3.0);
    }

    private static (T Min, T Max) MinMax<T>(ReadOnlySpan<T> values) where T : INumber<T>
    {
        if (values.Length == 0)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }

        var min = values[0];
        var max = values[0];

        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] < min) min = values[i];
            if (values[i] > max) max = values[i];
        }

        return (min, max);
    }
}