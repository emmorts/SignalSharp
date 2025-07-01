using SignalSharp.Optimization;

namespace SignalSharp.Tests.Optimization;

public static class ObjectiveFunctions
{
    public static ObjectiveEvaluation<double> Quadratic1D(GridSearchOptimizerTests.TestInput _, IReadOnlyDictionary<string, double> p, double targetX = 2.0)
    {
        double x = p["x"];
        double metric = (x - targetX) * (x - targetX);
        return new ObjectiveEvaluation<double>(metric);
    }

    public static ObjectiveEvaluation<double> Quadratic1D_WithNaN(
        GridSearchOptimizerTests.TestInput _,
        IReadOnlyDictionary<string, double> p,
        double targetX = 2.0
    )
    {
        double x = p["x"];
        if (x < 0)
            return new ObjectiveEvaluation<double>(double.NaN);
        double metric = (x - targetX) * (x - targetX);
        return new ObjectiveEvaluation<double>(metric);
    }

    public static ObjectiveEvaluation<double> Quadratic2D(
        GridSearchOptimizerTests.TestInput _,
        IReadOnlyDictionary<string, double> p,
        double targetX = 2.0,
        double targetY = 3.0
    )
    {
        double x = p["x"];
        double y = p["y"];
        double metric = (x - targetX) * (x - targetX) + (y - targetY) * (y - targetY);
        return new ObjectiveEvaluation<double>(metric);
    }

    /// <summary>
    /// Rosenbrock function. Minimum f(x,y)=0 at (1,1).
    /// f(x,y) = (a-x)^2 + b(y-x^2)^2
    /// Typically a=1, b=100.
    /// </summary>
    public static ObjectiveEvaluation<double> Rosenbrock(GridSearchOptimizerTests.TestInput _, IReadOnlyDictionary<string, double> p)
    {
        double x = p["x"];
        double y = p["y"];
        const double a = 1.0;
        const double b = 100.0;

        double term1 = a - x;
        double term2 = y - x * x;
        double metric = term1 * term1 + b * term2 * term2;

        return new ObjectiveEvaluation<double>(metric);
    }

    public static ObjectiveEvaluation<double> ThrowingFunction(GridSearchOptimizerTests.TestInput _, IReadOnlyDictionary<string, double> p)
    {
        throw new InvalidOperationException("Test exception from objective function.");
    }
}
