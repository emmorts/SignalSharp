using System.Numerics;
using System.Runtime.CompilerServices;

namespace SignalSharp.Utilities;

public static partial class StatisticalFunctions
{
    private static readonly int VectorDoubleSize = Vector<double>.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double MeanDoubleSimd(ReadOnlySpan<double> values)
    {
        int count = values.Length;
        if (count == 0)
        {
            return 0.0;
        }

        var sumVector = Vector<double>.Zero;
        int i = 0;
        int vectorLimit = count - VectorDoubleSize;

        for (; i <= vectorLimit; i += VectorDoubleSize)
        {
            var v = new Vector<double>(values.Slice(i, VectorDoubleSize));
            sumVector += v;
        }

        double sum = Vector.Dot(sumVector, Vector<double>.One);

        for (; i < count; i++)
        {
            sum += values[i];
        }

        return sum / count;
    }

    private static double VarianceDoubleSimd(ReadOnlySpan<double> values)
    {
        int count = values.Length;
        if (count <= 1)
        {
            return 0.0;
        }

        double mean = MeanDoubleSimd(values);
        var meanVector = new Vector<double>(mean);
        var varianceSumVector = Vector<double>.Zero;
        int i = 0;
        int vectorLimit = count - VectorDoubleSize;

        for (; i <= vectorLimit; i += VectorDoubleSize)
        {
            var v = new Vector<double>(values.Slice(i, VectorDoubleSize));
            var diff = v - meanVector;
            varianceSumVector += diff * diff;
        }

        double varianceSum = Vector.Dot(varianceSumVector, Vector<double>.One);

        for (; i < count; i++)
        {
            double diff = values[i] - mean;
            varianceSum += diff * diff;
        }

        return varianceSum / count;
    }

    private static double StandardDeviationDoubleSimd(ReadOnlySpan<double> values)
    {
        double variance = VarianceDoubleSimd(values);
        return Math.Sqrt(Math.Max(0.0, variance));
    }

    private static void NormalizeDoubleSimd(ReadOnlySpan<double> values, Span<double> destination)
    {
        int count = values.Length;
        if (count == 0)
        {
            return;
        }

        (double min, double max) = MinMaxGeneric(values);
        var range = max - min;

        if (NumericUtils.IsEffectivelyZero(range))
        {
            destination.Clear();
            return;
        }

        double invRange = 1.0 / range;
        var minVector = new Vector<double>(min);
        var invRangeVector = new Vector<double>(invRange);
        int i = 0;
        int vectorLimit = count - VectorDoubleSize;

        for (; i <= vectorLimit; i += VectorDoubleSize)
        {
            var v = new Vector<double>(values.Slice(i, VectorDoubleSize));
            var normalized = (v - minVector) * invRangeVector;
            normalized.CopyTo(destination.Slice(i, VectorDoubleSize));
        }

        for (; i < count; i++)
        {
            destination[i] = (values[i] - min) * invRange;
        }
    }

    private static void ZScoreNormalizationDoubleSimd(ReadOnlySpan<double> values, Span<double> destination)
    {
        int count = values.Length;
        if (count == 0)
        {
            return;
        }

        double mean = MeanDoubleSimd(values);
        double stdDev = StandardDeviationDoubleSimd(values);

        if (NumericUtils.IsEffectivelyZero(stdDev))
        {
            destination.Clear();
            return;
        }

        var meanVector = new Vector<double>(mean);
        var invStdDev = 1.0 / stdDev;
        var invStdDevVector = new Vector<double>(invStdDev);
        int i = 0;
        int vectorLimit = count - VectorDoubleSize;

        for (; i <= vectorLimit; i += VectorDoubleSize)
        {
            var v = new Vector<double>(values.Slice(i, VectorDoubleSize));
            var normalized = (v - meanVector) * invStdDevVector;
            normalized.CopyTo(destination.Slice(i, VectorDoubleSize));
        }

        for (; i < count; i++)
        {
            destination[i] = (values[i] - mean) * invStdDev;
        }
    }

    private static double SkewnessDoubleSimd(ReadOnlySpan<double> values)
    {
        int n = values.Length;
        if (n < 3)
        {
            throw new ArgumentException("Skewness requires at least three data points.", nameof(values));
        }

        double mean = MeanDoubleSimd(values);
        double variance = VarianceDoubleSimd(values);

        if (NumericUtils.IsEffectivelyZero(variance))
        {
            return 0.0;
        }

        double stdDev = Math.Sqrt(variance);

        var meanVector = new Vector<double>(mean);
        var invStdDev = 1.0 / stdDev;
        var invStdDevVector = new Vector<double>(invStdDev);
        var skewnessSumVector = Vector<double>.Zero;
        int i = 0;
        int vectorLimit = n - VectorDoubleSize;

        for (; i <= vectorLimit; i += VectorDoubleSize)
        {
            var v = new Vector<double>(values.Slice(i, VectorDoubleSize));
            var diff = v - meanVector;
            var normalizedDiff = diff * invStdDevVector;
            skewnessSumVector += normalizedDiff * normalizedDiff * normalizedDiff;
        }

        double skewnessSum = Vector.Dot(skewnessSumVector, Vector<double>.One);

        for (; i < n; i++)
        {
            double diff = (values[i] - mean) * invStdDev;
            skewnessSum += diff * diff * diff;
        }

        double nDouble = n;
        double denominator = (nDouble - 1.0) * (nDouble - 2.0);

        if (NumericUtils.IsEffectivelyZero(denominator))
        {
            return 0.0;
        }

        return nDouble * skewnessSum / denominator;
    }

