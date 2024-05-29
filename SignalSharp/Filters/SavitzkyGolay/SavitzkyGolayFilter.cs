using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using SignalSharp.Filters.SavitzkyGolay.Exceptions;

namespace SignalSharp.Filters.SavitzkyGolay;

/// <summary>
/// A static class providing methods to apply the Savitzky-Golay filter to a signal. 
/// This filter smooths a signal by fitting successive sub-sets of adjacent data points 
/// with a low-degree polynomial using the method of linear least squares.
/// </summary>
public static class SavitzkyGolay
{
    /// <summary>
    /// Applies the Savitzky-Golay filter to the input signal.
    /// </summary>
    /// <param name="inputSignal">The array of data points to be filtered.</param>
    /// <param name="windowLength">The length of the filter window. Must be an odd number.</param>
    /// <param name="polynomialOrder">The order of the polynomial used for filtering. Must be less than the window length.</param>
    /// <returns>The filtered signal.</returns>
    /// <exception cref="SavitzkyGolayInvalidPolynomialOrderException">Thrown when the polynomial order is greater than or equal to the window length.</exception>
    public static double[] Filter(double[] inputSignal, int windowLength, int polynomialOrder)
    {
        if (inputSignal.Length == 0) return [];
        
        // Input signal must be at least 2 * windowLength + 1 in order to apply Savitzky-Golay filter
        if (inputSignal.Length < (windowLength << 1) + 1) return inputSignal;

        if (polynomialOrder >= windowLength)
        {
            throw new SavitzkyGolayInvalidPolynomialOrderException($"{nameof(polynomialOrder)} must be less than {nameof(windowLength)}.");
        }

        var halfWindow = windowLength / 2;

        var coefficients = ComputeCoefficients(windowLength, polynomialOrder);
        var filteredSignal = ApplyConvolution(inputSignal, coefficients, halfWindow);
        
        if (inputSignal.Length >= windowLength)
        {
            PolynomialFitEdges(inputSignal, filteredSignal, windowLength, polynomialOrder);
        }

        RestoreMiddleSection(inputSignal, filteredSignal, halfWindow);

        return filteredSignal;
    }
    
    /// <summary>
    /// Computes the filter coefficients for the given window length and polynomial order.
    /// </summary>
    /// <param name="windowLength">The length of the filter window.</param>
    /// <param name="polyOrder">The order of the polynomial.</param>
    /// <returns>An array of coefficients.</returns>
    private static double[] ComputeCoefficients(int windowLength, int polyOrder)
    {
        var halfWindow = windowLength / 2;
        var x = GenerateXValues(windowLength, halfWindow);
        var A = CreateMatrixA(windowLength, polyOrder, x);
        var y = CreateVectorY(windowLength, halfWindow);

        return SolveCoefficients(A, y);
    }

    /// <summary>
    /// Generates an array of x-values for the given window length.
    /// </summary>
    /// <param name="windowLength">The length of the filter window.</param>
    /// <param name="halfWindow">Half the window length.</param>
    /// <returns>An array of x-values.</returns>
    private static double[] GenerateXValues(int windowLength, int halfWindow)
    {
        var pos = halfWindow - 0.5;
        var x = new double[windowLength];
        
        for (var i = 0; i < windowLength; i++)
        {
            x[i] = i - pos;
        }
        
        Array.Reverse(x);
        
        return x;
    }

    /// <summary>
    /// Creates a matrix A used for solving the polynomial coefficients.
    /// </summary>
    /// <param name="windowLength">The length of the filter window.</param>
    /// <param name="polyOrder">The order of the polynomial.</param>
    /// <param name="x">The array of x-values.</param>
    /// <returns>A matrix representing the polynomial equations.</returns>
    private static Matrix<double> CreateMatrixA(int windowLength, int polyOrder, double[] x)
    {
        var A = Matrix<double>.Build.Dense(windowLength, polyOrder + 1);
        for (var i = 0; i < windowLength; i++)
        {
            for (var j = 0; j <= polyOrder; j++)
            {
                A[i, j] = Math.Pow(x[i], j);
            }
        }
        return A;
    }

    /// <summary>
    /// Creates a vector y used for solving the polynomial coefficients.
    /// </summary>
    /// <param name="windowLength">The length of the filter window.</param>
    /// <param name="halfWindow">Half the window length.</param>
    /// <returns>A vector representing the desired output values.</returns>
    private static Vector<double> CreateVectorY(int windowLength, int halfWindow)
    {
        var y = Vector<double>.Build.Dense(windowLength);
        y[halfWindow] = 1;
        return y;
    }

    /// <summary>
    /// Solves for the polynomial coefficients using matrix algebra.
    /// </summary>
    /// <param name="A">The matrix representing the polynomial equations.</param>
    /// <param name="y">The vector representing the desired output values.</param>
    /// <returns>An array of polynomial coefficients.</returns>
    private static double[] SolveCoefficients(Matrix<double> A, Vector<double> y)
    {
        var coefficients = A.Solve(y);
        
        return coefficients.Reverse().ToArray();
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
                if (idx < 0) idx = 0;
                if (idx >= inputSignal.Length) idx = inputSignal.Length - 1;
                
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
    private static void PolynomialFitEdges(double[] inputSignal, double[] outputSignal, int windowLength, int polyOrder)
    {
        var halfWindow = windowLength / 2;
        
        PolynomialFit(inputSignal, outputSignal, 0, windowLength, 0, halfWindow, polyOrder);
        PolynomialFit(inputSignal, outputSignal, inputSignal.Length - windowLength, inputSignal.Length, inputSignal.Length - halfWindow, inputSignal.Length, polyOrder);
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
    private static void PolynomialFit(double[] inputSignal, double[] outputSignal, int windowStart, 
        int windowStop, int interpStart, int interpStop, int polyOrder)
    {
        var xVals = new double[windowStop - windowStart];
        var yVals = new double[windowStop - windowStart];
        
        for (var i = windowStart; i < windowStop; i++)
        {
            xVals[i - windowStart] = i - windowStart;
            yVals[i - windowStart] = inputSignal[i];
        }

        var polyCoefficients = Fit.Polynomial(xVals, yVals, polyOrder);

        for (var i = interpStart; i < interpStop; i++)
        {
            outputSignal[i] = EvaluatePolynomial(polyCoefficients, i - windowStart);
        }
    }

    /// <summary>
    /// Evaluates a polynomial at a given point.
    /// </summary>
    /// <param name="polyCoefficients">The array of polynomial coefficients.</param>
    /// <param name="t">The point at which to evaluate the polynomial.</param>
    /// <returns>The value of the polynomial at the given point.</returns>
    private static double EvaluatePolynomial(double[] polyCoefficients, double t)
    {
        return polyCoefficients
            .Select((t1, j) =>
            {
                return t1 * Math.Pow(t, j);
            })
            .Sum();
    }

    /// <summary>
    /// Restores the middle section of the input signal to the output signal.
    /// </summary>
    /// <param name="inputSignal">The array of data points to be filtered.</param>
    /// <param name="outputSignal">The array of filtered data points.</param>
    /// <param name="halfWindow">Half the window length.</param>
    private static void RestoreMiddleSection(double[] inputSignal, double[] outputSignal, int halfWindow)
    {
        for (var i = halfWindow; i < inputSignal.Length - halfWindow; i++)
        {
            outputSignal[i] = inputSignal[i];
        }
    }
}