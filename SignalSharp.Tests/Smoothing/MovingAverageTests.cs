using SignalSharp.Smoothing.MovingAverage;

namespace SignalSharp.Tests.Smoothing;

[TestFixture]
public class MovingAverageTests
{
    [Test]
    public void SimpleMovingAverage_ValidInput_ReturnsCorrectResult()
    {
        const int windowSize = 3;

        double[] signal = [1, 2, 3, 4, 5];
        double[] expected = [2, 3, 4];

        var result = MovingAverage.SimpleMovingAverage(signal, windowSize);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SimpleMovingAverage_NullSignal_ThrowsArgumentNullException()
    {
        const int windowSize = 3;

        double[] signal = null!;

        Assert.Throws<ArgumentNullException>(() => MovingAverage.SimpleMovingAverage(signal, windowSize));
    }

    [Test]
    public void SimpleMovingAverage_WindowSizeLessThanOrEqualToZero_ThrowsArgumentOutOfRangeException()
    {
        const int windowSize = 0;

        double[] signal = [1, 2, 3, 4, 5];

        Assert.Throws<ArgumentOutOfRangeException>(() => MovingAverage.SimpleMovingAverage(signal, windowSize));
    }

    [Test]
    public void SimpleMovingAverage_WindowSizeGreaterThanSignalLength_ThrowsArgumentOutOfRangeException()
    {
        double[] signal = [1, 2, 3];
        const int windowSize = 4;

        Assert.Throws<ArgumentOutOfRangeException>(() => MovingAverage.SimpleMovingAverage(signal, windowSize));
    }

    [Test]
    public void ExponentialMovingAverage_ValidInput_ReturnsCorrectResult()
    {
        const double alpha = 0.5;

        double[] signal = [1, 2, 3, 4, 5];
        double[] expected = [1, 1.5, 2.25, 3.125, 4.0625];

        var result = MovingAverage.ExponentialMovingAverage(signal, alpha);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void ExponentialMovingAverage_NullSignal_ThrowsArgumentNullException()
    {
        const double alpha = 0.5;

        double[] signal = null!;

        Assert.Throws<ArgumentNullException>(() => MovingAverage.ExponentialMovingAverage(signal, alpha));
    }

    [Test]
    public void ExponentialMovingAverage_AlphaOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        double alpha = 0;
        double[] signal = [1, 2, 3, 4, 5];

        Assert.Throws<ArgumentOutOfRangeException>(() => MovingAverage.ExponentialMovingAverage(signal, alpha));

        alpha = 1.1;
        Assert.Throws<ArgumentOutOfRangeException>(() => MovingAverage.ExponentialMovingAverage(signal, alpha));
    }

    [Test]
    public void WeightedMovingAverage_ValidInput_ReturnsCorrectResult()
    {
        double[] signal = [1, 2, 3, 4, 5];
        double[] weights = [0.1, 0.3, 0.6];
        double[] expected = [2.5, 3.5, 4.5];

        var result = MovingAverage.WeightedMovingAverage(signal, weights);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void WeightedMovingAverage_NullSignal_ThrowsArgumentNullException()
    {
        double[] signal = null!;
        double[] weights = [0.1, 0.3, 0.6];

        Assert.Throws<ArgumentNullException>(() => MovingAverage.WeightedMovingAverage(signal, weights));
    }

    [Test]
    public void WeightedMovingAverage_NullWeights_ThrowsArgumentNullException()
    {
        double[] signal = [1, 2, 3, 4, 5];
        double[] weights = null!;

        Assert.Throws<ArgumentNullException>(() => MovingAverage.WeightedMovingAverage(signal, weights));
    }

    [Test]
    public void WeightedMovingAverage_ZeroLengthWeights_ThrowsArgumentOutOfRangeException()
    {
        double[] signal = [1, 2, 3, 4, 5];
        double[] weights = [];

        Assert.Throws<ArgumentOutOfRangeException>(() => MovingAverage.WeightedMovingAverage(signal, weights));
    }

    [Test]
    public void WeightedMovingAverage_WeightsLengthGreaterThanSignal_ThrowsArgumentOutOfRangeException()
    {
        double[] signal = [1, 2, 3];
        double[] weights = [0.1, 0.3, 0.6, 0.8];

        Assert.Throws<ArgumentOutOfRangeException>(() => MovingAverage.WeightedMovingAverage(signal, weights));
    }
}
