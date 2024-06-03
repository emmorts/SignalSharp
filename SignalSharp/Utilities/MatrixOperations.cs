// ReSharper disable InconsistentNaming

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
    /// <summary>
    /// Transposes the given matrix.
    /// <para>
    /// The transpose of a matrix is obtained by swapping its rows with its columns.
    /// </para>
    /// </summary>
    /// <param name="matrix">The matrix to transpose.</param>
    /// <returns>The transposed matrix.</returns>
    /// <example>
    /// <code>
    /// double[,] matrix = { {1, 2}, {3, 4}, {5, 6} };
    /// double[,] result = MatrixOperations.Transpose(matrix);
    /// // result is { {1, 3, 5}, {2, 4, 6} }
    /// </code>
    /// </example>
    public static double[,] Transpose(double[,] matrix)
    {
        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);
        var transposed = new double[cols, rows];

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                transposed[j, i] = matrix[i, j];
            }
        }

        return transposed;
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
    /// Multiplies two matrices and returns the result.
    /// <para>
    /// Matrix multiplication is only defined when the number of columns in the first matrix
    /// matches the number of rows in the second matrix.
    /// </para>
    /// </summary>
    /// <param name="A">The first matrix.</param>
    /// <param name="B">The second matrix.</param>
    /// <returns>The product of the two matrices.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the number of columns in A does not match the number of rows in B.
    /// </exception>
    /// <example>
    /// <code>
    /// double[,] A = { {1, 2}, {3, 4} };
    /// double[,] B = { {5, 6}, {7, 8} };
    /// double[,] result = MatrixOperations.Multiply(A, B);
    /// // result is { {19, 22}, {43, 50} }
    /// </code>
    /// </example>
    public static double[,] Multiply(double[,] A, double[,] B)
    {
        var aRows = A.GetLength(0);
        var aCols = A.GetLength(1);
        var bRows = B.GetLength(0);
        var bCols = B.GetLength(1);
        var result = new double[aRows, bCols];

        if (aCols != bRows)
        {
            throw new ArgumentException("Number of columns in A must match number of rows in B.");
        }

        for (var i = 0; i < aRows; i++)
        {
            for (var j = 0; j < bCols; j++)
            {
                var sum = Vector<double>.Zero;

                int k;
                for (k = 0; k <= aCols - Vector<double>.Count; k += Vector<double>.Count)
                {
                    var va = new double[Vector<double>.Count];
                    var vb = new double[Vector<double>.Count];

                    for (var v = 0; v < Vector<double>.Count; v++)
                    {
                        va[v] = A[i, k + v];
                        vb[v] = B[k + v, j];
                    }

                    sum += new Vector<double>(va) * new Vector<double>(vb);
                }

                result[i, j] = Vector.Dot(sum, Vector<double>.One);

                for (; k < aCols; k++)
                {
                    result[i, j] += A[i, k] * B[k, j];
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Multiplies a matrix by a vector and returns the result.
    /// <para>
    /// This operation is defined when the number of columns in the matrix matches the length of the vector.
    /// </para>
    /// </summary>
    /// <param name="A">The matrix.</param>
    /// <param name="B">The vector.</param>
    /// <returns>The product of the matrix and the vector.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the number of columns in the matrix does not match the length of the vector.
    /// </exception>
    /// <example>
    /// <code>
    /// double[,] A = { {1, 2}, {3, 4}, {5, 6} };
    /// double[] B = { 7, 8 };
    /// double[] result = MatrixOperations.Multiply(A, B);
    /// // result is { 23, 53, 83 }
    /// </code>
    /// </example>
    public static double[] Multiply(double[,] A, double[] B)
    {
        var aRows = A.GetLength(0);
        var aCols = A.GetLength(1);

        if (aCols != B.Length)
        {
            throw new ArgumentException("Number of columns in A must match the length of vector B.");
        }

        var result = new double[aRows];

        for (var i = 0; i < aRows; i++)
        {
            var sum = Vector<double>.Zero;

            int k;
            for (k = 0; k <= aCols - Vector<double>.Count; k += Vector<double>.Count)
            {
                var va = new double[Vector<double>.Count];
                var vb = new double[Vector<double>.Count];

                for (var v = 0; v < Vector<double>.Count; v++)
                {
                    va[v] = A[i, k + v];
                    vb[v] = B[k + v];
                }

                sum += new Vector<double>(va) * new Vector<double>(vb);
            }

            result[i] = Vector.Dot(sum, Vector<double>.One);

            for (; k < aCols; k++)
            {
                result[i] += A[i, k] * B[k];
            }
        }

        return result;
    }
    
    /// <summary>
    /// Solves a system of linear equations Ax = b using QR factorization with SIMD optimization.
    /// </summary>
    /// <param name="A">The matrix representing the system of linear equations.</param>
    /// <param name="y">The vector representing the right-hand side of the equations.</param>
    /// <returns>An array of solutions for the system of linear equations.</returns>
    /// <example>
    /// <code>
    /// double[,] A = { { 1, 2 }, { 3, 4 } };
    /// double[] y = { 5, 6 };
    /// double[] result = SolveLinearSystemQR(A, y);
    /// </code>
    /// </example>
    public static double[] SolveLinearSystemQR(double[,] A, double[] y)
    {
        var m = A.GetLength(0);
        var n = A.GetLength(1);
        var Q = new double[m, n];
        var R = new double[n, n];

        var simdLength = Vector<double>.Count;

        // QR Factorization
        for (var k = 0; k < n; k++)
        {
            var norm = CalculateNorm(A, k, m, simdLength);
            R[k, k] = norm;

            NormalizeColumn(A, Q, k, norm, m);

            for (var j = k + 1; j < n; j++)
            {
                var dotProduct = CalculateDotProduct(Q, A, k, j, m, simdLength);
                R[k, j] = dotProduct;
                UpdateColumn(A, Q, k, j, dotProduct, m);
            }
        }

        // Compute Q^T * y
        var QTy = ComputeQTransposeY(Q, y, m, n, simdLength);

        // Solve R * x = Q^T * y using back substitution
        return BackSubstitution(R, QTy, n);
    }

    /// <summary>
    /// Calculates the Euclidean norm of a specified column in a matrix using SIMD optimization.
    /// </summary>
    /// <param name="A">The matrix from which the column is taken.</param>
    /// <param name="column">The index of the column for which the norm is calculated.</param>
    /// <param name="rowCount">The number of rows in the matrix.</param>
    /// <param name="vectorLength">The length of the SIMD vector.</param>
    /// <returns>The Euclidean norm of the column.</returns>
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
    /// <param name="A">The input matrix A.</param>
    /// <param name="Q">The output matrix Q.</param>
    /// <param name="column">The index of the column to normalize.</param>
    /// <param name="norm">The norm of the column.</param>
    /// <param name="rowCount">The number of rows in the matrices.</param>
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
    /// <param name="Q">The matrix Q.</param>
    /// <param name="A">The matrix A.</param>
    /// <param name="qColumn">The index of the column in matrix Q.</param>
    /// <param name="aColumn">The index of the column in matrix A.</param>
    /// <param name="rowCount">The number of rows in the matrices.</param>
    /// <param name="vectorLength">The length of the SIMD vector.</param>
    /// <returns>The dot product of the specified columns.</returns>
    private static double CalculateDotProduct(double[,] Q, double[,] A, int qColumn, int aColumn, int rowCount, 
        int vectorLength)
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
    /// <param name="A">The matrix A to be updated.</param>
    /// <param name="Q">The matrix Q.</param>
    /// <param name="qColumn">The index of the column in matrix Q.</param>
    /// <param name="aColumn">The index of the column in matrix A.</param>
    /// <param name="dotProduct">The calculated dot product.</param>
    /// <param name="rowCount">The number of rows in the matrices.</param>
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
    /// <param name="Q">The matrix Q.</param>
    /// <param name="Y">The vector Y.</param>
    /// <param name="rowCount">The number of rows in the matrix Q and the length of vector Y.</param>
    /// <param name="columnCount">The number of columns in the matrix Q.</param>
    /// <param name="vectorLength">The length of the SIMD vector.</param>
    /// <returns>The product of the transpose of matrix Q and vector Y.</returns>
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
    /// <param name="R">The upper triangular matrix R.</param>
    /// <param name="QTy">The product of Q^T and y.</param>
    /// <param name="dimension">The dimension of the system.</param>
    /// <returns>The solution vector x.</returns>
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

    /// <summary>
    /// Creates an augmented matrix by appending the identity matrix to the given matrix.
    /// </summary>
    /// <param name="matrix">The original matrix.</param>
    /// <param name="dimension">The dimension of the matrix (assumed to be square).</param>
    /// <returns>The augmented matrix.</returns>
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
    /// <param name="augmented">The augmented matrix.</param>
    /// <param name="dimension">The dimension of the matrix (assumed to be square).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the matrix is singular and cannot be inverted.
    /// </exception>
    private static void PerformGaussJordanElimination(double[,] augmented, int dimension)
    {
        for (var i = 0; i < dimension; i++)
        {
            var diagElement = augmented[i, i];
            if (diagElement == 0)
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
    /// <param name="augmented">The augmented matrix.</param>
    /// <param name="row">The row to normalize.</param>
    /// <param name="diagElement">The diagonal element of the current row.</param>
    /// <param name="dimension">The dimension of the matrix (assumed to be square).</param>
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
    /// <param name="augmented">The augmented matrix.</param>
    /// <param name="currentRow">The current row being processed.</param>
    /// <param name="dimension">The dimension of the matrix (assumed to be square).</param>
    private static void EliminateOtherRows(double[,] augmented, int currentRow, int dimension)
    {
        for (var k = 0; k < dimension; k++)
        {
            if (k == currentRow) continue;

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
    /// <param name="augmented">The augmented matrix.</param>
    /// <param name="dimension">The dimension of the matrix (assumed to be square).</param>
    /// <returns>The inverse of the original matrix.</returns>
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