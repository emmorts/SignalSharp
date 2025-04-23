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
        if (padding == Padding.None)
            return signal;

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
        var halfWindow = windowSize / 2;
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
            extendedSignal[i] = signal[halfWindow - i - 1];
            extendedSignal[extendedLength - 1 - i] = signal[signal.Length - halfWindow + i];
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
            extendedSignal[i] = signal[signal.Length - halfWindow + i];
            extendedSignal[extendedLength - 1 - i] = signal[i];
        }
    }
}
