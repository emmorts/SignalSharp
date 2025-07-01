# Nelder-Mead Optimizer

The Nelder-Mead algorithm is a popular direct search optimization method used for finding the minimum of a function in a multi-dimensional space. Unlike gradient-based methods, it does not require any derivative information, making it particularly well-suited for problems where the objective function is non-differentiable, discontinuous, or noisy.

## How Nelder-Mead Works

The algorithm operates by iteratively modifying a geometric shape called a **simplex**, which is a collection of `N+1` vertices in an `N`-dimensional parameter space (e.g., a triangle in 2D, a tetrahedron in 3D). It attempts to "crawl" towards the minimum of the objective function by replacing the worst-performing vertex with a better one in each step.

The core steps of the algorithm are:

1.  **Initialization**: An initial simplex is created, typically by taking an initial guess for the parameters and perturbing it along each dimension to create the other vertices. The objective function is evaluated at each vertex.

2.  **Ordering**: The vertices of the simplex are sorted based on their objective function values, from the best (lowest value) to the worst (highest value).

3.  **Iteration**: The algorithm repeatedly performs one of the following operations to replace the worst vertex (`x_worst`) with a new, better point. The centroid of the best `N` vertices (all except the worst) is used as a reference point.
    * **Reflection**: The worst vertex is reflected through the centroid of the remaining vertices. If this new point is better than the second-worst but not better than the best, it replaces the worst vertex, and the next iteration begins.
    * **Expansion**: If the reflected point is better than the current best, the algorithm "expands" further in that promising direction to see if an even better point can be found. The better of the expanded and reflected points replaces the worst vertex.
    * **Contraction**: If the reflected point is worse than the second-worst, the algorithm assumes it overshot the minimum and performs a "contraction" to move the point closer to the centroid.
    * **Shrink**: If the contraction step also fails to produce a better point, the entire simplex is shrunk towards the best vertex, effectively narrowing the search area.

4.  **Termination**: The process continues until a stopping criterion is met, such as the vertices of the simplex becoming sufficiently close together, the function values at the vertices becoming nearly identical, or a maximum number of iterations or function evaluations being reached.

## Configuration (`NelderMeadOptimizerOptions`)

The behavior of the Nelder-Mead optimizer is controlled via the `NelderMeadOptimizerOptions` record.

-   **`MaxIterations`**: The maximum number of iterations to perform for a single optimization run. *Default: 1000*.
-   **`MaxFunctionEvaluations`**: An optional global limit on the number of times the objective function can be called across all restarts. *Default: null*.
-   **`FunctionValueConvergenceTolerance`**: The tolerance for convergence based on the function values. If the difference between the worst and best function values in the simplex falls below this threshold, the algorithm may stop. *Default: 1e-6*.
-   **`EnableParameterConvergence`**: If `true`, the optimizer will also check for convergence based on how close the parameter vectors of the simplex vertices are to each other. *Default: true*.
-   **`ParameterConvergenceTolerance`**: The relative tolerance for parameter convergence. *Default: 1e-4*.
-   **`EnableMultiStart`**: If `true`, the optimizer will perform multiple restarts from different, randomly chosen starting points. This helps avoid getting stuck in local minima. *Default: false*.
-   **`MaxRestarts`**: The number of additional optimization runs to attempt if multi-start is enabled. *Default: 2*.
-   **`EnableAdaptiveParameters`**: If `true`, the algorithm's core coefficients (reflection, expansion, etc.) are subtly adjusted during the optimization process, which can sometimes improve performance. *Default: false*.
-   **Nelder-Mead Coefficients**: `ReflectionFactor` (default 1.0), `ExpansionFactor` (default 2.0), `ContractionFactor` (default 0.5), `ShrinkFactor` (default 0.5). These control the geometry of the simplex operations.
-   **Stagnation Control**: `StagnationThresholdCount` (default 10) and `StagnationImprovementThreshold` (default 1e-9) are used to detect if the optimization has stalled and should be stopped or restarted.
-   **Initial Simplex Generation**: `InitialSimplexRangeFactor` (default 0.05) and `InitialSimplexAbsoluteStepForZeroRange` (default 0.001) control the size and shape of the initial simplex.

## Advanced Features

### Multi-Start Optimization

A key weakness of Nelder-Mead is its susceptibility to getting trapped in local minima. By setting `EnableMultiStart = true`, the optimizer will automatically perform multiple runs. The first run starts from the `InitialGuess` provided in the `ParameterDefinition`. Subsequent runs start from random points within the parameter bounds. The best result found across all runs is returned. This significantly increases the likelihood of finding the global minimum.

### Boundary Handling

The Nelder-Mead algorithm itself does not inherently handle constraints. This implementation ensures that all trial parameter points are clamped to the `MinValue` and `MaxValue` defined for each parameter before being passed to the objective function. After the optimization, the final result includes a boundary analysis warning if the optimal parameters are found to be at or very close to their defined bounds, suggesting the true optimum may lie outside the search space.

