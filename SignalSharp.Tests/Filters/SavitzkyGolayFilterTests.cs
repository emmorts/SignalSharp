using SignalSharp.Filters.SavitzkyGolay;
using SignalSharp.Filters.SavitzkyGolay.Exceptions;

namespace SignalSharp.Tests.Filters;

[TestFixture]
public class SavitzkyGolayFilterTests
{
    [Test]
    public void SavitzkyGolayFilter_SimpleInput_ReturnsFilteredOutput()
    {
        const int windowLength = 3;
        const int polyOrder = 1;
        
        double[] x = [1, 2, 3, 4, 5];
        double[] expected = [1, 2, 3, 4, 5];

        var result = SavitzkyGolay.Filter(x, windowLength, polyOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_LargerInput_ReturnsFilteredOutput()
    {
        const int windowLength = 5;
        const int polyOrder = 2;
        
        double[] x = [2, 2.5, 3.4, 2.7, 2.6, 5.4, 6.2, 7.2, 4.2, 3.5, 3.25, 2];
        double[] expected = [1.96, 2.7, 3.4, 2.7, 2.6, 5.4, 6.2, 7.2, 4.2, 3.5, 2.61, 2.32];

        var result = SavitzkyGolay.Filter(x, windowLength, polyOrder);

        Assert.That(expected, Is.EqualTo(result).Within(1e-2));
    }

    [Test]
    public void SavitzkyGolayFilter_InvalidPolyOrder_ThrowsArgumentException()
    {
        const int windowLength = 2;
        const int polyOrder = 3;
        
        double[] x = [1, 2, 3, 4, 5];

        Assert.Throws<SavitzkyGolayInvalidPolynomialOrderException>(() => SavitzkyGolay.Filter(x, windowLength, polyOrder));
    }

    [Test]
    public void SavitzkyGolayFilter_EmptyInput_ReturnsEmptyOutput()
    {
        const int windowLength = 3;
        const int polyOrder = 1;

        var result = SavitzkyGolay.Filter([], windowLength, polyOrder);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SavitzkyGolayFilter_InputSmallerThanWindowLength_ReturnsInputAsOutput()
    {
        double[] x = [1, 2, 3];
        const int windowLength = 5;
        const int polyOrder = 2;

        var result = SavitzkyGolay.Filter(x, windowLength, polyOrder);

        Assert.That(x, Is.EqualTo(result));
    }
    
    [Test]
    public void SavitzkyGolayFilter_NegativeInputValues_ReturnsFilteredOutput()
    {
        const int windowLength = 3;
        const int polyOrder = 1;
        
        double[] x = [-1, -2, -3, -4, -5, -6, -7, -8];
        double[] expected = [-1, -2, -3, -4, -5, -6, -7, -8];

        var result = SavitzkyGolay.Filter(x, windowLength, polyOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_ConstantInput_ReturnsSameOutput()
    {
        const int windowLength = 5;
        const int polyOrder = 2;
        
        double[] x = [3, 3, 3, 3, 3, 3, 3, 3, 3];
        double[] expected = [3, 3, 3, 3, 3, 3, 3, 3, 3];

        var result = SavitzkyGolay.Filter(x, windowLength, polyOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_IncreasingLinearInput_ReturnsSameOutput()
    {
        const int windowLength = 3;
        const int polyOrder = 1;
        
        double[] x = [1, 2, 3, 4, 5, 6, 7];
        double[] expected = [1, 2, 3, 4, 5, 6, 7];

        var result = SavitzkyGolay.Filter(x, windowLength, polyOrder);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }
}