using System.Numerics;

namespace SignalSharp.Utilities;

public static partial class StatisticalFunctions
{
    private static T MeanGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        var count = values.Length;
        if (count == 0)
        {
            return T.Zero;
        }

        var sum = T.Zero;
        for (var i = 0; i < count; i++)
        {
            sum += values[i];
        }

        return sum / T.CreateChecked(count);
    }

    private static T VarianceGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        var count = values.Length;
        if (count <= 1)
        {
            return T.Zero;
        }

        var mean = MeanGeneric(values);
        var varianceSum = T.Zero;

        for (var i = 0; i < count; i++)
        {
            var diff = values[i] - mean;
            varianceSum += diff * diff;
        }

        return varianceSum / T.CreateChecked(count);
    }

    private static T StandardDeviationGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, IRootFunctions<T>
    {
        var variance = VarianceGeneric(values);
        return T.Sqrt(T.Max(T.Zero, variance));
    }

    private static T MinGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        if (values.IsEmpty)
        {
            throw new InvalidOperationException("Sequence contains no elements.");
        }

        var min = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] < min)
            {
                min = values[i];
            }
        }
        return min;
    }

    private static T MaxGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        if (values.IsEmpty)
        {
            throw new InvalidOperationException("Sequence contains no elements.");
        }

        var max = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] > max)
            {
                max = values[i];
            }
        }
        return max;
    }

    private static (T Min, T Max) MinMaxGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>
    {
        if (values.IsEmpty)
        {
            throw new InvalidOperationException("Sequence contains no elements.");
        }

        var min = values[0];
        var max = values[0];

        for (var i = 1; i < values.Length; i++)
        {
            var current = values[i];
            if (current < min)
            {
                min = current;
            }
            else if (current > max)
            {
                max = current;
            }
        }
        return (min, max);
    }

    private static void NormalizeGeneric<T>(ReadOnlySpan<T> values, Span<T> destination)
        where T : INumber<T>
    {
        var count = values.Length;
        if (count == 0)
        {
            return;
        }

        (T min, T max) = MinMaxGeneric(values);
        var range = max - min;

        if (NumericUtils.IsEffectivelyZero(range))
        {
            destination.Clear();
            return;
        }

        T invRange = T.One / range;
        for (var i = 0; i < count; i++)
        {
            destination[i] = (values[i] - min) * invRange;
        }
    }

    private static void ZScoreNormalizationGeneric<T>(ReadOnlySpan<T> values, Span<T> destination)
        where T : INumber<T>, IRootFunctions<T>
    {
        var count = values.Length;
        if (count == 0)
        {
            return;
        }

        var mean = MeanGeneric(values);
        var stdDev = StandardDeviationGeneric(values);

        if (NumericUtils.IsEffectivelyZero(stdDev))
        {
            destination.Clear();
            return;
        }

        T invStdDev = T.One / stdDev;
        for (var i = 0; i < count; i++)
        {
            destination[i] = (values[i] - mean) * invStdDev;
        }
    }

    private static T SkewnessGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, IRootFunctions<T>
    {
        var n = values.Length;
        if (n < 3)
        {
            throw new ArgumentException("Skewness requires at least three data points.", nameof(values));
        }

        var mean = MeanGeneric(values);
        var stdDev = StandardDeviationGeneric(values);

        if (NumericUtils.IsEffectivelyZero(stdDev))
        {
            return T.Zero;
        }

        T invStdDev = T.One / stdDev;
        var skewnessSum = T.Zero;

        for (var i = 0; i < n; i++)
        {
            var diff = (values[i] - mean) * invStdDev;
            skewnessSum += diff * diff * diff;
        }

        T nT = T.CreateChecked(n);
        T nMinus1T = T.CreateChecked(n - 1);
        T nMinus2T = T.CreateChecked(n - 2);
        T denominator = nMinus1T * nMinus2T;

        if (NumericUtils.IsEffectivelyZero(denominator))
        {
            return T.Zero;
        }

        return nT * skewnessSum / denominator;
    }

    private static T PopulationExcessKurtosisGeneric<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, IRootFunctions<T>
    {
        var n = values.Length;
        if (n < 4)
        {
            throw new ArgumentException("Population excess kurtosis requires at least four data points.", nameof(values));
        }

        var mean = MeanGeneric(values);
        var variance = VarianceGeneric(values);

        if (NumericUtils.IsEffectivelyZero(variance))
        {
            throw new ArgumentException("Population variance is effectively zero, cannot compute population kurtosis.", nameof(values));
        }

        var kurtosisSum = T.Zero;
        for (int i = 0; i < n; i++)
        {
            var diff = values[i] - mean;
            var diff2 = diff * diff;
            kurtosisSum += diff2 * diff2;
        }

        T nT = T.CreateChecked(n);
        T three = T.CreateChecked(3);
        T varianceSquared = variance * variance;

        if (NumericUtils.IsEffectivelyZero(varianceSquared))
        {
            throw new ArgumentException("Population variance squared is effectively zero, cannot compute population kurtosis.", nameof(values));
        }

        var popKurtosis = (kurtosisSum / nT) / varianceSquared;

        return popKurtosis - three;
    }

    private static T SampleKurtosisG2Generic<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, IRootFunctions<T>
    {
        var n = values.Length;
        if (n < 4)
        {
            throw new ArgumentException("Sample excess kurtosis (G2) requires at least four data points.", nameof(values));
        }

        var mean = MeanGeneric(values);
        var popVariance = VarianceGeneric(values);

        if (NumericUtils.IsEffectivelyZero(popVariance))
        {
            throw new ArgumentException("Population variance is effectively zero, cannot compute sample kurtosis G2.", nameof(values));
        }

        var popStdDev = T.Sqrt(T.Max(T.Zero, popVariance));

        if (NumericUtils.IsEffectivelyZero(popStdDev))
        {
            throw new ArgumentException("Population standard deviation is effectively zero, cannot compute sample kurtosis G2.", nameof(values));
        }

        T nT = T.CreateChecked(n);
        T n1 = T.CreateChecked(n - 1);
        T n2 = T.CreateChecked(n - 2);
        T n3 = T.CreateChecked(n - 3);
        T one = T.One;
        T three = T.CreateChecked(3);

        T denCheck1 = n1 * n2 * n3;
        T denCheck2 = n2 * n3;

        if (NumericUtils.IsEffectivelyZero(denCheck1) || NumericUtils.IsEffectivelyZero(denCheck2))
        {
            throw new ArgumentException("Denominator near zero during G2 calculation (n might be too small or precision issues).", nameof(values));
        }

        T invPopStdDev = one / popStdDev;
        var moment4SumPopStdDev = T.Zero;

        foreach (var value in values)
        {
            var diff = (value - mean) * invPopStdDev;
            var diff2 = diff * diff;
            moment4SumPopStdDev += diff2 * diff2;
        }

        T adjFactor = (n1 / nT) * (n1 / nT);
        var moment4SumSampleStdDev = moment4SumPopStdDev * adjFactor;

        T term1Coeff = nT * (nT + one) / denCheck1;
        T term3 = three * n1 * n1 / denCheck2;

        var kurtosisG2 = term1Coeff * moment4SumSampleStdDev - term3;

        return kurtosisG2;
    }
}
