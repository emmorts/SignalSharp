namespace SignalSharp.Resampling;

/// <summary>
/// Provides various methods for resampling signals, including downsampling, segment statistics, and approximation techniques.
/// </summary>
/// <remarks>
/// <para>
/// Resampling is a critical process in signal processing and data analysis. It involves altering the sampling rate of a signal 
/// to either reduce (downsample) or increase (upsample) the number of samples. This is particularly useful when dealing with 
/// signals of different sampling rates, reducing data size, or preparing data for further analysis.
/// </para>
/// 
/// <para>
/// The <see cref="Resampling"/> class includes methods for downsampling, computing segment-based statistics (mean, median, max, min), 
/// applying a moving average, and approximating a signal using Chebyshev polynomials. Each method is designed to handle common 
/// tasks in signal processing with efficiency and accuracy.
/// </para>
/// 
/// <para>
/// Consider using the resampling methods in scenarios where:
/// <list type="bullet">
///     <item>Signal data needs to be reduced in size while retaining essential information.</item>
///     <item>Segment-based statistics are required for analysis or feature extraction.</item>
///     <item>A smoothing technique like moving average is needed to reduce noise.</item>
///     <item>An efficient approximation of the signal is necessary for further processing or analysis.</item>
/// </list>
/// </para>
/// </remarks>
public static class Resampling
{
    /// <summary>
    /// Downsamples the given signal by the specified factor.
    /// </summary>
    /// <param name="signal">The input signal to be downsampled.</param>
    /// <param name="factor">The downsampling factor.</param>
    /// <returns>The downsampled signal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the factor is less than or equal to zero.</exception>
    public static double[] Downsample(double[] signal, int factor)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));

        var newLength = (int)Math.Ceiling(signal.Length / (double)factor);
        var result = new double[newLength];

        for (var i = 0; i < newLength; i++)
        {
            result[i] = signal[i * factor];
        }

        return result;
    }

    /// <summary>
    /// Computes the median of each segment in the signal, divided by the specified factor.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="factor">The segment size factor.</param>
    /// <param name="useQuickSelect">Indicates whether to use the QuickSelect algorithm for median computation.</param>
    /// <returns>An array containing the median of each segment.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the factor is less than or equal to zero.</exception>
    /// <remarks>
    /// <para>
    /// The <see cref="SegmentMedian"/> method divides the input signal into segments of size defined by the factor parameter 
    /// and computes the median of each segment. The median is a robust measure of central tendency, less affected by outliers 
    /// compared to the mean.
    /// </para>
    /// 
    /// <para>
    /// The <paramref name="useQuickSelect"/> parameter determines the algorithm used for median computation:
    /// <list type="bullet">
    ///     <item><c>true</c> - Uses the QuickSelect algorithm, which has an average-case time complexity of O(n), making it 
    ///     efficient for larger datasets. QuickSelect is a selection algorithm to find the k-th smallest element in an unordered list, 
    ///     and it can be adapted to find the median by selecting the middle element.</item>
    ///     <item><c>false</c> - Uses a straightforward sort-and-select method, which has a time complexity of O(n log n). This method 
    ///     sorts the segment and then selects the middle element (or the average of the two middle elements for even-sized segments). 
    ///     It is simpler but may be slower for large segments.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static double[] SegmentMedian(double[] signal, int factor, bool useQuickSelect = true)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));

        return useQuickSelect ? MedianQuickSelect(signal, factor) : MedianCalculate(signal, factor);
    }

    /// <summary>
    /// Computes the mean of each segment in the signal, divided by the specified factor.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="factor">The segment size factor.</param>
    /// <returns>An array containing the mean of each segment.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the factor is less than or equal to zero.</exception>
    public static double[] SegmentMean(double[] signal, int factor)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));

        var newLength = (int)Math.Ceiling(signal.Length / (double)factor);
        var result = new double[newLength];

        for (var i = 0; i < newLength; i++)
        {
            var start = i * factor;
            var end = Math.Min(start + factor, signal.Length);
            result[i] = signal.Skip(start).Take(end - start).Average();
        }

        return result;
    }

    /// <summary>
    /// Computes the maximum value of each segment in the signal, divided by the specified factor.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="factor">The segment size factor.</param>
    /// <returns>An array containing the maximum value of each segment.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the factor is less than or equal to zero.</exception>
    public static double[] SegmentMax(double[] signal, int factor)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));

        var newLength = (int)Math.Ceiling(signal.Length / (double)factor);
        var result = new double[newLength];

        for (var i = 0; i < newLength; i++)
        {
            var start = i * factor;
            var end = Math.Min(start + factor, signal.Length);
            result[i] = signal.Skip(start).Take(end - start).Max();
        }

        return result;
    }

    /// <summary>
    /// Computes the minimum value of each segment in the signal, divided by the specified factor.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="factor">The segment size factor.</param>
    /// <returns>An array containing the minimum value of each segment.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the factor is less than or equal to zero.</exception>
    public static double[] SegmentMin(double[] signal, int factor)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));

        var newLength = (int)Math.Ceiling(signal.Length / (double)factor);
        var result = new double[newLength];

        for (var i = 0; i < newLength; i++)
        {
            var start = i * factor;
            var end = Math.Min(start + factor, signal.Length);
            result[i] = signal.Skip(start).Take(end - start).Min();
        }

        return result;
    }

    /// <summary>
    /// Applies a moving average filter to the signal with the specified window size.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="windowSize">The size of the moving window.</param>
    /// <returns>The filtered signal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the window size is less than or equal to zero.</exception>
    public static double[] MovingAverage(double[] signal, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowSize, nameof(windowSize));

        var result = new double[signal.Length - windowSize + 1];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = signal.Skip(i).Take(windowSize).Average();
        }

        return result;
    }

    /// <summary>
    /// Approximates the signal using Chebyshev polynomials of the specified order.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="order">The order of the Chebyshev polynomials.</param>
    /// <returns>The approximated signal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the order is less than or equal to zero.</exception>
    public static double[] ChebyshevApproximation(double[] signal, int order)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order, nameof(order));

        var coefficients = ChebyshevFit(signal, order);
        var result = new double[signal.Length];

        for (var i = 0; i < signal.Length; i++)
        {
            result[i] = ChebyshevEvaluate(coefficients, i, signal.Length);
        }

        return result;
    }

    /// <summary>
    /// Computes the median of each segment using a simple calculation method.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="factor">The segment size factor.</param>
    /// <returns>An array containing the median of each segment.</returns>
    private static double[] MedianCalculate(double[] signal, int factor)
    {
        var newLength = (int)Math.Ceiling(signal.Length / (double)factor);
        var result = new double[newLength];

        for (var i = 0; i < newLength; i++)
        {
            var start = i * factor;
            var end = Math.Min(start + factor, signal.Length);
            result[i] = CalculateMedian(signal.Skip(start).Take(end - start).ToArray());
        }

        return result;
    }

    /// <summary>
    /// Computes the median of each segment using the QuickSelect algorithm.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="factor">The segment size factor.</param>
    /// <returns>An array containing the median of each segment.</returns>
    private static double[] MedianQuickSelect(double[] signal, int factor)
    {
        var newLength = (int)Math.Ceiling(signal.Length / (double)factor);
        var result = new double[newLength];

        for (var i = 0; i < newLength; i++)
        {
            var start = i * factor;
            var end = Math.Min(start + factor, signal.Length);
            result[i] = QuickSelectMedian(signal, start, end - start);
        }

        return result;
    }

    /// <summary>
    /// Calculates the median of an array of values.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <returns>The median value.</returns>
    /// <exception cref="ArgumentException">Thrown when the values array is null or empty.</exception>
    private static double CalculateMedian(double[] values)
    {
        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("Values array must not be null or empty.", nameof(values));
        }

        Array.Sort(values);

        var middle = values.Length / 2;

        return values.Length % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2.0
            : values[middle];
    }

    /// <summary>
    /// Computes the median using the QuickSelect algorithm for a specified segment.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <param name="start">The start index of the segment.</param>
    /// <param name="length">The length of the segment.</param>
    /// <returns>The median value.</returns>
    /// <exception cref="ArgumentException">Thrown when the values array is null or the length is less than or equal to zero.</exception>
    private static double QuickSelectMedian(double[] values, int start, int length)
    {
        if (values == null || length <= 0)
        {
            throw new ArgumentException("Values array must not be null or empty.", nameof(values));
        }

        var mid = length / 2;
        if (length % 2 == 0)
        {
            return 0.5 * (QuickSelect(values, start, length, mid - 1) + QuickSelect(values, start, length, mid));
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
    private static double QuickSelect(double[] values, int start, int length, int k)
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
    private static int Partition(double[] values, int start, int length)
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
    private static void Swap(double[] values, int a, int b)
    {
        (values[a], values[b]) = (values[b], values[a]);
    }

    /// <summary>
    /// Fits Chebyshev polynomials to the signal up to the specified order.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="order">The order of the Chebyshev polynomials.</param>
    /// <returns>The coefficients of the Chebyshev polynomials.</returns>
    private static double[] ChebyshevFit(double[] signal, int order)
    {
        var n = signal.Length;
        var t = new double[n];

        for (var i = 0; i < n; i++)
        {
            t[i] = Math.Cos(Math.PI * (i + 0.5) / n);
        }

        var coefficients = new double[order + 1];

        for (var k = 0; k <= order; k++)
        {
            var sum = 0.0;
            for (var i = 0; i < n; i++)
            {
                sum += signal[i] * Math.Cos(Math.PI * k * (i + 0.5) / n);
            }

            coefficients[k] = sum * 2.0 / n;
        }

        return coefficients;
    }

    /// <summary>
    /// Evaluates the Chebyshev polynomials at a given point.
    /// </summary>
    /// <param name="coefficients">The coefficients of the Chebyshev polynomials.</param>
    /// <param name="x">The point at which to evaluate the polynomials.</param>
    /// <param name="length">The length of the signal.</param>
    /// <returns>The evaluated value.</returns>
    private static double ChebyshevEvaluate(double[] coefficients, int x, int length)
    {
        return coefficients
            .Select((t, k) => t * Math.Cos(Math.PI * k * (x + 0.5) / length))
            .Sum();
    }
}