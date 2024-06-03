using SignalSharp.Utilities;

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
            var slice = signal[start..end].AsSpan();
            result[i] = StatisticalFunctions.Mean<double>(slice);
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
            var slice = signal[start..end].AsSpan();
            result[i] = StatisticalFunctions.Max<double>(slice);
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
            var slice = signal[start..end].AsSpan();
            result[i] = StatisticalFunctions.Min<double>(slice);
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
            var slice = signal[start..end].AsSpan();
            result[i] = StatisticalFunctions.Median<double>(slice);
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
            var slice = signal[start..end].AsSpan();
            result[i] = StatisticalFunctions.Median<double>(slice, true);
        }

        return result;
    }
}