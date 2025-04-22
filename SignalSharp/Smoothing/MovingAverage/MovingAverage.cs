using SignalSharp.Common;
using SignalSharp.Common.Models;

namespace SignalSharp.Smoothing.MovingAverage;

/// <summary>
/// Provides methods to calculate various types of moving averages on a signal.
/// </summary>
public static class MovingAverage
{
    /// <summary>
    /// Calculates the Simple Moving Average (SMA) of a given signal using an efficient sliding window approach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SMA is calculated by taking the average of a fixed number of points (window size) in the signal.
    /// This implementation uses a sliding window for O(N) efficiency.
    /// </para>
    /// <para>
    /// Behavior with Padding:
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Padding.None"/></term>
    ///         <description>The output signal will be shorter than the input signal, with a length of `signal.Length - windowSize + 1`. It contains only the averages where the window fits entirely within the original signal (similar to 'valid' convolution mode).</description>
    ///     </item>
    ///     <item>
    ///         <term>Other Padding Modes (<see cref="Padding.Constant"/>, <see cref="Padding.Mirror"/>, etc.)</term>
    ///         <description>The output signal will have the same length as the input signal. Padding is applied internally to compute the average values near the signal boundaries.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// double[] signal = {1, 2, 3, 4, 5};
    /// int windowSize = 3;
    ///
    /// double[] smaValid = MovingAverage.SimpleMovingAverage(signal, windowSize, Padding.None);
    /// // smaValid will be {2, 3, 4}
    /// </code>
    /// </example>
    /// <param name="signal">The input signal array.</param>
    /// <param name="windowSize">The number of points to include in each average calculation. Must be positive.</param>
    /// <param name="padding">The padding mode to use. Default is <see cref="Padding.None" />.</param>
    /// <param name="paddedValue">The value used for padding if <see cref="Padding.Constant" /> is selected. Default is 0.</param>
    /// <returns>A new array containing the simple moving averages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the window size is less than or equal to zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="padding"/> is <see cref="Padding.None"/> and <paramref name="windowSize"/> is greater than the signal length.</exception>
    public static double[] SimpleMovingAverage(double[] signal, int windowSize, Padding padding = Padding.None,
        double paddedValue = 0)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowSize, nameof(windowSize));

        if (padding == Padding.None)
        {
            // if no padding, window cannot be larger than the signal
            ArgumentOutOfRangeException.ThrowIfGreaterThan(windowSize, signal.Length, nameof(windowSize));

            if (signal.Length == 0 || windowSize > signal.Length) return [];

            var outputLength = signal.Length - windowSize + 1;
            var result = new double[outputLength];

            // calculate the sum of the first window
            double currentSum = 0;
            for (var k = 0; k < windowSize; k++)
            {
                currentSum += signal[k];
            }
            result[0] = currentSum / windowSize;

            // sliding window
            for (var i = 1; i < outputLength; i++)
            {
                currentSum = currentSum - signal[i - 1] + signal[i + windowSize - 1];
                result[i] = currentSum / windowSize;
            }
            
            return result;
        }
        else
        {
            // using padding
            if (signal.Length == 0) return [];

            var extendedSignal = SignalPadding.ApplyPadding(signal, windowSize, padding, paddedValue);

            var outputLength = signal.Length;
            var result = new double[outputLength];

            var extendedOutputLength = extendedSignal.Length - windowSize + 1;
            if (extendedOutputLength <= 0) return []; // should not happen if windowSize > 0 and ApplyPadding works

            // calculate the sum of the first window
            double currentSum = 0;
            for (var k = 0; k < windowSize; k++)
            {
                currentSum += extendedSignal[k];
            }
            result[0] = currentSum / windowSize;

            // sliding window up to 'outputLength' results
            for (var i = 1; i < outputLength; i++)
            {
                var leavingIndex = i - 1;
                var enteringIndex = i + windowSize - 1;

                if (enteringIndex >= extendedSignal.Length)
                {
                      break;
                }

                var elementLeaving = extendedSignal[leavingIndex];
                var elementEntering = extendedSignal[enteringIndex];

                currentSum = currentSum - elementLeaving + elementEntering;
                result[i] = currentSum / windowSize;
            }

            return result;
        }
    }

    /// <summary>
    /// Calculates the Exponential Moving Average (EMA) of a given signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The EMA is calculated recursively using a smoothing factor, alpha, which gives more weight to recent data points.
    /// The formula is: <c>EMA[t] = alpha * signal[t] + (1 - alpha) * EMA[t-1]</c>.
    /// The first value <c>EMA[0]</c> is initialized as <c>signal[0]</c>.
    /// </para>
    /// <para>
    /// This method calculates the EMA directly on the input signal; padding is not applicable to the standard EMA calculation.
    /// The output array will have the same length as the input signal.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// double[] signal = {1, 2, 3, 4, 5};
    /// double alpha = 0.5; // Smoothing factor
    /// double[] ema = MovingAverage.ExponentialMovingAverage(signal, alpha);
    /// // ema will be {1, 1.5, 2.25, 3.125, 4.0625}
    /// </code>
    /// </example>
    /// <param name="signal">The input signal array.</param>
    /// <param name="alpha">The smoothing factor. Must be in the range (0, 1]. A higher alpha gives more weight to recent points.</param>
    /// <returns>A new array containing the exponential moving averages, having the same length as the input signal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when alpha is not strictly greater than 0 and less than or equal to 1.</exception>
    public static double[] ExponentialMovingAverage(double[] signal, double alpha)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        // standard alpha range is (0, 1]. Alpha=0 means EMA[t] = EMA[t-1] (constant), Alpha=1 means EMA[t] = signal[t].
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(alpha, 0, nameof(alpha));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alpha, 1, nameof(alpha));

        if (signal.Length == 0) return [];

        var result = new double[signal.Length];
        result[0] = signal[0];

        for (var i = 1; i < signal.Length; i++)
        {
            result[i] = alpha * signal[i] + (1 - alpha) * result[i - 1];
        }

        return result;
    }

    /// <summary>
    /// Calculates the Weighted Moving Average (WMA) of a given signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WMA is calculated by applying a specified set of weights to the signal points within a sliding window.
    /// The length of the `weights` array determines the window size (`W`).
    /// The calculation for the output at index `i` (in 'valid' mode) is:
    /// `Sum(signal[i+j] * weights[j] for j=0 to W-1) / Sum(weights)`.
    /// </para>
    /// <para>
    /// The time complexity is O(N*W) where N is the signal length and W is the window size (weights length),
    /// as the weighted sum is recalculated for each output point.
    /// </para>
    /// <para>
    /// Behavior with Padding (consistent with SMA):
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Padding.None"/></term>
    ///         <description>The output signal will be shorter than the input signal, with a length of `signal.Length - W + 1`. It contains only the averages where the window fits entirely within the original signal ('valid' mode).</description>
    ///     </item>
    ///     <item>
    ///         <term>Other Padding Modes (<see cref="Padding.Constant"/>, <see cref="Padding.Mirror"/>, etc.)</term>
    ///         <description>The output signal will have the same length as the input signal. Padding is applied internally to compute the average values near the signal boundaries ('same' mode equivalent length).</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// double[] signal = {1, 2, 3, 4, 5};
    /// double[] weights = {0.1, 0.3, 0.6}; // Window size = 3, Sum = 1.0
    ///
    /// // No padding ('valid' mode)
    /// double[] wmaValid = MovingAverage.WeightedMovingAverage(signal, weights, Padding.None);
    /// // WMA[0] = (1*0.1 + 2*0.3 + 3*0.6) / 1.0 = 2.5
    /// // WMA[1] = (2*0.1 + 3*0.3 + 4*0.6) / 1.0 = 3.5
    /// // WMA[2] = (3*0.1 + 4*0.3 + 5*0.6) / 1.0 = 4.5
    /// // wmaValid will be {2.5, 3.5, 4.5} (Length = 5 - 3 + 1 = 3)
    ///
    /// // Constant padding ('same' mode output length)
    /// // Assuming ApplyPadding({1,2,3,4,5}, 3, Constant, 0) -> {0, 1, 2, 3, 4, 5, 0}
    /// // WMA[0] = (0*0.1 + 1*0.3 + 2*0.6) / 1.0 = 1.5
    /// // WMA[1] = (1*0.1 + 2*0.3 + 3*0.6) / 1.0 = 2.5
    /// // WMA[2] = (2*0.1 + 3*0.3 + 4*0.6) / 1.0 = 3.5
    /// // WMA[3] = (3*0.1 + 4*0.3 + 5*0.6) / 1.0 = 4.5
    /// // WMA[4] = (4*0.1 + 5*0.3 + 0*0.6) / 1.0 = 1.9
    /// double[] wmaPadded = MovingAverage.WeightedMovingAverage(signal, weights, Padding.Constant, 0);
    /// // wmaPadded will be {1.5, 2.5, 3.5, 4.5, 1.9} (Length = 5)
    /// </code>
    /// </example>
    /// <param name="signal">The input signal array.</param>
    /// <param name="weights">The weights to apply. The length of this array determines the window size. Must not be empty.</param>
    /// <param name="padding">The padding mode to use. Default is <see cref="Padding.None" />.</param>
    /// <param name="paddedValue">The value used for padding if <see cref="Padding.Constant" /> is selected. Default is 0.</param>
    /// <returns>A new array containing the weighted moving averages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the signal or weights are null.</exception>
    /// <exception cref="ArgumentException">Thrown when the weights array is empty or the sum of weights is zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="padding"/> is <see cref="Padding.None"/> and the weights length (window size) is greater than the signal length.</exception>
    public static double[] WeightedMovingAverage(double[] signal, double[] weights, Padding padding = Padding.None,
        double paddedValue = 0)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentNullException.ThrowIfNull(weights, nameof(weights));
        ArgumentOutOfRangeException.ThrowIfZero(weights.Length, nameof(weights));

        var windowSize = weights.Length;
        var weightsSum = weights.Sum();
        
        if (Math.Abs(weightsSum) < 1e-10) throw new ArgumentException("Sum of weights cannot be zero.", nameof(weights));

        if (padding == Padding.None)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(windowSize, signal.Length, nameof(weights));
            
            if (signal.Length == 0 || windowSize > signal.Length) return [];

            var outputLength = signal.Length - windowSize + 1;
            var result = new double[outputLength];
            if (outputLength == 0) return result;

            for (var i = 0; i < outputLength; i++)
            {
                double weightedSum = 0;
                for (var j = 0; j < windowSize; j++)
                {
                    weightedSum += signal[i + j] * weights[j];
                }
                result[i] = weightedSum / weightsSum;
            }
            
            return result;
        }
        else
        {
            if (signal.Length == 0) return [];

            var extendedSignal = SignalPadding.ApplyPadding(signal, windowSize, padding, paddedValue);
            var outputLength = signal.Length;
            var result = new double[outputLength];
            var extendedOutputLength = extendedSignal.Length - windowSize + 1;

            if (extendedOutputLength <= 0) return [];

            for (var i = 0; i < outputLength; i++)
            {
                if (i + windowSize > extendedSignal.Length) break; // stop if window goes out of bounds

                double weightedSum = 0;
                for (var j = 0; j < windowSize; j++)
                {
                    weightedSum += extendedSignal[i + j] * weights[j];
                }
                result[i] = weightedSum / weightsSum;
            }
            return result;
        }
    }
}