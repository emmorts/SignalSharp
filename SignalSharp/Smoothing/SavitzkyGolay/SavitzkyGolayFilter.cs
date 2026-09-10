using System.Numerics;
using SignalSharp.Common;
using SignalSharp.Common.Models;
using SignalSharp.Utilities;

// ReSharper disable InconsistentNaming

namespace SignalSharp.Smoothing.SavitzkyGolay;

/// <summary>
/// Provides methods to apply the Savitzky-Golay filter to a signal for smoothing.
/// </summary>
/// <remarks>
/// <para>
/// The Savitzky-Golay filter smooths a signal by fitting successive sub-sets of adjacent data points
/// with a low-degree polynomial using the method of linear least squares. It is widely used in data
/// preprocessing for its ability to preserve features of the dataset such as relative maxima, minima,
/// and width, which are usually flattened by other smoothing techniques.
/// </para>
/// <para>
/// This implementation requires the specification of a window length and a polynomial order.
/// The window length must be an odd number, and the polynomial order must be less than the window length.
/// </para>
/// </remarks>
public static class SavitzkyGolayFilter
{
    /// <summary>
    /// Applies the Savitzky-Golay filter to the input signal.
    /// </summary>
    /// <param name="inputSignal">The array of data points to be filtered.</param>
    /// <param name="windowLength">The length of the filter window. Must be an odd number.</param>
    /// <param name="polynomialOrder">The order of the polynomial used for filtering. Must be less than the window length.</param>
    /// <param name="derivativeOrder">The order of the derivative to compute. Default is 0 (no derivative).</param>
    /// <param name="padding">The padding method to apply to the signal. Default is None.</param>
    /// <param name="paddedValue">The value to use for padding. Default is 0.</param>
    /// <returns>The filtered signal. If the input signal length is less than <c>2 * windowLength + 1</c>, a copy of the input is returned unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inputSignal"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="windowLength"/> is not positive or is even, when the polynomial order is negative or
    /// greater than or equal to the window length, or when the derivative order is negative or greater than the polynomial order.
    /// </exception>
    /// <example>
    /// For example, to apply the Savitzky-Golay filter to an array of measurements:
    /// <code>
    /// double[] inputSignal = {1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0};
    /// double[] filteredSignal = SavitzkyGolayFilter.Apply(inputSignal, 5, 2);
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// The interior of the signal is produced by the Savitzky-Golay convolution.
    /// With <see cref="Padding.None"/> the first and last <c>windowLength / 2</c> samples are produced by
    /// fitting a polynomial to the corresponding edge window; with any other padding mode the padded
    /// convolution is used for the boundary samples instead.
    /// </para>
    /// <para>
    /// If the input signal length is insufficient to apply the filter (i.e., less than 2 * window length + 1),
    /// a copy of the original signal is returned.
    /// </para>
    /// <para>
    /// The generic overload converts the input to <see cref="double"/>, filters it, and converts the result back to
    /// <typeparamref name="T"/>. For integral <typeparamref name="T"/> the filtered values are therefore rounded
    /// (banker's rounding), and values outside the range of <typeparamref name="T"/> cause an
    /// <see cref="OverflowException"/>.
    /// </para>
    /// </remarks>
    public static T[] Apply<T>(
        T[] inputSignal,
        int windowLength,
        int polynomialOrder,
        int derivativeOrder = 0,
        Padding padding = Padding.None,
        double paddedValue = 0
    )
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(inputSignal);

        var doubleInputSignal = inputSignal.Select(x => Convert.ToDouble(x)).ToArray();

