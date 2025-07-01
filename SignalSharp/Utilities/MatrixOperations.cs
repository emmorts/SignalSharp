// ReSharper disable InconsistentNaming

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace SignalSharp.Utilities;

/// <summary>
/// Provides operations for matrix manipulation and arithmetic.
/// <para>
/// The class includes methods to transpose matrices and perform matrix multiplication.
/// </para>
/// </summary>
public static class MatrixOperations
{
    private const double SingularityTolerance = 1e-12;

    /// <summary>
    /// Transposes the given matrix.
    /// </summary>
    /// <typeparam name="T">The numeric type of the matrix elements, implementing <see cref="IFloatingPointIeee754{TSelf}"/>.</typeparam>
    /// <param name="matrix">The matrix to transpose.</param>
    /// <returns>The transposed matrix.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="matrix"/> is null.</exception>
    /// <example>
    /// <code>
    /// float[,] matrix = { {1f, 2f}, {3f, 4f}, {5f, 6f} };
    /// float[,] result = MatrixOperations.Transpose<float>(matrix);
    /// // result is { {1f, 3f, 5f}, {2f, 4f, 6f} }
    /// </code>
    /// </example>
    public static T[,] Transpose<T>(T[,] matrix)
        where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(matrix);

        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        var transposed = new T[cols, rows];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                transposed[j, i] = matrix[i, j];
            }
        }

        return transposed;
    }

    /// <summary>
    /// Adds two matrices of the same dimensions.
    /// </summary>
    /// <typeparam name="T">The numeric type of the matrix elements, implementing <see cref="IFloatingPointIeee754{TSelf}"/>.</typeparam>
    /// <param name="matrixA">The first matrix.</param>
    /// <param name="matrixB">The second matrix.</param>
    /// <returns>A new matrix representing the sum of <paramref name="matrixA"/> and <paramref name="matrixB"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="matrixA"/> or <paramref name="matrixB"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the dimensions of <paramref name="matrixA"/> and <paramref name="matrixB"/> do not match.</exception>
    public static T[,] Add<T>(T[,] matrixA, T[,] matrixB)
        where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(matrixA);
        ArgumentNullException.ThrowIfNull(matrixB);

        int rowsA = matrixA.GetLength(0);
        int colsA = matrixA.GetLength(1);
        int rowsB = matrixB.GetLength(0);
        int colsB = matrixB.GetLength(1);

        if (rowsA != rowsB || colsA != colsB)
        {
            throw new ArgumentException("Matrices must have the same dimensions for addition.");
        }

        var result = new T[rowsA, colsA];

        for (int i = 0; i < rowsA; i++)
        {
            for (int j = 0; j < colsA; j++)
            {
                result[i, j] = matrixA[i, j] + matrixB[i, j];
            }
        }

        return result;
    }

    /// <summary>
    /// Multiplies two matrices and returns the result.
    /// </summary>
    /// <typeparam name="T">The numeric type of the matrix elements, implementing <see cref="IFloatingPointIeee754{TSelf}"/>.</typeparam>
    /// <param name="matrixA">The first matrix.</param>
    /// <param name="matrixB">The second matrix.</param>
    /// <returns>The product of the two matrices.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="matrixA"/> or <paramref name="matrixB"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the inner dimensions of the matrices do not match for multiplication.</exception>
    /// <example>
    /// <code>
    /// float[,] A = { {1f, 2f}, {3f, 4f} };
    /// float[,] B = { {5f, 6f}, {7f, 8f} };
    /// float[,] result = MatrixOperations.Multiply<float>(A, B);
    /// // result is { {19f, 22f}, {43f, 50f} }
    /// </code>
    /// </example>
    public static T[,] Multiply<T>(T[,] matrixA, T[,] matrixB)
        where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(matrixA);
        ArgumentNullException.ThrowIfNull(matrixB);

        int rowsA = matrixA.GetLength(0);
        int colsA = matrixA.GetLength(1);
        int rowsB = matrixB.GetLength(0);
        int colsB = matrixB.GetLength(1);

        if (colsA != rowsB)
        {
            throw new ArgumentException("Inner dimensions of matrices do not match for multiplication.");
        }

        var result = new T[rowsA, colsB];

        for (int i = 0; i < rowsA; i++)
        {
            for (int j = 0; j < colsB; j++)
            {
                T sum = T.Zero;
                for (int k = 0; k < colsA; k++)
                {
                    sum += matrixA[i, k] * matrixB[k, j];
                }

                result[i, j] = sum;
            }
        }

        return result;
    }

    /// <summary>
    /// Multiplies a matrix by a vector and returns the result.
    /// </summary>
    /// <typeparam name="T">The numeric type of the elements, implementing <see cref="IFloatingPointIeee754{TSelf}"/>.</typeparam>
    /// <param name="matrixA">The matrix.</param>
    /// <param name="vectorB">The vector.</param>
    /// <returns>The product of the matrix and the vector as a new vector.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="matrixA"/> or <paramref name="vectorB"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the number of columns in the matrix does not match the length of the vector.</exception>
    /// <example>
    /// <code>
    /// float[,] A = { {1f, 2f}, {3f, 4f}, {5f, 6f} };
    /// float[] B = { 7f, 8f };
    /// float[] result = MatrixOperations.Multiply<float>(A, B);
    /// // result is { 23f, 53f, 83f }
    /// </code>
    /// </example>
    public static T[] Multiply<T>(T[,] matrixA, T[] vectorB)
        where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(matrixA);
        ArgumentNullException.ThrowIfNull(vectorB);

        int rowsA = matrixA.GetLength(0);
        int colsA = matrixA.GetLength(1);

        if (colsA != vectorB.Length)
        {
            throw new ArgumentException("Number of columns in the matrix must match the length of the vector.");
        }

        var result = new T[rowsA];

        for (int i = 0; i < rowsA; i++)
        {
            T sum = T.Zero;
            for (int k = 0; k < colsA; k++)
            {
                sum += matrixA[i, k] * vectorB[k];
            }

            result[i] = sum;
        }

        return result;
    }

    /// <summary>
    /// Multiplies a matrix by a scalar value.
    /// </summary>
    /// <typeparam name="T">The numeric type of the matrix elements and scalar, implementing <see cref="IFloatingPointIeee754{TSelf}"/>.</typeparam>
    /// <param name="scalar">The scalar value to multiply by.</param>
    /// <param name="matrix">The matrix to be multiplied.</param>
    /// <returns>A new matrix representing the product of the <paramref name="scalar"/> and <paramref name="matrix"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="matrix"/> is null.</exception>
    public static T[,] ScalarMultiply<T>(T scalar, T[,] matrix)
        where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(matrix);

        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        var result = new T[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = scalar * matrix[i, j];
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates combinations C(n, k).
    /// </summary>
    public static T Combinations<T>(int n, int k)
        where T : IFloatingPointIeee754<T>
    {
        if (k < 0 || k > n)
            return T.Zero;
        if (k == 0 || k == n)
            return T.One;
        if (k > n / 2)
            k = n - k; // take advantage of symmetry C(n, k) = C(n, n-k)

        T result = T.One;
        for (int i = 1; i <= k; i++)
        {
            result = result * T.CreateChecked(n - i + 1) / T.CreateChecked(i);
        }

        return result;
    }

    /// <summary>
    /// Inverts the given matrix.
    /// <para>
    /// The method uses the Gauss-Jordan elimination method to compute the inverse of a matrix.
    /// </para>
    /// </summary>
    /// <param name="matrix">The matrix to invert.</param>
    /// <returns>The inverse of the matrix.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the matrix is not square or is singular (non-invertible).
    /// </exception>
    /// <example>
    /// <code>
    /// double[,] matrix = { {1, 2}, {3, 4} };
    /// double[,] result = MatrixOperations.Inverse(matrix);
    /// // result is { {-2, 1}, {1.5, -0.5} }
    /// </code>
    /// </example>
    public static double[,] Inverse(double[,] matrix)
    {
        var n = matrix.GetLength(0);
        if (n != matrix.GetLength(1))
        {
            throw new ArgumentException("Matrix must be square.");
        }

        var augmented = CreateAugmentedMatrix(matrix, n);
        PerformGaussJordanElimination(augmented, n);
        return ExtractInverseMatrix(augmented, n);
    }

    /// <summary>
    /// Solves a system of linear equations Ax = b.
    /// For square systems (A is n x n), it uses Gaussian elimination.
    /// For overdetermined systems (A is m x n, m > n), it solves the normal equations A^T A x = A^T b using Gaussian elimination.
    /// </summary>
    /// <typeparam name="T">The numeric type of the elements, implementing <see cref="IFloatingPointIeee754{TSelf}"/>.</typeparam>
    /// <param name="A">The matrix representing the system of linear equations.
    /// If A is square, it will be modified in place. If A is overdetermined, it is not modified.</param>
    /// <param name="b">The vector representing the right-hand side of the equations.
    /// If A is square, b will be modified in place. If A is overdetermined, b is not modified.</param>
    /// <returns>An array of solutions for the system of linear equations.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="A"/> or <paramref name="b"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if dimensions are incompatible, if the matrix is singular or ill-conditioned, or if the system is underdetermined.
    /// </exception>
    /// <remarks>
    /// For square systems, this method modifies the input matrix <paramref name="A"/> and vector <paramref name="b"/> in place for efficiency.
    /// For overdetermined systems, intermediate matrices derived from <paramref name="A"/> and <paramref name="b"/> are created and modified,
    /// leaving the original <paramref name="A"/> and <paramref name="b"/> unchanged by the Gaussian elimination step.
    /// </remarks>
    public static T[] SolveLinearSystem<T>(T[,] A, T[] b)
        where T : IFloatingPointIeee754<T>
    {
        if (!TrySolveLinearSystem(A, b, out T[]? solution, out string? errorMessage))
        {
            throw new ArgumentException(errorMessage ?? "Failed to solve linear system for unspecified reasons.");
        }
        return solution!;
    }

    /// <summary>
    /// Attempts to solve a system of linear equations Ax = b.
    /// For square systems (A is n x n), it uses Gaussian elimination.
    /// For overdetermined systems (A is m x n, m > n), it solves the normal equations A^T A x = A^T b using Gaussian elimination.
    /// </summary>
    /// <typeparam name="T">The numeric type of the elements, implementing <see cref="IFloatingPointIeee754{TSelf}"/>.</typeparam>
    /// <param name="A">The matrix representing the system of linear equations.
    /// If A is square, it will be modified in place. If A is overdetermined, it is not modified.</param>
    /// <param name="b">The vector representing the right-hand side of the equations.
    /// If A is square, b will be modified in place. If A is overdetermined, b is not modified.</param>
    /// <param name="solution">When this method returns, contains the solution vector if the solution was found; otherwise, null.</param>
    /// <param name="errorMessage">When this method returns, contains an error message if the solution failed; otherwise, null.</param>
    /// <returns>True if the system was solved successfully; otherwise, false.</returns>
    /// <remarks>
    /// For square systems, this method modifies the input matrix <paramref name="A"/> and vector <paramref name="b"/> in place for efficiency.
    /// For overdetermined systems, intermediate matrices derived from <paramref name="A"/> and <paramref name="b"/> are created and modified,
    /// leaving the original <paramref name="A"/> and <paramref name="b"/> unchanged by the Gaussian elimination step.
    /// Underdetermined systems (fewer rows than columns in A) are not supported.
    /// </remarks>
    public static bool TrySolveLinearSystem<T>(T[,] A, T[] b, [NotNullWhen(true)] out T[]? solution, out string? errorMessage)
        where T : IFloatingPointIeee754<T>
    {
        solution = null;
        errorMessage = null;

        int rowsA = A.GetLength(0);
        int colsA = A.GetLength(1);

        if (rowsA != b.Length)
        {
            errorMessage = $"Number of rows in matrix A ({rowsA}) must match the length of vector b ({b.Length}).";
            return false;
        }

        // handle empty system cases
        if (rowsA == 0)
        {
            if (colsA == 0)
            {
                solution = [];
                return true;
            }

            errorMessage = "System has 0 equations but non-zero variables; cannot determine a unique solution.";
            return false;
        }

        // handle zero variables case
        if (colsA == 0)
        {
            var epsilon = NumericUtils.GetDefaultEpsilon<T>();
            for (int i = 0; i < rowsA; ++i)
            {
                if (!NumericUtils.IsEffectivelyZero(b[i], epsilon))
                {
                    errorMessage = "System has no variables but non-zero values in b vector, leading to inconsistency (0 = non-zero).";
                    return false;
                }
            }
            solution = [];
            return true;
        }

        if (rowsA == colsA)
        {
            return SolveSquareSystemInPlace(A, b, out solution, out errorMessage);
        }

        if (rowsA > colsA)
        {
            T[,] A_T = Transpose(A);
            T[,] A_T_A = Multiply(A_T, A);
            T[] A_T_b = Multiply(A_T, b);

            return SolveSquareSystemInPlace(A_T_A, A_T_b, out solution, out errorMessage);
        }

        errorMessage = "Underdetermined systems (fewer rows/equations than columns/variables in A) are not supported for a unique solution.";
        return false;
    }

    /// <summary>
    /// Solves a square system of linear equations Ax = b using Gaussian elimination with partial pivoting.
    /// Modifies A_square and b_square in place.
    /// </summary>
    private static bool SolveSquareSystemInPlace<T>(T[,] A_square, T[] b_square, out T[]? solution, out string? errorMessage)
        where T : IFloatingPointIeee754<T>
    {
        solution = null;
        errorMessage = null;

        int n = b_square.Length;
        var epsilon = NumericUtils.GetDefaultEpsilon<T>();

        // Forward Elimination with Partial Pivoting
        for (int i = 0; i < n; i++)
        {
            // find row with maximum pivot element
            int maxRow = i;
            for (int k = i + 1; k < n; k++)
            {
                if (T.Abs(A_square[k, i]) > T.Abs(A_square[maxRow, i]))
                {
                    maxRow = k;
                }
            }

            // swap rows if needed
            if (maxRow != i)
            {
                for (int k = i; k < n; k++)
                {
                    (A_square[maxRow, k], A_square[i, k]) = (A_square[i, k], A_square[maxRow, k]);
                }
                (b_square[maxRow], b_square[i]) = (b_square[i], b_square[maxRow]);
            }

            // check for singularity
            if (NumericUtils.IsEffectivelyZero(A_square[i, i], epsilon))
            {
                errorMessage = $"Matrix is singular or ill-conditioned at row {i}. Pivot element too small: {A_square[i, i]}.";
                return false;
            }

            // eliminate below
            for (int k = i + 1; k < n; k++)
            {
                T factor = A_square[k, i] / A_square[i, i];
                b_square[k] -= factor * b_square[i];

                for (int j = i; j < n; j++)
                {
                    A_square[k, j] -= factor * A_square[i, j];
                }
                A_square[k, i] = T.Zero;
            }
        }

        // back Substitution
        var x = new T[n];
        for (int i = n - 1; i >= 0; i--)
        {
            T sum = T.Zero;
            for (int j = i + 1; j < n; j++)
            {
                sum += A_square[i, j] * x[j];
            }

            if (NumericUtils.IsEffectivelyZero(A_square[i, i], epsilon))
            {
                errorMessage = $"Matrix became singular during back-substitution at row {i}. Pivot too small: {A_square[i, i]}.";
                return false;
            }
            x[i] = (b_square[i] - sum) / A_square[i, i];
        }

        solution = x;
        return true;
    }

    /// <summary>
    /// Calculates the Euclidean norm of a specified column in a matrix using SIMD optimization.
    /// </summary>
    private static double CalculateNorm(double[,] A, int column, int rowCount, int vectorLength)
    {
        double sumOfSquares = 0;
        var columnValues = new double[rowCount];

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            columnValues[rowIndex] = A[rowIndex, column];
        }

        int index;
        for (index = 0; index <= rowCount - vectorLength; index += vectorLength)
        {
            var vector = new Vector<double>(columnValues, index);
            sumOfSquares += Vector.Dot(vector, vector);
        }

        for (; index < rowCount; index++)
        {
            sumOfSquares += columnValues[index] * columnValues[index];
        }

        return Math.Sqrt(sumOfSquares);
    }

    /// <summary>
    /// Normalizes a specified column in matrix A and stores the result in matrix Q.
    /// </summary>
    private static void NormalizeColumn(double[,] A, double[,] Q, int column, double norm, int rowCount)
    {
        for (var i = 0; i < rowCount; i++)
        {
            Q[i, column] = A[i, column] / norm;
        }
    }

    /// <summary>
    /// Calculates the dot product of two specified columns from matrices Q and A using SIMD optimization.
    /// </summary>
    private static double CalculateDotProduct(double[,] Q, double[,] A, int qColumn, int aColumn, int rowCount, int vectorLength)
    {
        double dotProductSum = 0;

        var columnQValues = new double[rowCount];
        var columnAValues = new double[rowCount];

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            columnQValues[rowIndex] = Q[rowIndex, qColumn];
            columnAValues[rowIndex] = A[rowIndex, aColumn];
        }

        int index;
        for (index = 0; index <= rowCount - vectorLength; index += vectorLength)
        {
            var vectorQ = new Vector<double>(columnQValues, index);
            var vectorA = new Vector<double>(columnAValues, index);
            dotProductSum += Vector.Dot(vectorQ, vectorA);
        }

        for (; index < rowCount; index++)
        {
            dotProductSum += columnQValues[index] * columnAValues[index];
        }

        return dotProductSum;
    }

    /// <summary>
    /// Updates a specified column in matrix A using the values from matrix Q and the calculated dot product.
    /// </summary>
    private static void UpdateColumn(double[,] A, double[,] Q, int qColumn, int aColumn, double dotProduct, int rowCount)
    {
        for (var i = 0; i < rowCount; i++)
        {
            A[i, aColumn] -= Q[i, qColumn] * dotProduct;
        }
    }

    /// <summary>
    /// Computes the product of the transpose of matrix Q and vector Y (Q^T * Y) using SIMD optimization.
    /// </summary>
    private static double[] ComputeQTransposeY(double[,] Q, double[] Y, int rowCount, int columnCount, int vectorLength)
    {
        var qTransposeY = new double[columnCount];
        var columnQValues = new double[rowCount];

        for (var colIndex = 0; colIndex < columnCount; colIndex++)
        {
            double dotProductSum = 0;

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                columnQValues[rowIndex] = Q[rowIndex, colIndex];
            }

            int index;
            for (index = 0; index <= rowCount - vectorLength; index += vectorLength)
            {
                var vectorQ = new Vector<double>(columnQValues, index);
                var vectorYPart = new Vector<double>(Y, index);
                dotProductSum += Vector.Dot(vectorQ, vectorYPart);
            }

            for (; index < rowCount; index++)
            {
                dotProductSum += columnQValues[index] * Y[index];
            }

            qTransposeY[colIndex] = dotProductSum;
        }

        return qTransposeY;
    }

    /// <summary>
    /// Solves the upper triangular system R * x = Q^T * y using back substitution.
    /// </summary>
    private static double[] BackSubstitution(double[,] R, double[] QTy, int dimension)
    {
        var solution = new double[dimension];

        for (var rowIndex = dimension - 1; rowIndex >= 0; rowIndex--)
        {
            solution[rowIndex] = QTy[rowIndex];

            for (var columnIndex = rowIndex + 1; columnIndex < dimension; columnIndex++)
            {
                solution[rowIndex] -= R[rowIndex, columnIndex] * solution[columnIndex];
            }

            solution[rowIndex] /= R[rowIndex, rowIndex];
        }

        return solution;
    }

    private static bool TryBackSubstitution(double[,] R, double[] QTy, int dimension, out double[] solution)
    {
        solution = new double[dimension];

        for (var rowIndex = dimension - 1; rowIndex >= 0; rowIndex--)
        {
            if (Math.Abs(R[rowIndex, rowIndex]) < SingularityTolerance)
            {
                return false;
            }

            solution[rowIndex] = QTy[rowIndex];

            for (var columnIndex = rowIndex + 1; columnIndex < dimension; columnIndex++)
            {
                solution[rowIndex] -= R[rowIndex, columnIndex] * solution[columnIndex];
            }

            solution[rowIndex] /= R[rowIndex, rowIndex];
        }

        return true;
    }

    /// <summary>
    /// Creates an augmented matrix by appending the identity matrix to the given matrix.
    /// </summary>
    private static double[,] CreateAugmentedMatrix(double[,] matrix, int dimension)
    {
        var augmented = new double[dimension, 2 * dimension];

        for (var i = 0; i < dimension; i++)
        {
            for (var j = 0; j < dimension; j++)
            {
                augmented[i, j] = matrix[i, j];
            }

            augmented[i, dimension + i] = 1;
        }

        return augmented;
    }

    /// <summary>
    /// Performs the Gauss-Jordan elimination process on the augmented matrix.
    /// </summary>
    private static void PerformGaussJordanElimination(double[,] augmented, int dimension)
    {
        for (var i = 0; i < dimension; i++)
        {
            var diagElement = augmented[i, i];
            if (Math.Abs(diagElement) < SingularityTolerance)
            {
                throw new ArgumentException("Matrix is singular and cannot be inverted.");
            }

            NormalizeRow(augmented, i, diagElement, dimension);
            EliminateOtherRows(augmented, i, dimension);
        }
    }

    /// <summary>
    /// Normalizes the given row in the augmented matrix by dividing all elements by the diagonal element.
    /// </summary>
    private static void NormalizeRow(double[,] augmented, int row, double diagElement, int dimension)
    {
        for (var j = 0; j < 2 * dimension; j++)
        {
            augmented[row, j] /= diagElement;
        }
    }

    /// <summary>
    /// Eliminates other rows in the augmented matrix to create zeros in the current column.
    /// </summary>
    private static void EliminateOtherRows(double[,] augmented, int currentRow, int dimension)
    {
        for (var k = 0; k < dimension; k++)
        {
            if (k == currentRow)
                continue;

            var factor = augmented[k, currentRow];
            for (var j = 0; j < 2 * dimension; j++)
            {
                augmented[k, j] -= factor * augmented[currentRow, j];
            }
        }
    }

    /// <summary>
    /// Extracts the inverse matrix from the augmented matrix.
    /// </summary>
    private static double[,] ExtractInverseMatrix(double[,] augmented, int dimension)
    {
        var inverse = new double[dimension, dimension];

        for (var i = 0; i < dimension; i++)
        {
            for (var j = 0; j < dimension; j++)
            {
                inverse[i, j] = augmented[i, j + dimension];
            }
        }

        return inverse;
    }
}
