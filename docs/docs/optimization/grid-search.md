# Grid Search Optimizer

The Grid Search optimizer is a straightforward yet powerful method for hyperparameter tuning. It systematically works through multiple combinations of parameter values, cross-validating the model for each combination to determine which one gives the best performance. It is a brute-force, exhaustive search method that is simple to implement and understand.

## How Grid Search Works

The core idea behind Grid Search is to define a grid of parameter values and evaluate the objective function at every point on this grid. The point that yields the minimum metric value is declared the optimal set of parameters.

The process is as follows:

1.  **Define Parameter Space**: For each parameter you want to optimize, you define a range of possible values (`MinValue`, `MaxValue`).
2.  **Create the Grid**: The optimizer creates a multi-dimensional grid. Each dimension corresponds to a parameter, and the points along that dimension are the discrete values specified for that parameter (`DefaultGridSteps` or `PerParameterGridSteps`).
3.  **Exhaustive Evaluation**: The optimizer iterates through every single combination of parameters on the grid. For each combination, it calls the provided `objectiveFunction`.
4.  **Find the Best**: After evaluating all combinations, the optimizer identifies the set of parameters that resulted in the lowest metric value from the objective function.

This exhaustive approach guarantees that the best parameter combination *on the grid* will be found. However, the true optimum may lie between grid points.

## Configuration (`GridSearchOptimizerOptions`)

The behavior of the Grid Search optimizer is controlled by the `GridSearchOptimizerOptions` record.

-   **`DefaultGridSteps`**: The default number of points to create for each parameter's dimension in the grid. A higher number increases the search's resolution but also significantly increases the total number of evaluations. *Default: 10*.

-   **`PerParameterGridSteps`**: A dictionary to specify a different number of grid steps for individual parameters. This is useful when you want to search one parameter more finely than others. *Default: null*.

-   **`MaxFunctionEvaluations`**: An optional integer to limit the total number of objective function evaluations. If the total number of points on the grid exceeds this limit, the optimizer will intelligently subsample the grid to stay within budget. This is crucial for high-dimensional problems to keep computation time reasonable. *Default: null (no limit)*.

-   **`EnableParallelProcessing`**: If `true`, the optimizer will evaluate different parameter combinations in parallel, which can significantly speed up the search on multi-core processors. *Default: true*.

-   **`MaxDegreeOfParallelism`**: When parallel processing is enabled, this sets the maximum number of concurrent evaluations. If `null`, it defaults to `Environment.ProcessorCount`. *Default: null*.

-   **`EarlyStoppingThreshold`**: An optional `double`. If the objective function's metric value drops to this value or below, the search stops immediately, even if not all grid points have been evaluated. This is useful if you have a "good enough" threshold for your problem. *Default: null*.

-   **`UseLogarithmicScaleFor`**: A set of parameter names that should be sampled on a logarithmic scale instead of a linear one. This is extremely useful for parameters that span several orders of magnitude (e.g., learning rates from 0.0001 to 1.0). For this to work, the parameter's `MinValue` and `MaxValue` must both be positive. *Default: null*.

-   **`EnableAdaptiveRefinement`**: If `true`, the optimizer will perform a second, more focused grid search in a smaller region around the best point found in the initial search. This can significantly improve precision without having to use a very high number of steps in the initial search. *Default: false*.

-   **`RefinementRangeFactor`**: When adaptive refinement is enabled, this factor (between 0 and 1) determines the size of the new search space. For example, a factor of `0.2` means the new search range for each parameter will be 20% of its original range, centered on the best point found initially. *Default: 0.2*.

-   **`RefinementGridSteps`**: The number of grid steps to use for each parameter during the adaptive refinement phase. *Default: 5*.

## Advanced Features

### Adaptive Refinement

When `EnableAdaptiveRefinement` is `true`, Grid Search becomes a two-stage process:

1.  **Broad Search**: An initial grid search is performed across the entire specified parameter space.
2.  **Focused Search**: A new, smaller grid is created, centered around the best parameters found in the first stage. The size of this new grid is controlled by `RefinementRangeFactor`. A second grid search is then run on this smaller grid.

This approach allows you to efficiently "zoom in" on the most promising area of the parameter space, often yielding a more precise result than a single, high-density grid search for the same computational budget.

### Logarithmic Scaling

For parameters where the optimal value could be `0.001` or `0.1`, a standard linear grid is inefficient. A linear grid with 10 steps between 0.001 and 1.0 would have points like `0.001, 0.112, 0.223, ...`. Most of the search effort is spent on large values.

