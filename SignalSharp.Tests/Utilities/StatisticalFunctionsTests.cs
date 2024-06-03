using SignalSharp.Utilities;

namespace SignalSharp.Tests.Utilities;

[TestFixture]
public class StatisticalFunctionsTests
{
    private const double Tolerance = 1e-7;

    [Test]
    public void Mean_SimpleValues_ShouldReturnCorrectMean()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];

        var mean = StatisticalFunctions.Mean<double>(values);

        const double expected = 3.0;

        Assert.That(mean, Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void Variance_SimpleValues_ShouldReturnCorrectVariance()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var variance = StatisticalFunctions.Variance<double>(values);

        const double expected = 2.0;

        Assert.That(variance, Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void StandardDeviation_SimpleValues_ShouldReturnCorrectStandardDeviation()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var stdDev = StatisticalFunctions.StandardDeviation<double>(values);

        var expected = Math.Sqrt(2.0);

        Assert.That(stdDev, Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void ZScoreNormalization_SimpleValues_ShouldReturnNormalizedValues()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var normalized = StatisticalFunctions.ZScoreNormalization<double>(values).ToArray();

        double[] expected = [-1.41421356, -0.70710678, 0.0, 0.70710678, 1.41421356];

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Has.Length.EqualTo(expected.Length));
        
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(normalized[i], Is.EqualTo(expected[i]).Within(Tolerance));
            }
        });
    }

    [Test]
    public void MinMaxScaling_SimpleValues_ShouldReturnScaledValues()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var scaled = StatisticalFunctions.MinMaxScaling<double>(values).ToArray();

        double[] expected = [0.0, 0.25, 0.5, 0.75, 1.0];

        Assert.Multiple(() =>
        {
            Assert.That(scaled, Has.Length.EqualTo(expected.Length));
        
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(scaled[i], Is.EqualTo(expected[i]).Within(Tolerance));
            }
        });
    }

    [Test]
    public void Skewness_SimpleValues_ShouldReturnCorrectSkewness()
    {
        double[] values = [1.0, 2.0, 2.0, 3.0, 4.0];
        var skewness = StatisticalFunctions.Skewness<double>(values);

        const double expected = 0.5657196;

        Assert.That(skewness, Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void Kurtosis_SimpleValues_ShouldReturnCorrectKurtosis()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var kurtosis = StatisticalFunctions.Kurtosis<double>(values);

        const double expected = -1.3;

        Assert.That(kurtosis, Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void Normalize_SimpleValues_ShouldReturnNormalizedValues()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var normalized = StatisticalFunctions.Normalize<double>(values).ToArray();

        double[] expected = [0.0, 0.25, 0.5, 0.75, 1.0];

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Has.Length.EqualTo(expected.Length));
        
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(normalized[i], Is.EqualTo(expected[i]).Within(Tolerance));
            }
        });
    }
}