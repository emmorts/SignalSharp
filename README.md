![NuGet Version](https://img.shields.io/nuget/v/SignalSharp)

## SignalSharp

SignalSharp is a .NET library focused on time series analysis, change point detection, and signal smoothing/filtering tasks. It provides implementations of common algorithms optimized for clarity and efficiency.

### Core Features

#### Change Point Detection

Algorithms to identify points in time where the statistical properties of a signal change.

  * **PELT (Pruned Exact Linear Time)**:

      * Detects multiple change points efficiently.
      * Supports various cost functions to target specific types of changes, including mean, variance, event rate, and distribution shape.
      * Includes a `PELTPenaltySelector` for automatic penalty selection using BIC, AIC, or AICc for supported cost functions.

  * **CUSUM (Cumulative Sum)**:

      * Detects shifts in the mean of a signal.
      * Works by accumulating deviations from an expected level and triggering when a threshold is exceeded.

#### Signal Smoothing

Methods to reduce noise from measurements.

  * **Savitzky-Golay Filter**: Smooths data by fitting successive sub-sets of adjacent data points with a low-degree polynomial. This helps preserve signal features better than a simple moving average. 
  * **Moving Average**: A basic smoothing technique that calculates the average of data points within a sliding window.

#### Extrapolation

Methods for forecasting future values based on historical data.

  * **Simple Exponential Smoothing (SES)**: A forecasting method for univariate time series without a clear trend or seasonal pattern. It computes future values based on a weighted average of past observations, with recent observations given more weight. The forecast is a constant value, which is the last smoothed level calculated from the data.
  * **Holt's Linear Trend Method**: Also known as Double Exponential Smoothing, this method is for data with a trend but no seasonality. It supports both additive and multiplicative trends and can include a "damped" trend to improve long-term forecast accuracy. The implementation can also automatically find optimal smoothing parameters via a built-in grid search.
  * **Linear Extrapolation**: Forecasts future values by fitting a simple linear trend to a recent window of data points and extending that line into the future. The size of the fitting window is configurable.

#### Optimization

Hyperparameter tuning methods to find the best-performing parameters for a model.

  * **Grid Search**: An exhaustive search method that systematically evaluates a grid of parameter combinations to find the one that yields the best performance. It supports parallel processing and an "adaptive refinement" feature that performs a second, more focused search around the best initial result.
  * **Nelder-Mead**: A direct search method that "crawls" toward the minimum of a function in a multi-dimensional space without requiring derivative information. This makes it well-suited for objective functions that are noisy or non-differentiable. It can be configured to perform multiple restarts from different starting points to increase the likelihood of finding a global minimum.

#### Utilities

  * **Signal Padding**: Methods for common padding modes (`Constant`, `Mirror`, `Nearest`, `Periodic`).
  * **Statistical Functions**: Basic statistics (`Mean`, `Variance`, `StdDev`, `Median`, `Normalization`, etc.).

### Installation

Install the package via the `dotnet` CLI:

```sh
dotnet add package SignalSharp
```

Or use the NuGet Package Manager in your IDE.

### Usage

For detailed examples and API documentation, please refer to the [official documentation](https://emmorts.github.io/SignalSharp/).

Here's a quick example of how to use the PELT algorithm for change point detection:

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;

// Create a sample signal
double[] signal = { 1, 1, 1, 5, 5, 5, 1, 1, 1 };

// Initialize PELT algorithm (using default L2 cost, MinSize=1, Jump=1)
var options = new PELTOptions(); 
var algo = new PELTAlgorithm(options);

// Detect change points with a manually chosen penalty
int[] breakpoints = algo.FitAndDetect(signal, penalty: 2.0); 
Console.WriteLine($"Manual Penalty Change Points: {string.Join(", ", breakpoints)}"); // Expected: [3, 6]

// --- Or use Automatic Penalty Selection (requires a likelihood cost function) ---

// Configure PELT with Gaussian Likelihood (supports BIC/AIC)
var likelihoodOptions = new PELTOptions { CostFunction = new GaussianLikelihoodCostFunction(), MinSize = 2 };
var likelihoodAlgo = new PELTAlgorithm(likelihoodOptions);

// Create the selector
var selector = new PELTPenaltySelector(likelihoodAlgo);

// Choose selection method (e.g., BIC)
var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC);

// Fit data and select penalty
var result = selector.FitAndSelect(signal, selectionOptions);

Console.WriteLine($"Auto Penalty (BIC): {result.SelectedPenalty:F4}");
Console.WriteLine($"Auto Penalty Change Points: {string.Join(", ", result.OptimalBreakpoints)}"); 
// Expected: likely [3, 6], penalty will vary
```

### Contributing

Contributions are welcome! If you have ideas, suggestions, or bug reports, feel free to open an issue or submit a pull request.