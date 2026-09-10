using SignalSharp.Common.Models;
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
    public void SavitzkyGolayFilter_Decimal_SimpleInput_ReturnsFilteredOutput()
    {
        const int windowSize = 3;
        const int polynomialOrder = 1;

        decimal[] input = [1.0m, 2.0m, 3.0m, 4.0m, 5.0m];
        decimal[] expected = [1.0m, 2.0m, 3.0m, 4.0m, 5.0m];

        var result = SavitzkyGolayFilter.Apply(input, windowSize, polynomialOrder);

        Assert.That(result, Is.EqualTo(expected).Within(0.0000000001m));
    }

    [Test]
    public void SavitzkyGolayFilter_Int_SimpleInput_ReturnsFilteredOutput()
    {
        const int windowSize = 3;
        const int polynomialOrder = 1;

        int[] input = [1, 2, 3, 4, 5];
        int[] expected = [1, 2, 3, 4, 5];

        var result = SavitzkyGolayFilter.Apply(input, windowSize, polynomialOrder);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void SavitzkyGolayFilter_LargerInput_ReturnsFilteredOutput()
    {
        const int windowSize = 5;
        const int polynomialOrder = 2;

        double[] x = [2, 2.5, 3.4, 2.7, 2.6, 5.4, 6.2, 7.2, 4.2, 3.5, 3.25, 2];
        double[] expected =
        [
            1.96,
            2.7,
            3.04,
            2.691428571428571,
            3.217142857142857,
            4.791428571428571,
            6.748571428571429,
            6.3,
            4.898571428571429,
            3.465714285714285,
            2.612857142857143,
            2.324285714285714,
        ];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder);

        Assert.That(expected, Is.EqualTo(result).Within(1e-9));
    }

    [Test]
    public void SavitzkyGolayFilter_PreservesLinearSignalWithPadding()
    {
        const int windowSize = 5;
        const int polynomialOrder = 2;

        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder, 0, Padding.Mirror);

        // Interior samples of a linear signal are reproduced exactly; boundary samples are smoothed
        // by the mirror-padded convolution, so they stay within the range of the input.
        Assert.That(result[2..^2], Is.EqualTo(x[2..^2]).Within(1e-9));
        Assert.That(result, Is.All.GreaterThanOrEqualTo(1.0).And.All.LessThanOrEqualTo(12.0));
    }

    [Test]
    public void SavitzkyGolayFilter_DerivativeOfQuadratic_ReturnsExactDerivative()
    {
        const int windowSize = 5;
        const int polynomialOrder = 2;

        // Samples of (i + 1)^2, whose first derivative is 2 * (i + 1).
        double[] x = [1, 4, 9, 16, 25, 36, 49, 64, 81, 100, 121, 144];
        double[] expected = [.. Enumerable.Range(1, 12).Select(i => 2.0 * i)];

        var result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder, derivativeOrder: 1);

        Assert.That(result, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void SavitzkyGolayFilter_HighOrderEdgeFit_DoesNotThrowAndPreservesLinearSignal()
    {
        // High polynomial orders on wide windows make the edge-fit normal equations ill-conditioned; the fit
        // must still succeed and reproduce a linear ramp rather than being reported as singular.
        const int windowSize = 31;
        const int polynomialOrder = 10;

        double[] x = [.. Enumerable.Range(1, 2 * windowSize + 5).Select(i => 3.0 + 0.5 * i)];

        double[] result = [];

        Assert.That(() => result = SavitzkyGolayFilter.Apply(x, windowSize, polynomialOrder), Throws.Nothing);
        Assert.That(result, Is.EqualTo(x).Within(1e-8));
    }

    [Test]
    public void SavitzkyGolayFilter_EvenWindowLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SavitzkyGolayFilter.Apply([1.0, 2, 3, 4, 5], 4, 2));
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