        return Apply(doubleInputSignal, windowLength, polynomialOrder, derivativeOrder, padding, paddedValue)
            .Select(x => (T)Convert.ChangeType(x, typeof(T)))
            .ToArray();
    }

    /// <summary>
    /// Applies the Savitzky-Golay filter to the input signal.
    /// </summary>
    /// <param name="inputSignal">The array of data points to be filtered.</param>
    /// <param name="windowLength">The length of the filter window. Must be an odd number.</param>
    /// <param name="polynomialOrder">The order of the polynomial used for filtering. Must be less than the window length.</param>
    /// <param name="derivativeOrder">The order of the derivative to compute. Default is 0 (no derivative).</param>
    /// <param name="padding">The padding method to apply to the signal. Default is None.</param>
    /// <param name="paddedValue">The value to use for padding. Default is 0.</param>
    /// <returns>The filtered signal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inputSignal"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="windowLength"/> is not positive or is even, when the polynomial order is negative or
    /// greater than or equal to the window length, or when the derivative order is negative or greater than the polynomial order.
    /// </exception>
    /// <example>
    /// For example, to apply the Savitzky-Golay filter to an array of measurements:
    /// <code>
    /// double[] inputSignal = {1.0, 2.0, 3.0, 4.0, 5.0};
    /// double[] filteredSignal = SavitzkyGolay.Apply(inputSignal, 3, 1);
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// The interior of the signal is produced by the Savitzky-Golay convolution.
    /// With <see cref="Padding.None"/> the first and last <c>windowLength / 2</c> samples are produced by
    /// fitting a polynomial to the corresponding edge window; with any other padding mode the padded
    /// convolution is used for the boundary samples instead.
    /// </para>
    /// <para>
    /// If the input signal length is insufficient to apply the filter (i.e., less than 2 * window length + 1),
    /// a copy of the original signal is returned.
    /// </para>
    /// </remarks>
    public static double[] Apply(
        double[] inputSignal,
        int windowLength,
        int polynomialOrder,
        int derivativeOrder = 0,
        Padding padding = Padding.None,
        double paddedValue = 0
    )
    {
        ArgumentNullException.ThrowIfNull(inputSignal);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowLength, nameof(windowLength));
        ArgumentOutOfRangeException.ThrowIfNegative(polynomialOrder, nameof(polynomialOrder));
        ArgumentOutOfRangeException.ThrowIfNegative(derivativeOrder, nameof(derivativeOrder));

        if (windowLength % 2 == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowLength), windowLength, "Window length must be an odd number.");
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(polynomialOrder, windowLength, nameof(polynomialOrder));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(derivativeOrder, polynomialOrder, nameof(derivativeOrder));

        if (inputSignal.Length == 0)
            return [];

        // Input signal must be at least 2 * windowLength + 1 in order to apply Savitzky-Golay filter
        if (inputSignal.Length < (windowLength << 1) + 1)
            return inputSignal.ToArray();

        var halfWindow = windowLength / 2;

        var extendedSignal = padding is not Padding.None ? SignalPadding.ApplyPadding(inputSignal, windowLength, padding, paddedValue) : inputSignal;
        var coefficients = ComputeCoefficients(windowLength, polynomialOrder, derivativeOrder);
        var filteredSignal = ApplyConvolution(extendedSignal, coefficients, halfWindow);

        if (padding != Padding.None)
        {
            filteredSignal = SignalPadding.TrimPadding(filteredSignal, inputSignal.Length, windowLength);
        }

        // Without padding, boundary samples have no full window; fit a polynomial over the edge windows instead.
        // With padding, the padded convolution already provides values for the boundary samples.
        if (padding == Padding.None && inputSignal.Length >= windowLength)
        {
            PolynomialFitEdges(inputSignal, filteredSignal, windowLength, polynomialOrder, derivativeOrder);
        }

        return filteredSignal;
    }

    /// <summary>
    /// Computes the convolution (Savitzky-Golay) weights for the given window length, polynomial order and derivative order.
    /// </summary>
    /// <param name="windowLength">The length of the filter window (odd).</param>
    /// <param name="polyOrder">The order of the polynomial.</param>
    /// <param name="derivativeOrder">The order of the derivative.</param>
    /// <returns>An array of convolution weights, in sample order across the window.</returns>
    /// <remarks>
    /// The weights are computed as <c>w = A * (A^T A)^-1 * e_d * d!</c> where <c>A[i, j] = (i - halfWindow)^j</c>
    /// is the centered Vandermonde matrix and <c>e_d</c> is the unit vector of index <c>d = derivativeOrder</c>.
    /// For <c>d = 0</c> this is the first row of the pseudoinverse of <c>A</c>, scaled by <c>d!</c> for higher
    /// derivative orders. Convolving the weights with a window yields the least-squares polynomial value
    /// (or its <c>d</c>-th derivative) at the window centre.
    /// </remarks>
    private static double[] ComputeCoefficients(int windowLength, int polyOrder, int derivativeOrder)
    {
        var halfWindow = windowLength / 2;

        // Design matrix over the centered window: A[i, j] = (i - halfWindow)^j
        var design = new double[windowLength, polyOrder + 1];
        for (var i = 0; i < windowLength; i++)
        {
            var offset = i - halfWindow;
            for (var j = 0; j <= polyOrder; j++)
            {
                design[i, j] = Math.Pow(offset, j);
            }
        }

        // Normal equations matrix A^T A
        var normalMatrix = new double[polyOrder + 1, polyOrder + 1];
        for (var i = 0; i <= polyOrder; i++)
        {
            for (var j = 0; j <= polyOrder; j++)
            {
                double sum = 0;
                for (var k = 0; k < windowLength; k++)
                {
                    sum += design[k, i] * design[k, j];
                }
                normalMatrix[i, j] = sum;
            }
        }

        // Target: the derivativeOrder-th polynomial coefficient weighted by derivativeOrder!
        var target = new double[polyOrder + 1];
        target[derivativeOrder] = MathFunctions.Factorial(derivativeOrder);

        var projectedTarget = MatrixOperations.SolveLinearSystem(normalMatrix, target);

        return MatrixOperations.Multiply(design, projectedTarget);
    }

    /// <summary>
    /// Applies the convolution of the input signal with the computed coefficients.
    /// </summary>
    /// <param name="inputSignal">The array of data points to be filtered.</param>
    /// <param name="coefficients">The array of polynomial coefficients.</param>
    /// <param name="halfWindow">Half the window length.</param>
    /// <returns>The filtered signal.</returns>
    private static double[] ApplyConvolution(double[] inputSignal, double[] coefficients, int halfWindow)
    {
        var result = new double[inputSignal.Length];

        for (var i = 0; i < inputSignal.Length; i++)
        {
            double sum = 0;

            for (var j = 0; j < coefficients.Length; j++)
            {
                var idx = i + j - halfWindow;
                if (idx < 0)
                    idx = 0;
                if (idx >= inputSignal.Length)
                    idx = inputSignal.Length - 1;

                sum += inputSignal[idx] * coefficients[j];
            }

            result[i] = sum;
        }

        return result;
    }

    /// <summary>
    /// Fits a polynomial to the edges of the signal to handle boundary effects.
    /// </summary>
    /// <param name="inputSignal">The array of data points to be filtered.</param>
    /// <param name="outputSignal">The array of filtered data points.</param>
    /// <param name="windowLength">The length of the filter window.</param>
    /// <param name="polyOrder">The order of the polynomial.</param>
    private static void PolynomialFitEdges(double[] inputSignal, double[] outputSignal, int windowLength, int polyOrder, int derivativeOrder)
    {
        var halfWindow = windowLength / 2;

        PolynomialFit(inputSignal, outputSignal, 0, windowLength, 0, halfWindow, polyOrder, derivativeOrder);
        PolynomialFit(
            inputSignal,
            outputSignal,
            inputSignal.Length - windowLength,
            inputSignal.Length,
            inputSignal.Length - halfWindow,
            inputSignal.Length,
            polyOrder,
            derivativeOrder
        );
    }

    /// <summary>
    /// Fits a polynomial to a section of the signal.
    /// </summary>
    /// <param name="inputSignal">The array of data points to be filtered.</param>
    /// <param name="outputSignal">The array of filtered data points.</param>
    /// <param name="windowStart">The starting index of the window.</param>
    /// <param name="windowStop">The ending index of the window.</param>
    /// <param name="interpStart">The starting index of the interpolation.</param>
    /// <param name="interpStop">The ending index of the interpolation.</param>
    /// <param name="polyOrder">The order of the polynomial.</param>
    private static void PolynomialFit(
        double[] inputSignal,
        double[] outputSignal,
        int windowStart,
        int windowStop,
        int interpStart,
        int interpStop,
        int polyOrder,
        int derivativeOrder
    )
    {
        var windowLength = windowStop - windowStart;
        var center = (windowLength - 1) / 2.0;
        var halfRange = Math.Max(center, 1.0);

        var xVals = new double[windowLength];
        var yVals = new double[windowLength];

        for (var i = windowStart; i < windowStop; i++)
        {
            // Fit on a centered, unit-scaled basis (x in [-1, 1]). A raw monomial basis makes the normal
            // equations ill-conditioned for higher polynomial orders, to the point of being reported as singular.
            xVals[i - windowStart] = (i - windowStart - center) / halfRange;
            yVals[i - windowStart] = inputSignal[i];
        }

        var polyCoefficients = PolynomialFitter.FitPolynomial(xVals, yVals, polyOrder);

        // The fit is in the scaled variable, so the d-th derivative scales by halfRange^-d when mapped back.
        var derivativeScale = Math.Pow(halfRange, derivativeOrder);

        for (var i = interpStart; i < interpStop; i++)
        {
            var scaledX = (i - windowStart - center) / halfRange;
            outputSignal[i] = EvaluatePolynomialDerivative(polyCoefficients, scaledX, derivativeOrder) / derivativeScale;
        }
    }

    /// <summary>
    /// Evaluates the given derivative of a polynomial at a point.
    /// </summary>
    /// <param name="coefficients">The polynomial coefficients, from lowest to highest order.</param>
    /// <param name="x">The point at which to evaluate.</param>
    /// <param name="derivativeOrder">The derivative order (0 evaluates the polynomial itself).</param>
    private static double EvaluatePolynomialDerivative(double[] coefficients, double x, int derivativeOrder)
    {
        if (derivativeOrder == 0)
        {
            return PolynomialFitter.EvaluatePolynomial(coefficients, x);
        }

        double result = 0;

        for (var j = derivativeOrder; j < coefficients.Length; j++)
        {
            double factor = 1;
            for (var k = j - derivativeOrder + 1; k <= j; k++)
            {
                factor *= k;
            }

            result += coefficients[j] * factor * Math.Pow(x, j - derivativeOrder);
        }

        return result;
    }
}