    private static double PopulationExcessKurtosisDoubleSimd(ReadOnlySpan<double> values)
    {
        int n = values.Length;
        if (n < 4)
        {
            throw new ArgumentException("Population excess kurtosis requires at least four data points.", nameof(values));
        }

        double mean = MeanDoubleSimd(values);
        double variance = VarianceDoubleSimd(values);

        if (NumericUtils.IsEffectivelyZero(variance))
        {
            throw new ArgumentException("Population variance is effectively zero, cannot compute population kurtosis.", nameof(values));
        }

        var meanVector = new Vector<double>(mean);
        var kurtosisSumVector = Vector<double>.Zero;
        int i = 0;
        int vectorLimit = n - VectorDoubleSize;

        for (; i <= vectorLimit; i += VectorDoubleSize)
        {
            var v = new Vector<double>(values.Slice(i, VectorDoubleSize));
            var diff = v - meanVector;
            var diff2 = diff * diff;
            kurtosisSumVector += diff2 * diff2;
        }

        double kurtosisSum = Vector.Dot(kurtosisSumVector, Vector<double>.One);

        for (; i < n; i++)
        {
            double diff = values[i] - mean;
            double diff2 = diff * diff;
            kurtosisSum += diff2 * diff2;
        }

        double nDouble = n;
        double varianceSquared = variance * variance;

        if (NumericUtils.IsEffectivelyZero(varianceSquared))
        {
            throw new ArgumentException("Population variance squared is effectively zero, cannot compute population kurtosis.", nameof(values));
        }

        double popKurtosis = kurtosisSum / nDouble / varianceSquared;

        return popKurtosis - 3.0;
    }

    private static double SampleKurtosisG2DoubleSimd(ReadOnlySpan<double> values)
    {
        int n = values.Length;
        if (n < 4)
        {
            throw new ArgumentException("Sample excess kurtosis (G2) requires at least four data points.", nameof(values));
        }

        double mean = MeanDoubleSimd(values);
        double popVariance = VarianceDoubleSimd(values);

        if (NumericUtils.IsEffectivelyZero(popVariance))
        {
            throw new ArgumentException("Population variance is effectively zero, cannot compute sample kurtosis G2.", nameof(values));
        }

        double popStdDev = Math.Sqrt(popVariance);

        if (NumericUtils.IsEffectivelyZero(popStdDev))
        {
            throw new ArgumentException("Population standard deviation is effectively zero, cannot compute sample kurtosis G2.", nameof(values));
        }

        double nDouble = n;
        double n1 = nDouble - 1.0;
        double n2 = nDouble - 2.0;
        double n3 = nDouble - 3.0;
        const double three = 3.0;

        double denCheck1 = n1 * n2 * n3;
        double denCheck2 = n2 * n3;
        if (NumericUtils.IsEffectivelyZero(denCheck1) || NumericUtils.IsEffectivelyZero(denCheck2))
        {
            throw new ArgumentException("Denominator near zero during G2 calculation (n might be too small or precision issues).", nameof(values));
        }

        var meanVector = new Vector<double>(mean);
        var invPopStdDev = 1.0 / popStdDev;
        var invPopStdDevVector = new Vector<double>(invPopStdDev);
        var moment4SumPopStdDevVector = Vector<double>.Zero;
        int i = 0;
        int vectorLimit = n - VectorDoubleSize;

        for (; i <= vectorLimit; i += VectorDoubleSize)
        {
            var v = new Vector<double>(values.Slice(i, VectorDoubleSize));
            var diff = v - meanVector;
            var normalizedDiff = diff * invPopStdDevVector;
            var diff2 = normalizedDiff * normalizedDiff;
            moment4SumPopStdDevVector += diff2 * diff2;
        }

        double moment4SumPopStdDev = Vector.Dot(moment4SumPopStdDevVector, Vector<double>.One);

        for (; i < n; i++)
        {
            double diff = (values[i] - mean) * invPopStdDev;
            double diff2 = diff * diff;
            moment4SumPopStdDev += diff2 * diff2;
        }

        double adjFactor = (n1 / nDouble) * (n1 / nDouble);
        double moment4SumSampleStdDev = moment4SumPopStdDev * adjFactor;

        double term1Coeff = nDouble * (nDouble + 1.0) / denCheck1;
        double term3 = three * n1 * n1 / denCheck2;

        double kurtosisG2 = term1Coeff * moment4SumSampleStdDev - term3;

        return kurtosisG2;
    }
}
