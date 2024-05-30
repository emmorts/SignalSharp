using SignalSharp.Filters.SavitzkyGolay;
using SignalSharp.Filters.SavitzkyGolay.Exceptions;

namespace SignalSharp.Tests.Filters;

[TestFixture]
public class SavitzkyGolayFilterTests
{
    [Test]
    public void SavitzkyGolayFilter_SimpleInput_ReturnsFilteredOutput()
    {
        var savitzkyGolay = new SavitzkyGolay(3, 1);
        
        double[] x = [1, 2, 3, 4, 5];
        double[] expected = [1, 2, 3, 4, 5];

        var result = savitzkyGolay.Filter(x);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_LargerInput_ReturnsFilteredOutput()
    {
        var savitzkyGolay = new SavitzkyGolay(5, 2);
        
        double[] x = [2, 2.5, 3.4, 2.7, 2.6, 5.4, 6.2, 7.2, 4.2, 3.5, 3.25, 2];
        double[] expected = [1.96, 2.7, 3.4, 2.7, 2.6, 5.4, 6.2, 7.2, 4.2, 3.5, 2.61, 2.32];

        var result = savitzkyGolay.Filter(x);

        Assert.That(expected, Is.EqualTo(result).Within(1e-2));
    }

    [Test]
    public void SavitzkyGolayFilter_InvalidPolyOrder_ThrowsArgumentException()
    {
        Assert.Throws<SavitzkyGolayInvalidPolynomialOrderException>(() =>
        {
            var savitzkyGolay = new SavitzkyGolay(2, 3);
        });
    }

    [Test]
    public void SavitzkyGolayFilter_EmptyInput_ReturnsEmptyOutput()
    {
        var savitzkyGolay = new SavitzkyGolay(3, 1);

        var result = savitzkyGolay.Filter([]);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SavitzkyGolayFilter_InputSmallerThanWindowLength_ReturnsInputAsOutput()
    {
        var savitzkyGolay = new SavitzkyGolay(5, 2);
        
        double[] x = [1, 2, 3];

        var result = savitzkyGolay.Filter(x);

        Assert.That(x, Is.EqualTo(result));
    }
    
    [Test]
    public void SavitzkyGolayFilter_NegativeInputValues_ReturnsFilteredOutput()
    {
        var savitzkyGolay = new SavitzkyGolay(3, 1);
        
        double[] x = [-1, -2, -3, -4, -5, -6, -7, -8];
        double[] expected = [-1, -2, -3, -4, -5, -6, -7, -8];

        var result = savitzkyGolay.Filter(x);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_ConstantInput_ReturnsSameOutput()
    {
        var savitzkyGolay = new SavitzkyGolay(5, 2);
        
        double[] x = [3, 3, 3, 3, 3, 3, 3, 3, 3];
        double[] expected = [3, 3, 3, 3, 3, 3, 3, 3, 3];

        var result = savitzkyGolay.Filter(x);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void SavitzkyGolayFilter_IncreasingLinearInput_ReturnsSameOutput()
    {
        var savitzkyGolay = new SavitzkyGolay(3, 1);
        
        double[] x = [1, 2, 3, 4, 5, 6, 7];
        double[] expected = [1, 2, 3, 4, 5, 6, 7];

        var result = savitzkyGolay.Filter(x);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }
}