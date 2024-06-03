using System.Numerics;

namespace SignalSharp.Utilities;

/// <summary>
/// Provides a set of mathematical functions for numerical data processing.
/// </summary>
public static class MathFunctions
{
    /// <summary>
    /// Calculates the factorial of a non-negative integer.
    /// </summary>
    /// <param name="n">The non-negative integer.</param>
    /// <returns>The factorial of the integer.</returns>
    public static BigInteger Factorial(BigInteger n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n, nameof(n));
        
        return FactorialRecursive(n);
    }
    
    /// <summary>
    /// Calculates the factorial of a non-negative integer.
    /// </summary>
    /// <param name="n">The non-negative integer.</param>
    /// <returns>The factorial of the integer.</returns>
    public static int Factorial(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n, nameof(n));
        
        return FactorialRecursive(n);
    }

    private static BigInteger FactorialRecursive(BigInteger n)
    {
        if (n == BigInteger.Zero || n == BigInteger.One) return BigInteger.One;
        
        var mid = n / 2;
        
        return FactorialRecursive(mid) * FactorialRecursive(n - mid) * Combine(mid, n - mid);
    }

    private static int FactorialRecursive(int n)
    {
        if (n is 0 or 1) return 1;
        
        var mid = n / 2;
        
        return FactorialRecursive(mid) * FactorialRecursive(n - mid) * Combine(mid, n - mid);
    }

    private static BigInteger Combine(BigInteger a, BigInteger b)
    {
        BigInteger result = 1;
        
        for (var i = a + 1; i <= a + b; i++)
        {
            result *= i;
        }
        
        return result;
    }

    private static int Combine(int a, int b)
    {
        var result = 1;
        
        for (var i = a + 1; i <= a + b; i++)
        {
            result *= i;
        }
        
        return result;
    }
}