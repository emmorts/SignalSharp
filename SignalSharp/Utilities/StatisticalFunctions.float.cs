using System.Numerics;
using System.Runtime.CompilerServices;
using SignalSharp.Common;

namespace SignalSharp.Utilities;

public static partial class StatisticalFunctions
{
    private static readonly int VectorFloatSize = Vector<float>.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float MeanFloatSimd(ReadOnlySpan<float> values)
    {
        int count = values.Length;
        if (count == 0)
        {
            return 0.0f;
        }

        var sumVector = Vector<float>.Zero;
        int i = 0;
        int vectorLimit = count - VectorFloatSize;

        for (; i <= vectorLimit; i += VectorFloatSize)
        {
            var v = new Vector<float>(values.Slice(i, VectorFloatSize));
            sumVector += v;
        }

        float sum = Vector.Dot(sumVector, Vector<float>.One);

        for (; i < count; i++)
        {
            sum += values[i];
        }

        return sum / count;
    }

    private static float VarianceFloatSimd(ReadOnlySpan<float> values)
    {
        int count = values.Length;
        if (count <= 1)
        {
            return 0.0f;
        }

        float mean = MeanFloatSimd(values);
        var meanVector = new Vector<float>(mean);
        var varianceSumVector = Vector<float>.Zero;
        int i = 0;
        int vectorLimit = count - VectorFloatSize;

        for (; i <= vectorLimit; i += VectorFloatSize)
        {
            var v = new Vector<float>(values.Slice(i, VectorFloatSize));
            var diff = v - meanVector;
            varianceSumVector += diff * diff;
        }

        float varianceSum = Vector.Dot(varianceSumVector, Vector<float>.One);

        for (; i < count; i++)
        {
            float diff = values[i] - mean;
            varianceSum += diff * diff;
        }

        return varianceSum / count;
    }

    private static float StandardDeviationFloatSimd(ReadOnlySpan<float> values)
    {
        float variance = VarianceFloatSimd(values);
        return MathF.Sqrt(MathF.Max(0.0f, variance));
    }

    private static void NormalizeFloatSimd(ReadOnlySpan<float> values, Span<float> destination)
    {
        int count = values.Length;

        (float min, float max) = MinMaxGeneric(values);
        var range = max - min;

        if (NumericUtils.IsEffectivelyZero(range, NumericUtils.GetDefaultEpsilon<float>()))
        {
            destination.Clear();
            return;
        }

        float invRange = 1.0f / range;
        var minVector = new Vector<float>(min);
        var invRangeVector = new Vector<float>(invRange);
        int i = 0;
        int vectorLimit = count - VectorFloatSize;

        for (; i <= vectorLimit; i += VectorFloatSize)
        {
            var v = new Vector<float>(values.Slice(i, VectorFloatSize));
            var normalized = (v - minVector) * invRangeVector;
            normalized.CopyTo(destination.Slice(i, VectorFloatSize));
        }

        for (; i < count; i++)
        {
            destination[i] = (values[i] - min) * invRange;
        }
    }

    private static void ZScoreNormalizationFloatSimd(ReadOnlySpan<float> values, Span<float> destination)
    {
        int count = values.Length;

        float mean = MeanFloatSimd(values);
        float stdDev = StandardDeviationFloatSimd(values);

        if (NumericUtils.IsEffectivelyZero(stdDev))
        {
            destination.Clear();
            return;
        }

        var meanVector = new Vector<float>(mean);
        var invStdDev = 1.0f / stdDev;
        var invStdDevVector = new Vector<float>(invStdDev);
        int i = 0;
        int vectorLimit = count - VectorFloatSize;

        for (; i <= vectorLimit; i += VectorFloatSize)
        {
            var v = new Vector<float>(values.Slice(i, VectorFloatSize));
            var normalized = (v - meanVector) * invStdDevVector;
            normalized.CopyTo(destination.Slice(i, VectorFloatSize));
        }

        for (; i < count; i++)
        {
            destination[i] = (values[i] - mean) * invStdDev;
        }
    }

    private static float SkewnessFloatSimd(ReadOnlySpan<float> values)
    {
        int n = values.Length;
        if (n < 3)
        {
            throw new ArgumentException("Skewness requires at least three data points.", nameof(values));
        }

        float mean = MeanFloatSimd(values);
        float variance = VarianceFloatSimd(values);

        if (NumericUtils.IsEffectivelyZero(variance))
        {
            return 0.0f;
        }

        float stdDev = MathF.Sqrt(variance);
        if (NumericUtils.IsEffectivelyZero(stdDev))
        {
            return 0.0f;
        }

        var meanVector = new Vector<float>(mean);
        var invStdDev = 1.0f / stdDev;
        var invStdDevVector = new Vector<float>(invStdDev);
        var skewnessSumVector = Vector<float>.Zero;
        int i = 0;
        int vectorLimit = n - VectorFloatSize;

        for (; i <= vectorLimit; i += VectorFloatSize)
        {
            var v = new Vector<float>(values.Slice(i, VectorFloatSize));
            var diff = v - meanVector;
            var normalizedDiff = diff * invStdDevVector;
            skewnessSumVector += normalizedDiff * normalizedDiff * normalizedDiff;
        }

        float skewnessSum = Vector.Dot(skewnessSumVector, Vector<float>.One);

        for (; i < n; i++)
        {
            float diff = (values[i] - mean) * invStdDev;
            skewnessSum += diff * diff * diff;
        }

        float nFloat = n;
        float nMinus1 = nFloat - 1.0f;
        float nMinus2 = nFloat - 2.0f;
        float denominator = nMinus1 * nMinus2;

        if (NumericUtils.IsEffectivelyZero(denominator))
        {
            return 0.0f;
        }

        return nFloat / denominator * skewnessSum;
    }

