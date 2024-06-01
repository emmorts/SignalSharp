using SignalSharp.Detection.CUSUM;

namespace SignalSharp.Tests.Detection;

using NUnit.Framework;

[TestFixture]
public class CUSUMAlgorithmTests
{
    [Test]
    public void Detect_SimpleSignal_ShouldDetectChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 1.2,
            SlackFactor = 0.1
        };
        var cusum = new CUSUMAlgorithm(options);
        
        var signal = StatisticalFunctions
            .ZScoreNormalization([0.2, 1.1, 0.2, 4.0, 0.1, 0.2, -2.0, 0.2, 0.1])
            .ToArray();
        int[] expectedChangePoints = [3, 6];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_NoChangeSignal_ShouldDetectNoChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 5,
            SlackFactor = 1
        };
        var cusum = new CUSUMAlgorithm(options);
        var signal = StatisticalFunctions
            .ZScoreNormalization([0.2, 0.1, 0.2, 0.1, 0.2, 0.1, 0.2, 0.1])
            .ToArray();
        int[] expectedChangePoints = [];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_GradualChangeSignal_ShouldDetectChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 3,
            SlackFactor = 1
        };
        var cusum = new CUSUMAlgorithm(options);
        var signal = StatisticalFunctions
            .ZScoreNormalization<double>(
                Enumerable
                    .Range(0, 20)
                    .Select(i => i == 9 ? 15.0 : i * 0.1)
                    .ToArray())
            .ToArray();
        int[] expectedChangePoints = [9];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_SuddenSpikeSignal_ShouldDetectChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 1.5,
            SlackFactor = 0.1
        };
        var cusum = new CUSUMAlgorithm(options);
        
        var signal = StatisticalFunctions
            .ZScoreNormalization([0.1, 0.2, 0.3, 10.0, 0.1, 0.2, -10.0, 0.1, 0.2])
            .ToArray();
        
        int[] expectedChangePoints = [3, 6];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_LongConstantSignal_ShouldDetectNoChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 5,
            SlackFactor = 1
        };
        var cusum = new CUSUMAlgorithm(options);
        var signal = StatisticalFunctions
            .ZScoreNormalization<double>(Enumerable.Repeat(0.1, 100).ToArray())
            .ToArray();
        int[] expectedChangePoints = [];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }
    
    [Test]
    public void Detect_NullSignal_ShouldThrowArgumentNullException()
    {
        var cusum = new CUSUMAlgorithm();
        
        Assert.Throws<ArgumentNullException>(() => cusum.Detect(null!));
    }
    
    [Test]
    public void Constructor_ZeroExpectedStandardDeviation_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new CUSUMOptions
        {
            ExpectedStandardDeviation = 0
        };
        
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new CUSUMAlgorithm(options); });
    }
    
    [Test]
    public void Constructor_ZeroThresholdFactor_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new CUSUMOptions
        {
            ThresholdFactor = 0
        };
        
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new CUSUMAlgorithm(options); });
    }
    
    [Test]
    public void Constructor_NegativeSlackFactor_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new CUSUMOptions
        {
            SlackFactor = -1
        };
        
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new CUSUMAlgorithm(options); });
    }

    [Test]
    public void Detect_EmptySignal_ShouldDetectNoChangePoints()
    {
        var cusum = new CUSUMAlgorithm();
        double[] signal = [];
        int[] expectedChangePoints = [];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_SingleValueSignal_ShouldDetectNoChangePoints()
    {
        var cusum = new CUSUMAlgorithm();
        double[] signal = [0.5];
        int[] expectedChangePoints = [];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_HighVarianceSignal_ShouldDetectChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 2,
            SlackFactor = 0.5
        };
        var cusum = new CUSUMAlgorithm(options);
        double[] signal = [0.1, 0.2, 10.0, -10.0, 0.1, 0.2];
        int[] expectedChangePoints = [2, 3];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_ZeroVarianceSignal_ShouldDetectNoChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1e5,
            ThresholdFactor = 5,
            SlackFactor = 1
        };
        
        var cusum = new CUSUMAlgorithm(options);
        double[] signal = [0, 0, 0, 0, 0];
        int[] expectedChangePoints = [];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }

    [Test]
    public void Detect_AlternatingSignal_ShouldDetectAllChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 0.5,
            SlackFactor = 0
        };
        var cusum = new CUSUMAlgorithm(options);
        double[] signal = [-1, 1, -1, 1, -1, 1];
        int[] expectedChangePoints = [1, 2, 3, 4, 5];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }
    
    [Test]
    public void Detect_LargeConstantSignal_ShouldDetectNoChangePoints()
    {
        var options = new CUSUMOptions
        {
            ExpectedMean = 0,
            ExpectedStandardDeviation = 1,
            ThresholdFactor = 10,
            SlackFactor = 1
        };
        var cusum = new CUSUMAlgorithm(options);
        var signal = Enumerable.Repeat(0.1, 1000).ToArray();
        int[] expectedChangePoints = [];

        var detectedChangePoints = cusum.Detect(signal);

        Assert.That(detectedChangePoints, Is.EqualTo(expectedChangePoints));
    }
}