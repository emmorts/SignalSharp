using SignalSharp.Utilities;

// ReSharper disable InconsistentNaming

namespace SignalSharp.Tests.Utilities;

[TestFixture]
public class MatrixOperationsTests
{
    [Test]
    public void Transpose_SquareMatrix_ReturnsTransposedMatrix()
    {
        double[,] matrix =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
            { 7, 8, 9 },
        };

        double[,] expected =
        {
            { 1, 4, 7 },
            { 2, 5, 8 },
            { 3, 6, 9 },
        };

        var result = MatrixOperations.Transpose(matrix);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void Transpose_RectangularMatrix_ReturnsTransposedMatrix()
    {
        double[,] matrix =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
        };

        double[,] expected =
        {
            { 1, 4 },
            { 2, 5 },
            { 3, 6 },
        };

        var result = MatrixOperations.Transpose(matrix);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void Inverse_ValidSquareMatrix_ReturnsInverseMatrix()
    {
        double[,] matrix =
        {
            { 4, 7 },
            { 2, 6 },
        };

        double[,] expected =
        {
            { 0.6, -0.7 },
            { -0.2, 0.4 },
        };

        var result = MatrixOperations.Inverse(matrix);

        Assert.That(result, Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void Inverse_IdentityMatrix_ReturnsIdentityMatrix()
    {
        double[,] matrix =
        {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 },
        };

        double[,] expected =
        {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 },
        };

        var result = MatrixOperations.Inverse(matrix);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Inverse_SingularMatrix_ThrowsArgumentException()
    {
        double[,] matrix =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
            { 7, 8, 9 },
        };

        Assert.Throws<ArgumentException>(() => MatrixOperations.Inverse(matrix));
    }

    [Test]
    public void Inverse_NonSquareMatrix_ThrowsArgumentException()
    {
        double[,] matrix =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
        };

        Assert.Throws<ArgumentException>(() => MatrixOperations.Inverse(matrix));
    }

    [Test]
    public void Multiply_TwoMatrices_ReturnsProductMatrix()
    {
        double[,] A =
        {
            { 1, 2 },
            { 3, 4 },
        };

        double[,] B =
        {
            { 2, 0 },
            { 1, 2 },
        };

        double[,] expected =
        {
            { 4, 4 },
            { 10, 8 },
        };

        var result = MatrixOperations.Multiply(A, B);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void Multiply_MatrixAndVector_ReturnsProductVector()
    {
        double[,] A =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
        };

        var B = new double[] { 7, 8, 9 };

        double[] expected = { 50, 122 };

        var result = MatrixOperations.Multiply(A, B);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void Multiply_IncompatibleMatrices_ThrowsException()
    {
        double[,] A =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
        };

        double[,] B =
        {
            { 7, 8 },
            { 9, 10 },
        };

        Assert.Throws<ArgumentException>(() => MatrixOperations.Multiply(A, B));
    }

    [Test]
    public void SolveCoefficients_ValidInput_ReturnsCorrectCoefficients()
    {
        double[,] A =
        {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 },
        };

        double[] y = [7, 8, 9];
        double[] expected = [-6, 6.5];

        var result = MatrixOperations.SolveLinearSystemQR(A, y);

        Assert.That(expected, Is.EqualTo(result).Within(1e-10));
    }
}