    private static float PopulationExcessKurtosisFloatSimd(ReadOnlySpan<float> values)
    {
        int n = values.Length;
        if (n < 4)
        {
            throw new ArgumentException("Population excess kurtosis requires at least four data points.", nameof(values));
        }

        float mean = MeanFloatSimd(values);
        float variance = VarianceFloatSimd(values);

        if (NumericUtils.IsEffectivelyZero(variance, NumericUtils.GetVarianceEpsilon<float>()))
        {
            throw new ArgumentException("Population variance is effectively zero, cannot compute population kurtosis.", nameof(values));
        }

        var meanVector = new Vector<float>(mean);
        var kurtosisSumVector = Vector<float>.Zero;
        int i = 0;
        int vectorLimit = n - VectorFloatSize;

        for (; i <= vectorLimit; i += VectorFloatSize)
        {
            var v = new Vector<float>(values.Slice(i, VectorFloatSize));
            var diff = v - meanVector;
            var diff2 = diff * diff;
            kurtosisSumVector += diff2 * diff2;
        }

        float kurtosisSum = Vector.Dot(kurtosisSumVector, Vector<float>.One);

        for (; i < n; i++)
        {
            float diff = values[i] - mean;
            float diff2 = diff * diff;
            kurtosisSum += diff2 * diff2;
        }

        float nFloat = n;
        float varianceSquared = variance * variance;

        if (NumericUtils.IsEffectivelyZero(varianceSquared))
        {
            throw new ArgumentException("Population variance squared is effectively zero, cannot compute population kurtosis.", nameof(values));
        }

        float popKurtosis = kurtosisSum / nFloat / varianceSquared;

        return popKurtosis - 3.0f;
    }

    private static float SampleKurtosisG2FloatSimd(ReadOnlySpan<float> values)
    {
        int n = values.Length;
        if (n < 4)
        {
            throw new ArgumentException("Sample excess kurtosis (G2) requires at least four data points.", nameof(values));
        }

        float mean = MeanFloatSimd(values);
        float popVariance = VarianceFloatSimd(values);

        if (NumericUtils.IsEffectivelyZero(popVariance, NumericUtils.GetVarianceEpsilon<float>()))
        {
            throw new ArgumentException("Population variance is effectively zero, cannot compute sample kurtosis G2.", nameof(values));
        }

        float popStdDev = MathF.Sqrt(popVariance);

        if (NumericUtils.IsEffectivelyZero(popStdDev, NumericUtils.GetVarianceEpsilon<float>()))
        {
            throw new ArgumentException("Population standard deviation is effectively zero, cannot compute sample kurtosis G2.", nameof(values));
        }

        float nFloat = n;
        float n1 = nFloat - 1.0f;
        float n2 = nFloat - 2.0f;
        float n3 = nFloat - 3.0f;
        const float three = 3.0f;

        float denCheck1 = n1 * n2 * n3;
        float denCheck2 = n2 * n3;

        if (NumericUtils.IsEffectivelyZero(denCheck1, Constants.FloatStrictEpsilon) || NumericUtils.IsEffectivelyZero(denCheck2, Constants.FloatStrictEpsilon))
        {
            throw new ArgumentException("Denominator near zero during G2 calculation (n might be too small or precision issues).", nameof(values));
        }

        var meanVector = new Vector<float>(mean);
        var invPopStdDev = 1.0f / popStdDev;
        var invPopStdDevVector = new Vector<float>(invPopStdDev);
        var moment4SumPopStdDevVector = Vector<float>.Zero;
        int i = 0;
        int vectorLimit = n - VectorFloatSize;

        for (; i <= vectorLimit; i += VectorFloatSize)
        {
            var v = new Vector<float>(values.Slice(i, VectorFloatSize));
            var diff = v - meanVector;
            var normalizedDiff = diff * invPopStdDevVector;
            var diff2 = normalizedDiff * normalizedDiff;
            moment4SumPopStdDevVector += diff2 * diff2;
        }

        float moment4SumPopStdDev = Vector.Dot(moment4SumPopStdDevVector, Vector<float>.One);

        for (; i < n; i++)
        {
            float diff = (values[i] - mean) * invPopStdDev;
            float diff2 = diff * diff;
            moment4SumPopStdDev += diff2 * diff2;
        }

        float adjFactor = n1 / nFloat * (n1 / nFloat);
        float moment4SumSampleStdDev = moment4SumPopStdDev * adjFactor;

        float term1Coeff = nFloat * (nFloat + 1.0f) / denCheck1;
        float term3 = three * n1 * n1 / denCheck2;

        float kurtosisG2 = term1Coeff * moment4SumSampleStdDev - term3;

        return kurtosisG2;
    }
}
