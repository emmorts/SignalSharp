using System.Numerics;

namespace SignalSharp.Utilities;

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
    public static T Mean<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>
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
    /// Calculates the median of a set of values.
    /// </summary>
    /// <param name="values">The set of values.</param>
    /// <param name="useQuickSelect">A flag indicating whether to use the QuickSelect algorithm to compute the median.</param>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <returns>The median of the values.</returns>
    /// <example>
    /// <code>
    /// var values = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
    /// var median = StatisticalFunctions.Median(values);
    /// Console.WriteLine(median); // Output: 3.0
    /// </code>
    /// </example>
    public static T Median<T>(ReadOnlySpan<T> values, bool useQuickSelect = false)
        where T : INumber<T>
    {
        var valuesArray = values.ToArray();
        
        return useQuickSelect ? QuickSelectMedian(valuesArray) : CalculateMedian(valuesArray);
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
    public static T Variance<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>
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
    public static T StandardDeviation<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>, IRootFunctions<T>
    {
        return T.Sqrt(Variance(values));
    }

    /// <summary>
    /// Calculates the minimum value in a set of values.
    /// </summary>
    /// <param name="values">The set of values.</param>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <returns>The minimum value in the set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input sequence is empty.</exception>
    public static T Min<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        if (values.Length == 0)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }

        var min = values[0];

        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] < min) min = values[i];
        }

        return min;
    }

    /// <summary>
    /// Calculates the maximum value in a set of values.
    /// </summary>
    /// <param name="values">The set of values.</param>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <returns>The maximum value in the set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input sequence is empty.</exception>
    public static T Max<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        if (values.Length == 0)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }

        var max = values[0];

        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] > max) max = values[i];
        }

        return max;
    }

    /// <summary>
    /// Normalizes a set of values to the range [0, 1].
    /// </summary>
    /// <typeparam name="T">The numeric type of the values.</typeparam>
    /// <param name="values">The set of values.</param>
    /// <returns>The normalized values.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input sequence is empty.</exception>
    /// <remarks>
    /// Normalization scales the values such that the minimum value becomes 0 and the maximum value becomes 1.
    /// </remarks>
    public static IEnumerable<T> Normalize<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>
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
    public static IEnumerable<T> ZScoreNormalization<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>, IRootFunctions<T>
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
    public static IEnumerable<T> MinMaxScaling<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>
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
    public static T Skewness<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>, IRootFunctions<T>
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
    public static T Kurtosis<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>, IRootFunctions<T>, IPowerFunctions<T>
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

    private static (T Min, T Max) MinMax<T>(ReadOnlySpan<T> values) 
        where T : INumber<T>
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
    
    /// <summary>
    /// Calculates the median of an array of values.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <returns>The median value.</returns>
    /// <exception cref="ArgumentException">Thrown when the values array is null or empty.</exception>
    private static T CalculateMedian<T>(T[] values)
        where T : INumber<T>
    {
        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("Values array must not be null or empty.", nameof(values));
        }

        Array.Sort(values);

        var middle = values.Length / 2;

        return values.Length % 2 == 0
            ? (values[middle - 1] + values[middle]) / T.CreateChecked(2.0)
            : values[middle];
    }
    
    /// <summary>
    /// Computes the median using the QuickSelect algorithm for a provided array.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <returns>The median value.</returns>
    /// <exception cref="ArgumentException">Thrown when the values array is null or the length is less than or equal to zero.</exception>
    private static T QuickSelectMedian<T>(T[] values)
        where T : INumber<T>
    {
        const int start = 0;
        
        var length = values.Length;
        
        if (values == null || length <= 0)
        {
            throw new ArgumentException("Values array must not be null or empty.", nameof(values));
        }

        var mid = length / 2;
        if (length % 2 == 0)
        {
            var mid1 = QuickSelect(values, start, length, mid);
            var mid2 = QuickSelect(values, start, length, mid - 1);
            
            return T.CreateChecked(0.5) * (mid1 + mid2);
        }

        return QuickSelect(values, start, length, mid);
    }

    /// <summary>
    /// Selects the k-th smallest element in a segment of the array using the QuickSelect algorithm.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <param name="start">The start index of the segment.</param>
    /// <param name="length">The length of the segment.</param>
    /// <param name="k">The k-th position to find.</param>
    /// <returns>The k-th smallest element.</returns>
    private static T QuickSelect<T>(T[] values, int start, int length, int k)
        where T : INumber<T>
    {
        while (true)
        {
            if (length == 1) return values[start];

            var pivotIndex = Partition(values, start, length);
            var leftLength = pivotIndex - start;

            if (leftLength == k) return values[pivotIndex];

            if (k < leftLength)
            {
                length = leftLength;
            }
            else
            {
                k -= leftLength + 1;
                start = pivotIndex + 1;
                length -= leftLength + 1;
            }
        }
    }

    /// <summary>
    /// Partitions the array segment around a pivot for the QuickSelect algorithm.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <param name="start">The start index of the segment.</param>
    /// <param name="length">The length of the segment.</param>
    /// <returns>The index of the pivot after partitioning.</returns>
    private static int Partition<T>(T[] values, int start, int length)
        where T : INumber<T>
    {
        var pivot = values[start];
        var left = start + 1;
        var right = start + length - 1;

        while (true)
        {
            while (left <= right && values[left] <= pivot) left++;
            while (left <= right && values[right] > pivot) right--;

            if (left > right) break;

            Swap(values, left, right);
        }

        Swap(values, start, right);

        return right;
    }

    /// <summary>
    /// Swaps two elements in an array.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <param name="a">The index of the first element.</param>
    /// <param name="b">The index of the second element.</param>
    private static void Swap<T>(T[] values, int a, int b)
    {
        (values[a], values[b]) = (values[b], values[a]);
    }
}