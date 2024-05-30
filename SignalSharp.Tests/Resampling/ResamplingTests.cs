using SignalSharp.Resampling;

namespace SignalSharp.Tests.Resampling;

[TestFixture]
public class ResamplingTests
{
    [Test]
    public void Downsample_ShouldThrowArgumentNullException_WhenSignalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.Downsample(null!, 2));
    }

    [Test]
    public void Downsample_ShouldThrowArgumentOutOfRangeException_WhenFactorIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.Downsample([1, 2, 3], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.Downsample([1, 2, 3], -1));
    }

    [Test]
    public void Downsample_ShouldReturnCorrectResult_WhenCalledWithValidInputs()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];
        
        var result = SignalSharp.Resampling.Resampling.Downsample(signal, 2);
        double[] expected = [1, 3, 5];
        
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldThrowArgumentNullException_WhenSignalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.SegmentMedian(null!, 2));
    }

    [Test]
    public void SegmentMedian_ShouldThrowArgumentNullException_WhenSignalIsNull_DisableQuickSelect()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.SegmentMedian(null!, 2, false));
    }

    [Test]
    public void SegmentMedian_ShouldThrowArgumentOutOfRangeException_WhenFactorIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMedian([1, 2, 3], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMedian([1, 2, 3], -1));
    }

    [Test]
    public void SegmentMedian_ShouldThrowArgumentOutOfRangeException_WhenFactorIsZeroOrNegative_DisableQuickSelect()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMedian([1, 2, 3], 0, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMedian([1, 2, 3], -1, false));
    }

    [Test]
    public void SegmentMedian_ShouldReturnCorrectResult_WhenCalledWithValidInputs()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 2);
        double[] expected = [1.5, 3.5, 5.5];
        
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldReturnCorrectResult_WhenCalledWithValidInputs_DisableQuickSelect()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 2, false);
        double[] expected = [1.5, 3.5, 5.5];
        
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldHandleOddLengthFactor()
    {
        double[] signal = [1, 3, 2, 5, 4, 6, 7];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 3);
        double[] expected = [2, 5, 7];
        
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldHandleOddLengthFactor_DisableQuickSelect()
    {
        double[] signal = [1, 3, 2, 5, 4, 6, 7];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 3, false);
        double[] expected = [2, 5, 7];
        
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldHandleSingleElementSignal()
    {
        double[] signal = [1];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 2);
        double[] expected = [1];
        
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldHandleSingleElementSignal_DisableQuickSelect()
    {
        double[] signal = [1];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 2, false);
        double[] expected = [1];
        
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldHandleFactorLargerThanSignalLength()
    {
        double[] signal = [1, 2, 3];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 5);
        
        double[] expected = [2];
        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMedian_ShouldHandleFactorLargerThanSignalLength_DisableQuickSelect()
    {
        double[] signal = [1, 2, 3];
        
        var result = SignalSharp.Resampling.Resampling.SegmentMedian(signal, 5, false);
        
        double[] expected = [2];
        Assert.That(expected, Is.EqualTo(result));
    }
    
    [Test]
    public void SegmentMean_ShouldThrowArgumentNullException_WhenSignalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.SegmentMean(null!, 2));
    }

    [Test]
    public void SegmentMean_ShouldThrowArgumentOutOfRangeException_WhenFactorIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMean([1, 2, 3], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMean([1, 2, 3], -1));
    }

    [Test]
    public void SegmentMean_ShouldReturnCorrectResult_WhenCalledWithValidInputs()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];

        var result = SignalSharp.Resampling.Resampling.SegmentMean(signal, 2);
        double[] expected = [1.5, 3.5, 5.5];

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMax_ShouldThrowArgumentNullException_WhenSignalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.SegmentMax(null!, 2));
    }

    [Test]
    public void SegmentMax_ShouldThrowArgumentOutOfRangeException_WhenFactorIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMax([1, 2, 3], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMax([1, 2, 3], -1));
    }

    [Test]
    public void SegmentMax_ShouldReturnCorrectResult_WhenCalledWithValidInputs()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];

        var result = SignalSharp.Resampling.Resampling.SegmentMax(signal, 2);
        double[] expected = [2, 4, 6];

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void SegmentMin_ShouldThrowArgumentNullException_WhenSignalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.SegmentMin(null!, 2));
    }

    [Test]
    public void SegmentMin_ShouldThrowArgumentOutOfRangeException_WhenFactorIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMin([1, 2, 3], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.SegmentMin([1, 2, 3], -1));
    }

    [Test]
    public void SegmentMin_ShouldReturnCorrectResult_WhenCalledWithValidInputs()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];

        var result = SignalSharp.Resampling.Resampling.SegmentMin(signal, 2);
        double[] expected = [1, 3, 5];

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void MovingAverage_ShouldThrowArgumentNullException_WhenSignalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.MovingAverage(null!, 2));
    }

    [Test]
    public void MovingAverage_ShouldThrowArgumentOutOfRangeException_WhenWindowSizeIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.MovingAverage([1, 2, 3], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.MovingAverage([1, 2, 3], -1));
    }

    [Test]
    public void MovingAverage_ShouldReturnCorrectResult_WhenCalledWithValidInputs()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];

        var result = SignalSharp.Resampling.Resampling.MovingAverage(signal, 2);
        double[] expected = [1.5, 2.5, 3.5, 4.5, 5.5];

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void ChebyshevApproximation_ShouldThrowArgumentNullException_WhenSignalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalSharp.Resampling.Resampling.ChebyshevApproximation(null!, 2));
    }

    [Test]
    public void ChebyshevApproximation_ShouldThrowArgumentOutOfRangeException_WhenOrderIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.ChebyshevApproximation([1, 2, 3], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignalSharp.Resampling.Resampling.ChebyshevApproximation([1, 2, 3], -1));
    }

    [Test]
    public void ChebyshevApproximation_ShouldReturnCorrectResult_WhenCalledWithValidInputs()
    {
        double[] signal = [1, 2, 3, 4, 5, 6];
        const int order = 3;

        var result = SignalSharp.Resampling.Resampling.ChebyshevApproximation(signal, order);
        double[] expected = [4.51196, 5.46731, 6.54466, 7.45534, 8.53269, 9.48803];

        Assert.That(expected, Is.EqualTo(result).Within(1e-5));
    }
}