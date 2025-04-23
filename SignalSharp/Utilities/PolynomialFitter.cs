namespace SignalSharp.Utilities;

/// <summary>
/// Provides methods for polynomial fitting and evaluation.
/// </summary>
public static class PolynomialFitter
{
    /// <summary>
    /// Fits a polynomial of specified order to the given data points using the least squares method.
    /// </summary>
    /// <param name="x">The x-coordinates of the data points.</param>
    /// <param name="y">The y-coordinates of the data points.</param>
    /// <param name="order">The order of the polynomial to fit.</param>
    /// <returns>An array of coefficients for the fitted polynomial, from lowest to highest order.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// - The input arrays have different lengths.
    /// - The polynomial order is negative.
    /// - The number of data points is less than or equal to the polynomial order.
    /// </exception>
    public static double[] FitPolynomial(double[] x, double[] y, int order)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException("Input arrays must have the same length.");
        }

        if (order < 0)
        {
            throw new ArgumentException("Polynomial order must be non-negative.");
        }

        if (x.Length <= order)
        {
            throw new ArgumentException("Number of data points must be greater than the polynomial order.");
        }

        var n = x.Length;
        var terms = order + 1;

        // Create the design matrix
        var design = new double[n, terms];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < terms; j++)
            {
                design[i, j] = Math.Pow(x[i], j);
            }
        }

        // Calculate (X^T * X)
        var xtx = new double[terms, terms];
        for (var i = 0; i < terms; i++)
        {
            for (var j = 0; j < terms; j++)
            {
                double sum = 0;
                for (var k = 0; k < n; k++)
                {
                    sum += design[k, i] * design[k, j];
                }
                xtx[i, j] = sum;
            }
        }

        // Calculate (X^T * y)
        var xty = new double[terms];
        for (var i = 0; i < terms; i++)
        {
            double sum = 0;
            for (var j = 0; j < n; j++)
            {
                sum += design[j, i] * y[j];
            }
            xty[i] = sum;
        }

        // Solve the system (X^T * X) * coefficients = (X^T * y)
        return SolveLinearSystem(xtx, xty);
    }

    /// <summary>
    /// Evaluates a polynomial at a given x-value using the provided coefficients.
    /// </summary>
    /// <param name="coefficients">The coefficients of the polynomial, from lowest to highest order.</param>
    /// <param name="x">The x-value at which to evaluate the polynomial.</param>
    /// <returns>The y-value of the polynomial at the given x-value.</returns>
    public static double EvaluatePolynomial(double[] coefficients, double x)
    {
        double result = 0;

        for (var i = 0; i < coefficients.Length; i++)
        {
            result += coefficients[i] * Math.Pow(x, i);
        }

        return result;
    }

    private static double[] SolveLinearSystem(double[,] A, double[] b)
    {
        var n = b.Length;
        var x = new double[n];

        // Gaussian elimination with partial pivoting
        for (var i = 0; i < n; i++)
        {
            // Find pivot
            var maxRow = i;
            for (var k = i + 1; k < n; k++)
            {
                if (Math.Abs(A[k, i]) > Math.Abs(A[maxRow, i]))
                {
                    maxRow = k;
                }
            }

            // Swap maximum row with current row
            for (var k = i; k < n; k++)
            {
                (A[maxRow, k], A[i, k]) = (A[i, k], A[maxRow, k]);
            }

            (b[maxRow], b[i]) = (b[i], b[maxRow]);

            // Make all rows below this one 0 in current column
            for (var k = i + 1; k < n; k++)
            {
                var factor = A[k, i] / A[i, i];
                b[k] -= factor * b[i];
                for (var j = i; j < n; j++)
                {
                    A[k, j] -= factor * A[i, j];
                }
            }
        }

        // Back substitution
        for (var i = n - 1; i >= 0; i--)
        {
            var sum = 0.0;
            for (var j = i + 1; j < n; j++)
            {
                sum += A[i, j] * x[j];
            }
            x[i] = (b[i] - sum) / A[i, i];
        }

        return x;
    }
}
