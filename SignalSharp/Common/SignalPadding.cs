using SignalSharp.Common.Models;

namespace SignalSharp.Common;

/// <summary>
/// Provides methods for applying and trimming padding on signals.
/// </summary>
public static class SignalPadding
{
    /// <summary>
    /// <para>Applies padding to a signal based on the specified padding type and window size.</para>
    /// <para>This method supports the following types of padding:
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="Padding.Constant"/>: Pads with a constant value.</description>
    /// </item>
    /// <item>
    /// <description><see cref="Padding.Mirror"/>: Pads with a mirror reflection of the signal.</description>
    /// </item>
    /// <item>
    /// <description><see cref="Padding.Nearest"/>: Pads with the nearest value from the signal.</description>
    /// </item>
    /// <item>
    /// <description><see cref="Padding.Periodic"/>: Pads with a periodic repetition of the signal.</description>
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="signal">The input signal to pad.</param>
    /// <param name="windowSize">The size of the padding window.</param>
    /// <param name="padding">The type of padding to apply.</param>
    /// <param name="paddedValue">The value to use for constant padding.</param>
    /// <returns>A new signal array with the applied padding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="signal"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="windowSize"/> is negative or an unknown padding mode is supplied.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="signal"/> is empty and padding other than <see cref="Padding.None"/> is requested.</exception>
    /// <remarks>
    /// When <paramref name="padding"/> is <see cref="Padding.None"/> the input array is returned unchanged (same reference).
    /// Mirror and periodic padding support window sizes larger than the signal by reflecting/repeating the signal.
    /// </remarks>
    /// <example>
    /// <code>
    /// double[] signal = { 1.0, 2.0, 3.0 };
    /// var windowSize = 4;
    /// var padding = Padding.Constant;
    /// double paddedValue = 0.0;
    /// double[] paddedSignal = SignalPadding.ApplyPadding(signal, windowSize, padding, paddedValue);
    /// </code>
    /// </example>
    public static double[] ApplyPadding(double[] signal, int windowSize, Padding padding, double paddedValue)
    {
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentOutOfRangeException.ThrowIfNegative(windowSize, nameof(windowSize));

        if (padding == Padding.None)
            return signal;

        if (signal.Length == 0)
        {
            throw new ArgumentException("Signal must contain at least one element to apply padding.", nameof(signal));
        }

        var halfWindow = windowSize / 2;
        var extendedLength = signal.Length + 2 * halfWindow;
        var extendedSignal = new double[extendedLength];

        Array.Copy(signal, 0, extendedSignal, halfWindow, signal.Length);

        switch (padding)
        {
            case Padding.Constant:
                ApplyConstantPadding(extendedSignal, halfWindow, extendedLength, paddedValue);
                break;
            case Padding.Mirror:
                ApplyMirrorPadding(extendedSignal, halfWindow, extendedLength, signal);
                break;
            case Padding.Nearest:
                ApplyNearestPadding(extendedSignal, halfWindow, extendedLength, signal);
                break;
            case Padding.Periodic:
                ApplyPeriodicPadding(extendedSignal, halfWindow, extendedLength, signal);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(padding), padding, "Unknown padding mode.");
        }

        return extendedSignal;
    }

    /// <summary>
    /// Trims the padding from an extended signal to return it to its original length.
    /// </summary>
    /// <param name="extendedSignal">The extended signal with padding.</param>
    /// <param name="originalLength">The original length of the signal before padding was applied.</param>
    /// <param name="windowSize">The size of the padding window used.</param>
    /// <returns>A new signal array with the padding trimmed off.</returns>
    /// <example>
    /// <code>
    /// double[] extendedSignal = { 0.0, 0.0, 1.0, 2.0, 3.0, 0.0, 0.0 };
    /// int originalLength = 3;
    /// int windowSize = 4;
    /// double[] trimmedSignal = SignalPadding.TrimPadding(extendedSignal, originalLength, windowSize);
    /// </code>
    /// </example>
    /// <para>This method removes the padding added by the <see cref="ApplyPadding"/> method and returns the signal to its original length.</para>
    public static double[] TrimPadding(double[] extendedSignal, int originalLength, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(extendedSignal);
        ArgumentOutOfRangeException.ThrowIfNegative(originalLength, nameof(originalLength));
        ArgumentOutOfRangeException.ThrowIfNegative(windowSize, nameof(windowSize));

        var halfWindow = windowSize / 2;

        if (extendedSignal.Length < originalLength + 2 * halfWindow)
        {
            throw new ArgumentException(
                $"Extended signal (length {extendedSignal.Length}) is too short to contain {originalLength} original samples with window size {windowSize}.",
                nameof(extendedSignal)
            );
        }

        var trimmedSignal = new double[originalLength];

        Array.Copy(extendedSignal, halfWindow, trimmedSignal, 0, originalLength);

        return trimmedSignal;
    }

    private static void ApplyConstantPadding(double[] extendedSignal, int halfWindow, int extendedLength, double paddedValue)
    {
        for (var i = 0; i < halfWindow; i++)
        {
            extendedSignal[i] = paddedValue;
            extendedSignal[extendedLength - 1 - i] = paddedValue;
        }
    }

    private static void ApplyMirrorPadding(double[] extendedSignal, int halfWindow, int extendedLength, double[] signal)
    {
        for (var i = 0; i < halfWindow; i++)
        {
            extendedSignal[i] = signal[MirrorIndex(halfWindow - i - 1, signal.Length)];
            extendedSignal[extendedLength - 1 - i] = signal[MirrorIndex(signal.Length - halfWindow + i, signal.Length)];
        }
    }

    private static void ApplyNearestPadding(double[] extendedSignal, int halfWindow, int extendedLength, double[] signal)
    {
        for (var i = 0; i < halfWindow; i++)
        {
            extendedSignal[i] = signal[0];
            extendedSignal[extendedLength - 1 - i] = signal[^1];
        }
    }

    private static void ApplyPeriodicPadding(double[] extendedSignal, int halfWindow, int extendedLength, double[] signal)
    {
        for (var i = 0; i < halfWindow; i++)
        {
            extendedSignal[i] = signal[PeriodicIndex(signal.Length - halfWindow + i, signal.Length)];
            extendedSignal[extendedLength - halfWindow + i] = signal[PeriodicIndex(i, signal.Length)];
        }
    }

    /// <summary>
    /// Maps an arbitrary (possibly negative or out-of-range) index onto a signal index using
    /// symmetric, edge-inclusive reflection. For indices inside <c>[0, length)</c> this is the identity.
    /// </summary>
    private static int MirrorIndex(int index, int length)
    {
        var period = 2 * length;
        var k = ((index % period) + period) % period;

        return k < length ? k : period - k - 1;
    }

    /// <summary>
    /// Maps an arbitrary (possibly negative) index onto a signal index by wrapping periodically.
    /// </summary>
    private static int PeriodicIndex(int index, int length)
    {
        return ((index % length) + length) % length;
    }
}
