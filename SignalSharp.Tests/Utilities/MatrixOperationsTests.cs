using SignalSharp.Utilities;

// ReSharper disable InconsistentNaming

namespace SignalSharp.Tests.Utilities;

[TestFixture]
public class MatrixOperationsTests
{
    #region Transpose Tests

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
    public void Transpose_SingleElementMatrix_ReturnsSameMatrix()
    {
        double[,] matrix =
        {
            { 42 },
        };
        double[,] expected =
        {
            { 42 },
        };

        var result = MatrixOperations.Transpose(matrix);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void Transpose_EmptyMatrix_ReturnsEmptyMatrix()
    {
        double[,] matrix = new double[0, 0];
        double[,] expected = new double[0, 0];

        var result = MatrixOperations.Transpose(matrix);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetLength(0), Is.Zero);
            Assert.That(result.GetLength(1), Is.Zero);
        }
    }

    [Test]
    public void Transpose_NullMatrix_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Transpose<double>(null!));
    }

    #endregion

    #region Inverse Tests

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
    public void Inverse_InverseOfInverse_ReturnsOriginalMatrix()
    {
        double[,] original =
        {
            { 4, 7 },
            { 2, 6 },
        };

        var inverse = MatrixOperations.Inverse(original);
        var inverseOfInverse = MatrixOperations.Inverse(inverse);

        Assert.That(inverseOfInverse, Is.EqualTo(original).Within(1e-10));
    }

    [Test]
    public void Inverse_Product_ReturnsIdentity()
    {
        double[,] matrix =
        {
            { 4, 7 },
            { 2, 6 },
        };

        var inverse = MatrixOperations.Inverse(matrix);
        var product = MatrixOperations.Multiply(matrix, inverse);

        double[,] identity =
        {
            { 1, 0 },
            { 0, 1 },
        };

        Assert.That(product, Is.EqualTo(identity).Within(1e-10));
    }

    #endregion

    #region Addition Tests

    [Test]
    public void Add_TwoValidMatrices_ReturnsCorrectSum()
    {
        double[,] matrixA =
        {
            { 1, 2 },
            { 3, 4 },
        };

        double[,] matrixB =
        {
            { 5, 6 },
            { 7, 8 },
        };

        double[,] expected =
        {
            { 6, 8 },
            { 10, 12 },
        };

        var result = MatrixOperations.Add(matrixA, matrixB);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void Add_MatrixAndZeroMatrix_ReturnsOriginalMatrix()
    {
        double[,] matrixA =
        {
            { 1, 2 },
            { 3, 4 },
        };

        double[,] matrixB =
        {
            { 0, 0 },
            { 0, 0 },
        };

        var result = MatrixOperations.Add(matrixA, matrixB);

        Assert.That(matrixA, Is.EqualTo(result));
    }

    [Test]
    public void Add_MatricesWithDifferentDimensions_ThrowsArgumentException()
    {
        double[,] matrixA =
        {
            { 1, 2 },
            { 3, 4 },
        };

        double[,] matrixB =
        {
            { 5, 6, 7 },
            { 8, 9, 10 },
        };

        Assert.Throws<ArgumentException>(() => MatrixOperations.Add(matrixA, matrixB));
    }

    [Test]
    public void Add_NullMatrices_ThrowsArgumentNullException()
    {
        double[,] matrixA =
        {
            { 1, 2 },
            { 3, 4 },
        };

        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Add(matrixA, null!));
        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Add(null!, matrixA));
        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Add<double>(null!, null!));
    }

    #endregion

    #region Multiply Tests

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

        double[] expected = [50, 122];

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
    public void Multiply_MatrixWithIdentity_ReturnsOriginalMatrix()
    {
        double[,] matrix =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
        };

        double[,] identity =
        {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 },
        };

        var result = MatrixOperations.Multiply(matrix, identity);

        Assert.That(result, Is.EqualTo(matrix));
    }

    [Test]
    public void Multiply_IdentityWithMatrix_ReturnsOriginalMatrix()
    {
        double[,] matrix =
        {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 },
        };

        double[,] identity =
        {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 },
        };

        var result = MatrixOperations.Multiply(identity, matrix);

        Assert.That(result, Is.EqualTo(matrix));
    }

    [Test]
    public void Multiply_MatrixAndVectorIncompatibleDimensions_ThrowsException()
    {
        double[,] A =
        {
            { 1, 2 },
            { 3, 4 },
        };

        var B = new double[] { 5, 6, 7 };

        Assert.Throws<ArgumentException>(() => MatrixOperations.Multiply(A, B));
    }

    [Test]
    public void Multiply_NullInputs_ThrowsArgumentNullException()
    {
        double[,] A =
        {
            { 1, 2 },
            { 3, 4 },
        };

        double[] v = [5, 6];

        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Multiply(A, (double[,])null!));
        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Multiply(null!, A));
        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Multiply(null!, v));
        Assert.Throws<ArgumentNullException>(() => MatrixOperations.Multiply<double>(A, (double[])null!));
    }

    #endregion

    #region ScalarMultiply Tests

    [Test]
    public void ScalarMultiply_ValidInputs_ReturnsCorrectResult()
    {
        double[,] matrix =
        {
            { 1, 2 },
            { 3, 4 },
        };

        const double scalar = 2.5;

        double[,] expected =
        {
            { 2.5, 5 },
            { 7.5, 10 },
        };

        var result = MatrixOperations.ScalarMultiply(scalar, matrix);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void ScalarMultiply_ZeroScalar_ReturnsZeroMatrix()
    {
        double[,] matrix =
        {
            { 1, 2 },
            { 3, 4 },
        };

        double scalar = 0;

        double[,] expected =
        {
            { 0, 0 },
            { 0, 0 },
        };

        var result = MatrixOperations.ScalarMultiply(scalar, matrix);

        Assert.That(expected, Is.EqualTo(result));
    }

    [Test]
    public void ScalarMultiply_OneScalar_ReturnsSameMatrix()
    {
        double[,] matrix =
        {
            { 1, 2 },
            { 3, 4 },
        };

        const double scalar = 1;

        var result = MatrixOperations.ScalarMultiply(scalar, matrix);

        Assert.That(matrix, Is.EqualTo(result));
    }

    [Test]
    public void ScalarMultiply_NullMatrix_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MatrixOperations.ScalarMultiply(2.0, null!));
    }

    #endregion

    #region Combinations Tests

    [Test]
    public void Combinations_ValidInputs_ReturnsCorrectResults()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(MatrixOperations.Combinations<double>(5, 2), Is.EqualTo(10));
            Assert.That(MatrixOperations.Combinations<double>(10, 3), Is.EqualTo(120));
            Assert.That(MatrixOperations.Combinations<double>(7, 4), Is.EqualTo(35));
        }
    }

    [Test]
    public void Combinations_SpecialCases_ReturnsCorrectResults()
    {
        // C(n,0) = 1
        Assert.That(MatrixOperations.Combinations<double>(5, 0), Is.EqualTo(1));

        // C(n,n) = 1
        Assert.That(MatrixOperations.Combinations<double>(5, 5), Is.EqualTo(1));

        // C(n,1) = n
        Assert.That(MatrixOperations.Combinations<double>(7, 1), Is.EqualTo(7));

        // C(n,k) = C(n,n-k)
        Assert.That(MatrixOperations.Combinations<double>(8, 3), Is.EqualTo(MatrixOperations.Combinations<double>(8, 5)));
    }

    [Test]
    public void Combinations_EdgeCases_ReturnsCorrectResults()
    {
        // k > n returns 0
        Assert.That(MatrixOperations.Combinations<double>(3, 5), Is.Zero);

        // k < 0 returns 0
        Assert.That(MatrixOperations.Combinations<double>(5, -2), Is.Zero);

        // Edge case for C(0,0)
        Assert.That(MatrixOperations.Combinations<double>(0, 0), Is.EqualTo(1));
    }

    [Test]
    public void Combinations_LargeValues_MaintainsAccuracy()
    {
        // Known value for C(20,10)
        Assert.That(MatrixOperations.Combinations<double>(20, 10), Is.EqualTo(184756).Within(0.001));
    }

    #endregion

    #region SolveLinearSystem Tests

    [Test]
    public void SolveLinearSystem_ValidInput_ReturnsCorrectCoefficients()
    {
        double[,] A =
        {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 },
        };

        double[] y = [7, 8, 9];
        double[] expected = [-6, 6.5];

        var result = MatrixOperations.SolveLinearSystem(A, y);

        Assert.That(expected, Is.EqualTo(result).Within(1e-10));
    }

    [Test]
    public void SolveLinearSystem_SquareSystem_ReturnsCorrectSolution()
    {
        double[,] A =
        {
            { 2, 1, -1 },
            { -3, -1, 2 },
            { -2, 1, 2 },
        };

        double[] b = [8, -11, -3];
        double[] expected = [2, 3, -1];

        var result = MatrixOperations.SolveLinearSystem(A, b);

        Assert.That(expected, Is.EqualTo(result).Within(1e-10));
    }

    [Test]
    public void SolveLinearSystem_IncompatibleDimensions_ThrowsArgumentException()
    {
        double[,] A =
        {
            { 1, 2 },
            { 3, 4 },
        };

        double[] b = [5, 6, 7];

        Assert.Throws<ArgumentException>(() => MatrixOperations.SolveLinearSystem(A, b));
    }

    [Test]
    public void SolveLinearSystem_UnderdeterminedSystem_ThrowsArgumentException()
    {
        double[,] A =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
        };

        double[] b = [7, 8];

        Assert.Throws<ArgumentException>(() => MatrixOperations.SolveLinearSystem(A, b));
    }

    [Test]
    public void SolveLinearSystem_SingularMatrix_ThrowsArgumentException()
    {
        double[,] A =
        {
            { 1, 2, 3 },
            { 2, 4, 6 },
            { 3, 6, 9 },
        };

        double[] b = [7, 8, 9];

        Assert.Throws<ArgumentException>(() => MatrixOperations.SolveLinearSystem(A, b));
    }

    [Test]
    public void TrySolveLinearSystem_ValidInput_ReturnsTrue()
    {
        double[,] A =
        {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 },
        };

        double[] b = [7, 8, 9];

        bool success = MatrixOperations.TrySolveLinearSystem(A, b, out double[]? solution, out string? errorMessage);

        Assert.That(success, Is.True);
        Assert.That(solution, Is.Not.Null);
        Assert.That(errorMessage, Is.Null);

        double[] expected = [-6, 6.5];
        Assert.That(expected, Is.EqualTo(solution).Within(1e-10));
    }

    [Test]
    public void TrySolveLinearSystem_SingularMatrix_ReturnsFalse()
    {
        double[,] A =
        {
            { 1, 2, 3 },
            { 2, 4, 6 },
            { 3, 6, 9 },
        };

        double[] b = [7, 8, 9];

        bool success = MatrixOperations.TrySolveLinearSystem(A, b, out double[]? solution, out string? errorMessage);

        Assert.That(success, Is.False);
        Assert.That(solution, Is.Null);
        Assert.That(errorMessage, Is.Not.Null);
    }

    [Test]
    public void TrySolveLinearSystem_EmptySystem_ReturnsEmptySolution()
    {
        double[,] A = new double[0, 0];
        double[] b = [];

        bool success = MatrixOperations.TrySolveLinearSystem(A, b, out double[]? solution, out string? errorMessage);

        Assert.That(success, Is.True);
        Assert.That(solution, Is.Not.Null);
        Assert.That(solution, Is.Empty);
        Assert.That(errorMessage, Is.Null);
    }

    [Test]
    public void TrySolveLinearSystem_NoVariables_ReturnsEmptySolution()
    {
        double[,] A = new double[2, 0];
        double[] b = [0, 0];

        bool success = MatrixOperations.TrySolveLinearSystem(A, b, out double[]? solution, out string? errorMessage);

        Assert.That(success, Is.True);
        Assert.That(solution, Is.Not.Null);
        Assert.That(solution, Is.Empty);
        Assert.That(errorMessage, Is.Null);
    }

    [Test]
    public void TrySolveLinearSystem_NoVariablesWithNonZeroB_ReturnsFalse()
    {
        double[,] A = new double[2, 0];
        double[] b = [1, 2];

        bool success = MatrixOperations.TrySolveLinearSystem(A, b, out double[]? solution, out string? errorMessage);

        Assert.That(success, Is.False);
        Assert.That(solution, Is.Null);
        Assert.That(errorMessage, Is.Not.Null);
    }

    #endregion
}
