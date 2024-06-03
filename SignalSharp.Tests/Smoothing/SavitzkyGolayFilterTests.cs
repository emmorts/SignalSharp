using SignalSharp.Smoothing.SavitzkyGolay;

namespace SignalSharp.Tests.Smoothing;

[TestFixture]
public class SavitzkyGolayFilterTests
{
    [Test]
    public void SavitzkyGolayFilter_SimpleInput_ReturnsFilteredOutput()
    {
        const int windowSize = 3;
        const int polynomialOrder = 1;
        
        double[] x = [1, 2, 3, 4, 5];
        double[] expected = [1, 2, 3, 4, 5];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_LargerInput_ReturnsFilteredOutput()
    {
        const int windowSize = 5;
        const int polynomialOrder = 2;
        
        double[] x = [2, 2.5, 3.4, 2.7, 2.6, 5.4, 6.2, 7.2, 4.2, 3.5, 3.25, 2];
        double[] expected = [1.96, 2.7, 3.4, 2.7, 2.6, 5.4, 6.2, 7.2, 4.2, 3.5, 2.61, 2.32];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder);

        Assert.That(expected, Is.EqualTo(result).Within(1e-2));
    }

    [Test]
    public void SavitzkyGolayFilter_InvalidPolyOrder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var savitzkyGolay = SavitzkyGolayFilter.Apply([], 2, 3);
        });
    }

    [Test]
    public void SavitzkyGolayFilter_EmptyInput_ReturnsEmptyOutput()
    {
        var result = SavitzkyGolayFilter.Apply([], 3, 1);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SavitzkyGolayFilter_InputSmallerThanWindowLength_ReturnsInputAsOutput()
    {
        const int windowSize = 5;
        const int polynomialOrder = 2;
        
        double[] x = [1, 2, 3];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder);

        Assert.That(x, Is.EqualTo(result));
    }
    
    [Test]
    public void SavitzkyGolayFilter_NegativeInputValues_ReturnsFilteredOutput()
    {
        const int windowSize = 3;
        const int polynomialOrder = 1;
        
        double[] x = [-1, -2, -3, -4, -5, -6, -7, -8];
        double[] expected = [-1, -2, -3, -4, -5, -6, -7, -8];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_ConstantInput_ReturnsSameOutput()
    {
        const int windowSize = 5;
        const int polynomialOrder = 2;
        
        double[] x = [3, 3, 3, 3, 3, 3, 3, 3, 3];
        double[] expected = [3, 3, 3, 3, 3, 3, 3, 3, 3];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_IncreasingLinearInput_ReturnsSameOutput()
    {
        const int windowSize = 3;
        const int polynomialOrder = 1;
        
        double[] x = [1, 2, 3, 4, 5, 6, 7];
        double[] expected = [1, 2, 3, 4, 5, 6, 7];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }
}