## Usage Example

Here is an example of using `NelderMeadOptimizer` to find the minimum of the Rosenbrock function, a classic non-convex test case for optimization algorithms.

```csharp
using SignalSharp.Optimization;
using SignalSharp.Optimization.NelderMead;

// The Rosenbrock function, with a minimum of 0 at (x=1, y=1).
public static ObjectiveEvaluation<double> Rosenbrock(
    object _, // Placeholder for input data
    IReadOnlyDictionary<string, double> p)
{
    double x = p["x"];
    double y = p["y"];
    const double a = 1.0;
    const double b = 100.0;
    
    double term1 = a - x;
    double term2 = y - x * x;
    double metric = Math.Pow(term1, 2) + b * Math.Pow(term2, 2);
    
    return new ObjectiveEvaluation<double>(metric);
}

// --- Main execution ---
public static async Task RunOptimization()
{
    // 1. Configure the optimizer
    var options = new NelderMeadOptimizerOptions
    {
        FunctionValueConvergenceTolerance = 1e-8,
        ParameterConvergenceTolerance = 1e-4,
        MaxIterations = 500,
        EnableMultiStart = true, // Use multi-start to improve robustness
        MaxRestarts = 3
    };
    var optimizer = new NelderMeadOptimizer<object, double>(options);

    // 2. Define parameters with bounds and an initial guess
    var parameters = new List<ParameterDefinition>
    {
        new("x", -2.0, 2.0, InitialGuess: -1.2),
        new("y", -1.0, 3.0, InitialGuess: 1.0)
    };
    
    // 3. Run the optimization
    var result = await optimizer.OptimizeAsync(
        new object(), // No real input data needed
        parameters,
        Rosenbrock,
        CancellationToken.None);

    // 4. Print the results
    if (result.Success)
    {
        Console.WriteLine("Optimization successful!");
        Console.WriteLine($"Minimized Metric: {result.MinimizedMetric:F6}");
        Console.WriteLine("Best Parameters:");
        foreach (var (name, value) in result.BestParameters)
        {
            Console.WriteLine($"  {name}: {value:F4}");
        }
        Console.WriteLine($"Total Iterations: {result.Iterations}");
        Console.WriteLine($"Total Function Evaluations: {result.FunctionEvaluations}");
        Console.WriteLine($"Message: {result.Message}");
    }
    else
    {
        Console.WriteLine($"Optimization failed: {result.Message}");
    }
}

/*
Expected Output (will vary slightly due to random starts):

Optimization successful!
Minimized Metric: 0.000000
Best Parameters:
  x: 1.0000
  y: 1.0000
Total Iterations: ...
Total Function Evaluations: ...
Message: Converged successfully.
*/
````

## When to Use Nelder-Mead

### Strengths

  * **No Derivatives Required**: Its greatest advantage is that it doesn't need gradient information. This makes it ideal for objective functions that are noisy, stochastic, or have no simple analytical form.
  * **Good for Non-Convex Problems**: While not guaranteed to find the global minimum, it is often effective at navigating complex, non-convex landscapes, especially with multi-start.
  * **Relatively Simple Concept**: The core logic of moving a simplex is more intuitive than many gradient-based or quasi-Newton methods.

### Weaknesses

  * **Can Be Slow**: Convergence can be slow, especially for high-dimensional problems, as it relies on a series of direct function evaluations rather than following a gradient.
  * **May Converge to Non-Optimal Points**: The algorithm can get stuck in local minima or even fail to converge, especially on ill-conditioned problems. Multi-start helps, but does not eliminate this risk.
  * **Performance Degrades with Dimensionality**: The number of function evaluations per iteration grows with the number of parameters. The algorithm is generally recommended for problems with a low to moderate number of dimensions (e.g., \< 10).
  * **Can Collapse**: The simplex can sometimes become "flat" or degenerate, causing the algorithm to stall. The implementation includes checks to mitigate this, but it can still be an issue.

For problems where the objective function is smooth and derivatives are available, gradient-based methods will almost always be more efficient. For low-dimensional, non-differentiable, or noisy problems, Nelder-Mead is an excellent and robust choice.

## API References

  * @"SignalSharp.Optimization.NelderMead.NelderMeadOptimizer\`2?text=NelderMeadOptimizer"
  * @"SignalSharp.Optimization.NelderMead.NelderMeadOptimizerOptions?text=NelderMeadOptimizerOptions"
  * @"SignalSharp.Optimization.IParameterOptimizer\`2?text=IParameterOptimizer"
  * @"SignalSharp.Optimization.ParameterDefinition?text=ParameterDefinition"
  * @"SignalSharp.Optimization.OptimizationResult\`1?text=OptimizationResult"
