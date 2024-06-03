using SignalSharp.Common;
using SignalSharp.Common.Models;

namespace SignalSharp.Smoothing.MovingAverage;

/// <summary>
/// Provides methods to calculate various types of moving averages on a signal.
/// </summary>
public static class MovingAverage
{
    /// <summary>
    /// Calculates the Simple Moving Average (SMA) of a given signal.
    /// <para>
    /// The SMA is calculated by taking the average of a fixed number of points in the signal,
    /// defined by the window size, and then sliding the window along the signal.
    /// </para>
    /// <example>
    /// <code>
    /// double[] signal = {1, 2, 3, 4, 5};
    /// int windowSize = 3;
    /// double[] sma = MovingAverage.SimpleMovingAverage(signal, windowSize);
    /// // sma will be {2, 3, 4}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="signal">The input signal to calculate the moving average on.</param>
    /// <param name="windowSize">The number of points to include in each average calculation.</param>
    /// <param name="padding">The padding mode to use when calculating the moving average. Default is <see cref="Padding.None" />.</param>
    /// <param name="paddedValue">The value to use for padding if <see cref="Padding.Constant" /> is selected. Default is 0.</param>
    /// <returns>A new array containing the simple moving averages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the window size is less than or equal to zero or greater than the signal length.</exception>
    public static double[] SimpleMovingAverage(double[] signal, int windowSize, Padding padding = Padding.None, 
        double paddedValue = 0)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowSize, nameof(windowSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(windowSize, signal.Length, nameof(windowSize));

        var extendedSignal = SignalPadding.ApplyPadding(signal, windowSize, padding, paddedValue);
        var result = new double[extendedSignal.Length - windowSize + 1];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = extendedSignal.Skip(i).Take(windowSize).Average();
        }

        return result;
    }

    /// <summary>
    /// Calculates the Exponential Moving Average (EMA) of a given signal.
    /// <para>
    /// The EMA is calculated using a smoothing factor, alpha, which gives more weight to recent data points.
    /// The formula is: EMA_t = alpha * signal_t + (1 - alpha) * EMA_(t-1)
    /// </para>
    /// <example>
    /// <code>
    /// double[] signal = {1, 2, 3, 4, 5};
    /// double alpha = 0.5;
    /// double[] ema = MovingAverage.ExponentialMovingAverage(signal, alpha);
    /// // ema will be {1, 1.5, 2.25, 3.125, 4.0625}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="signal">The input signal to calculate the moving average on.</param>
    /// <param name="alpha">The smoothing factor, must be in the range (0, 1].</param>
    /// <param name="padding">The padding mode to use when calculating the moving average. Default is <see cref="Padding.None" />.</param>
    /// <param name="paddedValue">The value to use for padding if <see cref="Padding.Constant" /> is selected. Default is 0.</param>
    /// <returns>A new array containing the exponential moving averages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when alpha is not in the range (0, 1].</exception>
    public static double[] ExponentialMovingAverage(double[] signal, double alpha, Padding padding = Padding.None, 
        double paddedValue = 0)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(alpha, 0, nameof(alpha));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alpha, 1, nameof(alpha));

        var extendedSignal = SignalPadding.ApplyPadding(signal, signal.Length, padding, paddedValue);
        var result = new double[extendedSignal.Length];
        result[0] = extendedSignal[0];

        for (var i = 1; i < result.Length; i++)
        {
            result[i] = alpha * extendedSignal[i] + (1 - alpha) * result[i - 1];
        }

        return result;
    }

    /// <summary>
    /// Calculates the Weighted Moving Average (WMA) of a given signal.
    /// <para>
    /// The WMA is calculated by applying a set of weights to the points within the window size.
    /// Each point in the signal is multiplied by the corresponding weight, and the results are summed.
    /// </para>
    /// <example>
    /// <code>
    /// double[] signal = {1, 2, 3, 4, 5};
    /// double[] weights = {0.1, 0.3, 0.6};
    /// double[] wma = MovingAverage.WeightedMovingAverage(signal, weights);
    /// // wma will be {2.3, 3.3, 4.3}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="signal">The input signal to calculate the moving average on.</param>
    /// <param name="weights">The weights to apply to the points within the window size.</param>
    /// <param name="padding">The padding mode to use when calculating the moving average. Default is <see cref="Padding.None" />.</param>
    /// <param name="paddedValue">The value to use for padding if <see cref="Padding.Constant" /> is selected. Default is 0.</param>
    /// <returns>A new array containing the weighted moving averages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal or weights are null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the weights array length is zero or greater than the signal length.</exception>
    public static double[] WeightedMovingAverage(double[] signal, double[] weights, Padding padding = Padding.None, 
        double paddedValue = 0)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentNullException.ThrowIfNull(weights, nameof(weights));
        ArgumentOutOfRangeException.ThrowIfZero(weights.Length, nameof(weights));
        ArgumentOutOfRangeException.ThrowIfLessThan(signal.Length, weights.Length, nameof(weights));

        var windowSize = weights.Length;
        var extendedSignal = SignalPadding.ApplyPadding(signal, windowSize, padding, paddedValue);
        var result = new double[extendedSignal.Length - windowSize + 1];

        for (var i = 0; i < result.Length; i++)
        {
            double sum = 0;
            for (var j = 0; j < windowSize; j++)
            {
                sum += extendedSignal[i + j] * weights[j];
            }
            result[i] = sum / weights.Sum();
        }

        return result;
    }
}