using System.Numerics;
using SignalSharp.Common;
using SignalSharp.Utilities;

namespace SignalSharp.Tests.Utilities;

[TestFixture]
public class StatisticalFunctionsTests
{
    [Test]
    public void Mean_Double_SimpleValues_ShouldReturnCorrectMean()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var mean = StatisticalFunctions.Mean<double>(values.AsSpan());
        Assert.That(mean, Is.EqualTo(3.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Mean_Float_SimpleValues_ShouldReturnCorrectMean()
    {
        float[] values = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
        var mean = StatisticalFunctions.Mean<float>(values);
        Assert.That(mean, Is.EqualTo(3.0f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Mean_Decimal_SimpleValues_ShouldReturnCorrectMean()
    {
        decimal[] values = [1.0m, 2.0m, 3.0m, 4.0m, 5.0m];
        var mean = StatisticalFunctions.Mean<decimal>(values);
        Assert.That(mean, Is.EqualTo(3.0m).Within(Constants.DecimalDefaultEpsilon));
    }

    [Test]
    public void Mean_Double_EmptySpan_ShouldReturnZero()
    {
        double[] values = [];
        var mean = StatisticalFunctions.Mean<double>(values.AsSpan());
        Assert.That(mean, Is.EqualTo(0.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Mean_Float_EmptySpan_ShouldReturnZero()
    {
        float[] values = [];
        var mean = StatisticalFunctions.Mean<float>(values);
        Assert.That(mean, Is.EqualTo(0.0f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Mean_Double_SingleValue_ShouldReturnTheValue()
    {
        double[] values = [42.5];
        var mean = StatisticalFunctions.Mean<double>(values.AsSpan());
        Assert.That(mean, Is.EqualTo(42.5).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Mean_Double_NegativeValues_ShouldReturnCorrectMean()
    {
        double[] values = [-1.0, -2.0, -3.0, -4.0, -5.0];
        var mean = StatisticalFunctions.Mean<double>(values.AsSpan());
        Assert.That(mean, Is.EqualTo(-3.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Mean_Double_MixedSignValues_ShouldReturnCorrectMean()
    {
        double[] values = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var mean = StatisticalFunctions.Mean<double>(values.AsSpan());
        Assert.That(mean, Is.EqualTo(0.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Mean_Double_NonVectorAlignedSize_ShouldReturnCorrectMean()
    {
        double[] values = GenerateNonVectorAlignedData(Vector<double>.Count + 1);
        double expectedMean = values.Sum() / values.Length;
        var mean = StatisticalFunctions.Mean<double>(values.AsSpan());
        Assert.That(mean, Is.EqualTo(expectedMean).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Median_Double_OddLength_Sort_ShouldReturnMiddleValue()
    {
        double[] values = [1.0, 5.0, 2.0, 4.0, 3.0];
        var median = StatisticalFunctions.Median<double>(values, useQuickSelect: true);
        Assert.That(median, Is.EqualTo(3.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Median_Float_EvenLength_Sort_ShouldReturnAverageOfMiddleTwo()
    {
        float[] values = [1.0f, 6.0f, 2.0f, 5.0f, 3.0f, 4.0f]; // Sorted: 1, 2, 3, 4, 5, 6 -> Median (3+4)/2 = 3.5
        var median = StatisticalFunctions.Median<float>(values, useQuickSelect: false);
        Assert.That(median, Is.EqualTo(3.5f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Median_Double_OddLength_QuickSelect_ShouldReturnMiddleValue()
    {
        double[] values = [1.0, 5.0, 2.0, 4.0, 3.0];
        var median = StatisticalFunctions.Median<double>(values, useQuickSelect: true);
        Assert.That(median, Is.EqualTo(3.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Median_Float_EvenLength_QuickSelect_ShouldReturnAverageOfMiddleTwo()
    {
        float[] values = [1.0f, 6.0f, 2.0f, 5.0f, 3.0f, 4.0f];
        var median = StatisticalFunctions.Median<float>(values, useQuickSelect: true);
        Assert.That(median, Is.EqualTo(3.5f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Median_Decimal_SingleValue_ShouldReturnTheValue()
    {
        decimal[] values = [42.5m];
        var median = StatisticalFunctions.Median<decimal>(values);
        Assert.That(median, Is.EqualTo(42.5m).Within(Constants.DecimalDefaultEpsilon));
    }

    [Test]
    public void Median_Double_WithDuplicates_ShouldReturnCorrectMedian()
    {
        double[] values = [1.0, 2.0, 2.0, 3.0, 4.0]; // Sorted: 1, 2, 2, 3, 4 -> Median 2
        var median = StatisticalFunctions.Median<double>(values);
        Assert.That(median, Is.EqualTo(2.0).Within(Constants.DefaultEpsilon));

        double[] valuesEven = [1.0, 2.0, 2.0, 3.0, 3.0, 4.0]; // Sorted: 1, 2, 2, 3, 3, 4 -> Median (2+3)/2 = 2.5
        var medianEven = StatisticalFunctions.Median<double>(valuesEven);
        Assert.That(medianEven, Is.EqualTo(2.5).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Median_EmptySpan_ShouldThrowArgumentException()
    {
        double[] values = [];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.Median<double>(values));
    }

    [Test]
    public void Variance_Double_SimpleValues_ShouldReturnCorrectVariance()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0]; // Mean = 3. Var = [(1-3)^2 + (2-3)^2 + (3-3)^2 + (4-3)^2 + (5-3)^2] / 5 = [4+1+0+1+4]/5 = 10/5 = 2
        var variance = StatisticalFunctions.Variance<double>(values.AsSpan());
        Assert.That(variance, Is.EqualTo(2.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Variance_Float_SimpleValues_ShouldReturnCorrectVariance()
    {
        float[] values = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
        var variance = StatisticalFunctions.Variance<float>(values);
        Assert.That(variance, Is.EqualTo(2.0f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Variance_Decimal_SimpleValues_ShouldReturnCorrectVariance()
    {
        decimal[] values = [1.0m, 2.0m, 3.0m, 4.0m, 5.0m];
        var variance = StatisticalFunctions.Variance<decimal>(values);
        Assert.That(variance, Is.EqualTo(2.0m).Within(Constants.DecimalDefaultEpsilon));
    }

    [Test]
    public void Variance_Double_EmptySpan_ShouldReturnZero()
    {
        double[] values = [];
        var variance = StatisticalFunctions.Variance<double>(values.AsSpan());
        Assert.That(variance, Is.EqualTo(0.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Variance_Float_SingleValue_ShouldReturnZero()
    {
        float[] values = [42.5f];
        var variance = StatisticalFunctions.Variance<float>(values);
        Assert.That(variance, Is.EqualTo(0.0f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Variance_Double_IdenticalValues_ShouldReturnZero()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        var variance = StatisticalFunctions.Variance<double>(values.AsSpan());
        Assert.That(variance, Is.EqualTo(0.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Variance_Double_NonVectorAlignedSize_ShouldReturnCorrectVariance()
    {
        double[] values = GenerateNonVectorAlignedData(Vector<double>.Count + 1);
        double mean = values.Average();
        double expectedVariance = values.Select(x => (x - mean) * (x - mean)).Sum() / values.Length;

        var variance = StatisticalFunctions.Variance<double>(values.AsSpan());
        Assert.That(variance, Is.EqualTo(expectedVariance).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void StandardDeviation_Double_SimpleValues_ShouldReturnCorrectStandardDeviation()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0]; // Var = 2
        var stdDev = StatisticalFunctions.StandardDeviation<double>(values.AsSpan());
        Assert.That(stdDev, Is.EqualTo(Math.Sqrt(2.0)).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void StandardDeviation_Float_SimpleValues_ShouldReturnCorrectStandardDeviation()
    {
        float[] values = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f]; // Var = 2
        var stdDev = StatisticalFunctions.StandardDeviation<float>(values);
        Assert.That(stdDev, Is.EqualTo((float)Math.Sqrt(2.0)).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void StandardDeviation_Double_EmptySpan_ShouldReturnZero()
    {
        double[] values = [];
        var stdDev = StatisticalFunctions.StandardDeviation<double>(values.AsSpan());
        Assert.That(stdDev, Is.EqualTo(0.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void StandardDeviation_Float_SingleValue_ShouldReturnZero()
    {
        float[] values = [42.5f];
        var stdDev = StatisticalFunctions.StandardDeviation<float>(values);
        Assert.That(stdDev, Is.EqualTo(0.0f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void StandardDeviation_Double_IdenticalValues_ShouldReturnZero()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        var stdDev = StatisticalFunctions.StandardDeviation<double>(values.AsSpan());
        Assert.That(stdDev, Is.EqualTo(0.0).Within(Constants.StrictEpsilon));
    }

    [Test]
    public void StandardDeviation_HandlesNearZeroVariance()
    {
        const double mean = 100.0;
        const double smallDiff = Constants.DefaultEpsilon;
        double[] values = [mean - smallDiff, mean, mean + smallDiff];
        var stdDev = StatisticalFunctions.StandardDeviation<double>(values.AsSpan());

        Assert.That(stdDev, Is.GreaterThanOrEqualTo(0.0).Within(Constants.StrictEpsilon));
        Assert.That(stdDev, Is.Not.NaN);
    }

    [Test]
    public void Min_Double_SimpleValues_ShouldReturnMinimum()
    {
        double[] values = [3.0, 1.0, 4.0, 1.5, 5.0];
        var min = StatisticalFunctions.Min<double>(values);
        Assert.That(min, Is.EqualTo(1.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Max_Float_SimpleValues_ShouldReturnMaximum()
    {
        float[] values = [3.0f, 1.0f, 4.0f, 1.5f, 5.0f];
        var max = StatisticalFunctions.Max<float>(values);
        Assert.That(max, Is.EqualTo(5.0f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Min_Decimal_NegativeValues_ShouldReturnMinimum()
    {
        decimal[] values = [-3.0m, -1.0m, -4.0m, -1.5m, -5.0m];
        var min = StatisticalFunctions.Min<decimal>(values);
        Assert.That(min, Is.EqualTo(-5.0m).Within(Constants.DecimalDefaultEpsilon));
    }

    [Test]
    public void Max_Double_MixedSignValues_ShouldReturnMaximum()
    {
        double[] values = [-3.0, 1.0, -4.0, 0.0, 5.0, -5.0];
        var max = StatisticalFunctions.Max<double>(values);
        Assert.That(max, Is.EqualTo(5.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Min_SingleValue_ShouldReturnTheValue()
    {
        double[] values = [42.5];
        var min = StatisticalFunctions.Min<double>(values);
        Assert.That(min, Is.EqualTo(42.5).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Max_SingleValue_ShouldReturnTheValue()
    {
        float[] values = [42.5f];
        var max = StatisticalFunctions.Max<float>(values);
        Assert.That(max, Is.EqualTo(42.5f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void Min_EmptySpan_ShouldThrowInvalidOperationException()
    {
        double[] values = [];
        Assert.Throws<InvalidOperationException>(() => StatisticalFunctions.Min<double>(values));
    }

    [Test]
    public void Max_EmptySpan_ShouldThrowInvalidOperationException()
    {
        float[] values = [];
        Assert.Throws<InvalidOperationException>(() => StatisticalFunctions.Max<float>(values));
    }

    [Test]
    public void Normalize_Double_SimpleValues_ShouldReturnNormalizedValues()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0]; // Min=1, Max=5, Range=4
        var normalized = StatisticalFunctions.Normalize<double>(values.AsSpan());
        double[] expected = [0.0, 0.25, 0.5, 0.75, 1.0];
        AssertEqualWithin(expected, normalized, Constants.DefaultEpsilon);
    }

    [Test]
    public void Normalize_Float_SimpleValues_ShouldReturnNormalizedValues()
    {
        float[] values = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f]; // Min=1, Max=5, Range=4
        var normalized = StatisticalFunctions.Normalize<float>(values);
        float[] expected = [0.0f, 0.25f, 0.5f, 0.75f, 1.0f];
        AssertEqualWithin(expected, normalized, Constants.FloatDefaultEpsilon);
    }

    [Test]
    public void Normalize_Decimal_SimpleValues_ShouldReturnNormalizedValues()
    {
        decimal[] values = [10m, 20m, 30m, 40m, 50m]; // Min=10, Max=50, Range=40
        var normalized = StatisticalFunctions.Normalize<decimal>(values);
        decimal[] expected = [0.0m, 0.25m, 0.5m, 0.75m, 1.0m];
        AssertEqualWithin(expected, normalized, Constants.DecimalDefaultEpsilon);
    }

    [Test]
    public void Normalize_Double_IdenticalValues_ShouldReturnZeros()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        var normalized = StatisticalFunctions.Normalize<double>(values.AsSpan());
        double[] expected = [0.0, 0.0, 0.0, 0.0];
        AssertEqualWithin(expected, normalized, Constants.StrictEpsilon);
    }

    [Test]
    public void Normalize_Float_IdenticalValues_ShouldReturnZeros()
    {
        float[] values = [5.0f, 5.0f, 5.0f, 5.0f];
        var normalized = StatisticalFunctions.Normalize<float>(values);
        float[] expected = [0.0f, 0.0f, 0.0f, 0.0f];
        AssertEqualWithin(expected, normalized, Constants.FloatStrictEpsilon);
    }

    [Test]
    public void Normalize_Double_SingleValue_ShouldReturnZero()
    {
        double[] values = [42.5];
        var normalized = StatisticalFunctions.Normalize<double>(values.AsSpan());
        double[] expected = [0.0];
        AssertEqualWithin(expected, normalized, Constants.StrictEpsilon);
    }

    [Test]
    public void Normalize_Double_NegativeAndPositiveValues_ShouldReturnNormalizedValues()
    {
        double[] values = [-5.0, 0.0, 5.0, 10.0]; // Min=-5, Max=10, Range=15
        var normalized = StatisticalFunctions.Normalize<double>(values.AsSpan());
        // Expected: (-5 - -5)/15=0, (0 - -5)/15=5/15=1/3, (5 - -5)/15=10/15=2/3, (10 - -5)/15=15/15=1
        double[] expected = [0.0, 1.0 / 3.0, 2.0 / 3.0, 1.0];
        AssertEqualWithin(expected, normalized, Constants.DefaultEpsilon);
    }

    [Test]
    public void Normalize_Double_NonVectorAlignedSize_ShouldReturnNormalizedValues()
    {
        double[] values = GenerateNonVectorAlignedData(Vector<double>.Count + 1);
        double min = values.Min();
        double max = values.Max();
        double range = max - min;
        double[] expected = range == 0 ? new double[values.Length] : values.Select(v => (v - min) / range).ToArray();

        var normalized = StatisticalFunctions.Normalize<double>(values.AsSpan());
        AssertEqualWithin(expected, normalized, Constants.DefaultEpsilon);
    }

    [Test]
    public void Normalize_Double_EmptySpan_ShouldThrowInvalidOperationException()
    {
        double[] values = [];

        Assert.That(StatisticalFunctions.Normalize<double>(values.AsSpan()), Is.Empty);
    }

    [Test]
    public void Normalize_Float_EmptySpan_ShouldThrowInvalidOperationException()
    {
        float[] values = [];

        Assert.That(StatisticalFunctions.Normalize<float>(values.AsSpan()), Is.Empty);
    }

    [Test]
    public void Normalize_NonAllocating_Double_SimpleValues_ShouldWriteNormalizedValues()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        double[] destination = new double[values.Length];
        double[] expected = [0.0, 0.25, 0.5, 0.75, 1.0];

        StatisticalFunctions.Normalize(values.AsSpan(), destination.AsSpan());

        AssertEqualWithin(expected, destination, Constants.DefaultEpsilon);
    }

    [Test]
    public void Normalize_NonAllocating_Double_IdenticalValues_ShouldWriteZeros()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        double[] destination = new double[values.Length];
        double[] expected = [0.0, 0.0, 0.0, 0.0];

        StatisticalFunctions.Normalize(values.AsSpan(), destination.AsSpan());

        AssertEqualWithin(expected, destination, Constants.StrictEpsilon);
    }

    [Test]
    public void Normalize_NonAllocating_Double_SingleValue_ShouldWriteZero()
    {
        double[] values = [42.5];
        double[] destination = new double[values.Length];
        double[] expected = [0.0];

        StatisticalFunctions.Normalize(values.AsSpan(), destination.AsSpan());

        AssertEqualWithin(expected, destination, Constants.StrictEpsilon);
    }

    [Test]
    public void Normalize_NonAllocating_Double_NonVectorAlignedSize_ShouldWriteNormalizedValues()
    {
        double[] values = GenerateNonVectorAlignedData(Vector<double>.Count + 1);
        double[] destination = new double[values.Length];
        double min = values.Min();
        double max = values.Max();
        double range = max - min;
        double[] expected = range == 0 ? new double[values.Length] : values.Select(v => (v - min) / range).ToArray();

        StatisticalFunctions.Normalize(values.AsSpan(), destination.AsSpan());

        AssertEqualWithin(expected, destination, Constants.DefaultEpsilon);
    }

    [Test]
    public void Normalize_NonAllocating_Double_EmptySpan_ShouldThrowInvalidOperationException()
    {
        double[] values = [];
        double[] destination = [];

        StatisticalFunctions.Normalize(values.AsSpan(), destination.AsSpan());

        Assert.That(destination, Is.Empty);
    }

    [Test]
    public void Normalize_NonAllocating_Double_MismatchedLengths_ShouldThrowArgumentException()
    {
        double[] values = [1.0, 2.0, 3.0];
        double[] destination = new double[2];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.Normalize(values.AsSpan(), destination.AsSpan()));
    }

    [Test]
    public void ZScoreNormalization_Double_SimpleValues_ShouldReturnNormalizedValues()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var normalized = StatisticalFunctions.ZScoreNormalization<double>(values.AsSpan());
        double invStdDev = 1.0 / Math.Sqrt(2.0);
        double[] expected = [-2.0 * invStdDev, -1.0 * invStdDev, 0.0, 1.0 * invStdDev, 2.0 * invStdDev];
        AssertEqualWithin(expected, normalized, Constants.DefaultEpsilon);

        Assert.That(StatisticalFunctions.Mean<double>(normalized), Is.EqualTo(0.0).Within(Constants.StrictEpsilon));
        Assert.That(StatisticalFunctions.StandardDeviation<double>(normalized), Is.EqualTo(1.0).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void ZScoreNormalization_Float_SimpleValues_ShouldReturnNormalizedValues()
    {
        float[] values = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
        var normalized = StatisticalFunctions.ZScoreNormalization<float>(values);
        float invStdDev = 1.0f / (float)Math.Sqrt(2.0);
        float[] expected = [-2.0f * invStdDev, -1.0f * invStdDev, 0.0f, 1.0f * invStdDev, 2.0f * invStdDev];

        AssertEqualWithin(expected, normalized, Constants.FloatDefaultEpsilon);
        Assert.That(StatisticalFunctions.Mean<float>(normalized), Is.EqualTo(0.0f).Within(Constants.FloatStrictEpsilon));
        Assert.That(StatisticalFunctions.StandardDeviation<float>(normalized), Is.EqualTo(1.0f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void ZScoreNormalization_Double_IdenticalValues_ShouldReturnZeros()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        var normalized = StatisticalFunctions.ZScoreNormalization<double>(values.AsSpan());
        double[] expected = [0.0, 0.0, 0.0, 0.0];
        AssertEqualWithin(expected, normalized, Constants.StrictEpsilon);
    }

    [Test]
    public void ZScoreNormalization_Float_IdenticalValues_ShouldReturnZeros()
    {
        float[] values = [5.0f, 5.0f, 5.0f, 5.0f];
        var normalized = StatisticalFunctions.ZScoreNormalization<float>(values);
        float[] expected = [0.0f, 0.0f, 0.0f, 0.0f];
        AssertEqualWithin(expected, normalized, Constants.FloatStrictEpsilon);
    }

    [Test]
    public void ZScoreNormalization_Double_SingleValue_ShouldReturnZero()
    {
        // Although StdDev is 0 for a single point, the function handles it gracefully
        double[] values = [42.5];
        var normalized = StatisticalFunctions.ZScoreNormalization<double>(values.AsSpan());
        double[] expected = [0.0];
        AssertEqualWithin(expected, normalized, Constants.StrictEpsilon);
    }

    [Test]
    public void ZScoreNormalization_Float_SingleValue_ShouldReturnZero()
    {
        float[] values = [42.5f];
        var normalized = StatisticalFunctions.ZScoreNormalization<float>(values);
        float[] expected = [0.0f];
        AssertEqualWithin(expected, normalized, Constants.FloatStrictEpsilon);
    }

    [Test]
    public void ZScoreNormalization_Double_EmptySpan_ShouldReturnEmptyArray()
    {
        double[] values = [];
        var normalized = StatisticalFunctions.ZScoreNormalization<double>(values.AsSpan());
        Assert.That(normalized, Is.Empty);
    }

    [Test]
    public void ZScoreNormalization_Float_EmptySpan_ShouldReturnEmptyArray()
    {
        float[] values = [];
        var normalized = StatisticalFunctions.ZScoreNormalization<float>(values);
        Assert.That(normalized, Is.Empty);
    }

    [Test]
    public void ZScoreNormalization_Double_NonVectorAlignedSize_ShouldReturnNormalizedValues()
    {
        double[] values = GenerateNonVectorAlignedData(Vector<double>.Count + 1);
        double mean = values.Average();
        double stdDev = StatisticalFunctions.StandardDeviation<double>(values);
        double[] expected = stdDev == 0 ? new double[values.Length] : values.Select(v => (v - mean) / stdDev).ToArray();

        var normalized = StatisticalFunctions.ZScoreNormalization<double>(values.AsSpan());
        AssertEqualWithin(expected, normalized, Constants.DefaultEpsilon);

        if (stdDev != 0)
        {
            Assert.That(StatisticalFunctions.Mean<double>(normalized), Is.EqualTo(0.0).Within(Constants.StrictEpsilon));
            Assert.That(StatisticalFunctions.StandardDeviation<double>(normalized), Is.EqualTo(1.0).Within(Constants.DefaultEpsilon));
        }
    }

    [Test]
    public void Skewness_Double_SymmetricDistribution_ShouldBeNearZero()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0]; // Perfectly symmetric
        var skewness = StatisticalFunctions.Skewness<double>(values.AsSpan());
        Assert.That(skewness, Is.EqualTo(0.0).Within(Constants.StrictEpsilon));
    }

    [Test]
    public void Skewness_Float_SymmetricDistribution_ShouldBeNearZero()
    {
        float[] values = [1.0f, 2.0f, 2.0f, 3.0f, 3.0f, 4.0f]; // Symmetric
        var skewness = StatisticalFunctions.Skewness<float>(values);
        Assert.That(skewness, Is.EqualTo(0.0f).Within(Constants.FloatStrictEpsilon));
    }

    [Test]
    public void Skewness_Double_RightSkewed_ShouldBePositive()
    {
        double[] values = [1, 2, 3, 4, 10];
        var skewness = StatisticalFunctions.Skewness<double>(values.AsSpan());
        Assert.That(skewness, Is.GreaterThan(0.0));
        double expected = 3.0 * Math.Sqrt(10.0) / 4.0;
        Assert.That(skewness, Is.EqualTo(expected).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Skewness_Double_LeftSkewed_ShouldBeNegative()
    {
        double[] values = [1, 7, 8, 9, 10];
        var skewness = StatisticalFunctions.Skewness<double>(values.AsSpan());
        Assert.That(skewness, Is.LessThan(0.0));
        double expected = -3.0 * Math.Sqrt(10.0) / 4.0;
        Assert.That(skewness, Is.EqualTo(expected).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void Skewness_Double_IdenticalValues_ShouldReturnZero()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        var skewness = StatisticalFunctions.Skewness<double>(values.AsSpan());
        Assert.That(skewness, Is.EqualTo(0.0).Within(Constants.StrictEpsilon));
    }

    [Test]
    public void Skewness_Float_IdenticalValues_ShouldReturnZero()
    {
        float[] values = [5.0f, 5.0f, 5.0f, 5.0f];
        var skewness = StatisticalFunctions.Skewness<float>(values);
        Assert.That(skewness, Is.EqualTo(0.0f).Within(Constants.FloatStrictEpsilon));
    }

    [Test]
    public void Skewness_LessThan3Values_ShouldThrowArgumentException()
    {
        double[] values2 = [1.0, 2.0];
        float[] values1 = [1.0f];

        Assert.Throws<ArgumentException>(() => StatisticalFunctions.Skewness<double>(values2.AsSpan()));
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.Skewness<float>(values1));
    }

    [Test]
    public void PopulationKurtosis_Double_UniformDistribution_ShouldBeNegativeExcess()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var kurtosis = StatisticalFunctions.PopulationExcessKurtosis<double>(values.AsSpan());
        Assert.That(kurtosis, Is.EqualTo(-1.3).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void PopulationKurtosis_Double_Leptokurtic_ShouldBeNegativeExcessInThisCase()
    {
        double[] values = [1, 4, 5, 6, 9];
        var kurtosis = StatisticalFunctions.PopulationExcessKurtosis<double>(values.AsSpan());
        double expected = (514.0 / 5.0) / Math.Pow(34.0 / 5.0, 2) - 3.0;
        Assert.That(kurtosis, Is.EqualTo(expected).Within(Constants.DefaultEpsilon));
        Assert.That(kurtosis, Is.GreaterThan(-1.3), "Kurtosis should be less negative than uniform");
    }

    [Test]
    public void PopulationKurtosis_Double_Platykurtic_ShouldBeNegativeExcess()
    {
        double[] values = [1, 1, 5, 9, 9];
        var kurtosis = StatisticalFunctions.PopulationExcessKurtosis<double>(values.AsSpan());
        double expected = (1024.0 / 5.0) / Math.Pow(64.0 / 5.0, 2) - 3.0;
        Assert.That(kurtosis, Is.EqualTo(expected).Within(Constants.DefaultEpsilon));
        Assert.That(kurtosis, Is.LessThan(-1.3), "Kurtosis should be more negative than uniform");
    }

    [Test]
    public void PopulationKurtosis_Double_IdenticalValues_ShouldThrowArgumentException()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.PopulationExcessKurtosis<double>(values.AsSpan()));
    }

    [Test]
    public void PopulationKurtosis_Double_LessThan4Values_ShouldThrowArgumentException()
    {
        double[] values3 = [1.0, 2.0, 3.0];
        double[] values2 = [1.0, 2.0];
        double[] values1 = [1.0];
        double[] values0 = [];

        Assert.Throws<ArgumentException>(() => StatisticalFunctions.PopulationExcessKurtosis<double>(values3.AsSpan()));
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.PopulationExcessKurtosis<double>(values2.AsSpan()));
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.PopulationExcessKurtosis<double>(values1.AsSpan()));
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.PopulationExcessKurtosis<double>(values0.AsSpan()));
    }

    [Test]
    public void PopulationKurtosis_Float_UniformDistribution_ShouldBeNegativeExcess()
    {
        float[] values = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
        var kurtosis = StatisticalFunctions.PopulationExcessKurtosis<float>(values.AsSpan());
        Assert.That(kurtosis, Is.EqualTo(-1.3f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void PopulationKurtosis_Float_IdenticalValues_ShouldThrowArgumentException()
    {
        float[] values = [5.0f, 5.0f, 5.0f, 5.0f];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.PopulationExcessKurtosis<float>(values.AsSpan()));
    }

    [Test]
    public void PopulationKurtosis_Float_LessThan4Values_ShouldThrowArgumentException()
    {
        float[] values3 = [1.0f, 2.0f, 3.0f];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.PopulationExcessKurtosis<float>(values3.AsSpan()));
    }

    [Test]
    public void SampleKurtosisG2_Double_UniformDistribution_ShouldBeNearZero()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var kurtosisG2 = StatisticalFunctions.SampleKurtosisG2<double>(values.AsSpan());
        Assert.That(kurtosisG2, Is.EqualTo(-1.2).Within(Constants.DefaultEpsilon));
    }

    [Test]
    public void SampleKurtosisG2_Double_IdenticalValues_ShouldThrowArgumentException()
    {
        double[] values = [5.0, 5.0, 5.0, 5.0];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.SampleKurtosisG2<double>(values.AsSpan()));
    }

    [Test]
    public void SampleKurtosisG2_Double_LessThan4Values_ShouldThrowArgumentException()
    {
        double[] values3 = [1.0, 2.0, 3.0];
        double[] values2 = [1.0, 2.0];
        double[] values1 = [1.0];
        double[] values0 = [];

        Assert.Throws<ArgumentException>(() => StatisticalFunctions.SampleKurtosisG2<double>(values3.AsSpan()));
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.SampleKurtosisG2<double>(values2.AsSpan()));
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.SampleKurtosisG2<double>(values1.AsSpan()));
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.SampleKurtosisG2<double>(values0.AsSpan()));
    }

    [Test]
    public void SampleKurtosisG2_Float_UniformDistribution_ShouldBeNearZero()
    {
        float[] values = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
        var kurtosisG2 = StatisticalFunctions.SampleKurtosisG2<float>(values);
        Assert.That(kurtosisG2, Is.EqualTo(-1.2f).Within(Constants.FloatDefaultEpsilon));
    }

    [Test]
    public void SampleKurtosisG2_Float_IdenticalValues_ShouldThrowArgumentException()
    {
        float[] values = [5.0f, 5.0f, 5.0f, 5.0f];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.SampleKurtosisG2<float>(values));
    }

    [Test]
    public void SampleKurtosisG2_Float_LessThan4Values_ShouldThrowArgumentException()
    {
        float[] values3 = [1.0f, 2.0f, 3.0f];
        Assert.Throws<ArgumentException>(() => StatisticalFunctions.SampleKurtosisG2<float>(values3));
    }

    private static void AssertEqualWithin<T>(T[] expected, T[] actual, T tolerance)
        where T : INumber<T>
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length), "Array lengths differ.");
        Assert.Multiple(() =>
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(tolerance), $"Mismatch at index {i}");
            }
        });
    }

    private static void AssertEqualWithin<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, T tolerance)
        where T : INumber<T>
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length), "Span lengths differ.");

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(actual[i], Is.EqualTo(expected[i]).Within(tolerance), $"Mismatch at index {i}");
        }
    }

    private static double[] GenerateNonVectorAlignedData(int size)
    {
        if (size <= 0)
            size = 1;

        var vectorSize = Vector<double>.Count;
        if (vectorSize > 1 && size % vectorSize == 0)
        {
            size++; // make it non-aligned
        }
        else if (vectorSize <= 1)
        {
            size = Math.Max(size, 3);
        }

        var data = new double[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = i + 1.0;
        }

        return data;
    }
}