By adding the parameter's name to `UseLogarithmicScaleFor`, the grid points are spaced logarithmically (e.g., `0.001, 0.003, 0.01, 0.03, ...`). This provides much better resolution at the smaller end of the scale, which is often where the most sensitivity lies for such parameters.

### Boundary Analysis

After the search is complete, the optimizer checks if the best-found parameters lie at or very near the boundaries of the search space (e.g., at `MinValue` or `MaxValue`). If they do, a warning is included in the result message. This is a strong indication that the true optimal value might lie outside the defined search range, and you should consider expanding the bounds for that parameter.

## Usage Example

Here is an example of using `GridSearchOptimizer` to find the minimum of a simple 2D quadratic function.

```csharp
using SignalSharp.Optimization;
using SignalSharp.Optimization.GridSearch;

// Define a simple objective function to be minimized
// Minimum is at (x=2.25, y=3.75), with a value of 0.
public static ObjectiveEvaluation<double> Quadratic2D(
    object _, // Placeholder for input data
    IReadOnlyDictionary<string, double> p)
{
    double x = p["x"];
    double y = p["y"];
    const double targetX = 2.25;
    const double targetY = 3.75;
    
    double metric = Math.Pow(x - targetX, 2) + Math.Pow(y - targetY, 2);
    return new ObjectiveEvaluation<double>(metric);
}

// --- Main execution ---
public static async Task RunOptimization()
{
    // 1. Configure the optimizer
    var options = new GridSearchOptimizerOptions
    {
        DefaultGridSteps = 11, // A coarse grid for the initial search
        EnableAdaptiveRefinement = true,
        RefinementGridSteps = 7, // A finer grid for the refinement step
        EnableParallelProcessing = true
    };
    var optimizer = new GridSearchOptimizer<object, double>(options);

    // 2. Define the parameters to optimize, with their bounds
    var parameters = new List<ParameterDefinition>
    {
        new("x", 0.0, 5.0),
        new("y", 0.0, 5.0)
    };
    
    // 3. Run the optimization
    var result = await optimizer.OptimizeAsync(
        new object(), // No real input data needed for this simple function
        parameters,
        Quadratic2D,
        CancellationToken.None);

    // 4. Print the results
    if (result.Success)
    {
        Console.WriteLine($"Optimization successful!");
        Console.WriteLine($"Minimized Metric: {result.MinimizedMetric:F6}");
        Console.WriteLine("Best Parameters:");
        foreach (var (name, value) in result.BestParameters)
        {
            Console.WriteLine($"  {name}: {value:F4}");
        }
        Console.WriteLine($"Total Function Evaluations: {result.FunctionEvaluations}");
        Console.WriteLine($"Message: {result.Message}");
    }
    else
    {
        Console.WriteLine($"Optimization failed: {result.Message}");
    }
}

/*
Expected Output:

Optimization successful!
Minimized Metric: 0.000000
Best Parameters:
  x: 2.2500
  y: 3.7500
Total Function Evaluations: 170
Message: Grid search with adaptive refinement completed successfully.
*/
```

## When to Use Grid Search

### Strengths

* **Simplicity**: It's easy to understand and configure.
* **Guaranteed Coverage**: It will find the best point on the defined grid, making it deterministic (for a given grid).
* **Parallelizable**: The search is "embarrassingly parallel," making it very efficient on multi-core systems.
* **Good for Low-Dimensional Problems**: For problems with a small number of parameters (e.g., 1-4), it is often a very effective and reliable choice.

### Weaknesses

* **The Curse of Dimensionality**: The number of grid points grows exponentially with the number of parameters. For a problem with `D` parameters and `N` steps for each, the total evaluations are `N^D`. This makes it impractical for high-dimensional search spaces.
* **Inefficient**: It spends equal time evaluating all parts of the search space, even those that are clearly not promising.
* **Resolution Dependent**: The quality of the solution is limited by the resolution of the grid. The true optimum may lie between grid points. Adaptive refinement helps mitigate this but doesn't eliminate the issue entirely.

For problems with many parameters (> 4-5), other methods like **Nelder-Mead** or randomized search algorithms may be more efficient.

## API References

* @"SignalSharp.Optimization.GridSearch.GridSearchOptimizer`2?text=GridSearchOptimizer"
* @"SignalSharp.Optimization.GridSearch.GridSearchOptimizerOptions?text=GridSearchOptimizerOptions"
* @"SignalSharp.Optimization.IParameterOptimizer`2?text=IParameterOptimizer"
* @"SignalSharp.Optimization.ParameterDefinition?text=ParameterDefinition"
* @"SignalSharp.Optimization.OptimizationResult`1?text=OptimizationResult"
