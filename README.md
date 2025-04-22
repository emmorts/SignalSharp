## SignalSharp

SignalSharp is a .NET library focused on time series analysis, change point detection, and signal smoothing/filtering tasks. It provides implementations of common algorithms optimized for clarity and efficiency.

## Core Features

### Change Point Detection

Algorithms to identify points in time where the statistical properties of a signal change.

*   **PELT (Pruned Exact Linear Time)**:
  *   Detects multiple change points efficiently.
  *   Supports exact (`Jump = 1`) and approximate (`Jump > 1`) modes for speed/accuracy trade-offs.
  *   Configurable via cost functions to target specific types of changes:
    *   `L1CostFunction`: Robust to outliers, sensitive to median shifts.
    *   `L2CostFunction`: Sensitive to mean shifts, assumes constant variance (computationally efficient).
    *   `GaussianLikelihoodCostFunction`: Sensitive to changes in both mean and variance, assumes normality (computationally efficient).
    *   `RBFCostFunction`: Kernel-based, can detect changes in the underlying distribution shape.
    *   `ARCostFunction`: Fits an Autoregressive model, sensitive to changes in signal dynamics/autocorrelation (univariate only).

*   **CUSUM (Cumulative Sum)**:
  *   Detects shifts in the mean of a signal.
  *   Works by accumulating deviations from an expected level and triggering when a threshold is exceeded. Useful for process monitoring.

### Signal Smoothing

Methods to reduce noise from measurements.

*   **Savitzky-Golay Filter**: Smooths data by fitting successive sub-sets of adjacent data points with a low-degree polynomial using linear least squares. Helps preserve signal features better than a simple moving average.
*   **Moving Average**: Basic smoothing technique that calculates the average of data points within a sliding window.

### Utilities

*   **Signal Padding**: Methods for common padding modes (`Constant`, `Mirror`, `Nearest`, `Periodic`).
*   **Statistical Functions**: Basic statistics (`Mean`, `Variance`, `StdDev`, `Median`, `Normalization`, etc.).

## Installation

Install the package via the `dotnet` CLI:

```sh
dotnet add package SignalSharp
```

Or use the NuGet Package Manager in your IDE.

## Usage

For detailed examples and API documentation, please refer to the [official documentation](https://emmorts.github.io/SignalSharp/).

Here's a quick example of how to use the PELT algorithm for change point detection:

```csharp
using SignalSharp;

// Create a sample signal
double[] signal = [1, 1, 1, 5, 5, 5, 1, 1, 1];

// Initialize PELT algorithm
var options = new PELTOptions { CostFunction = new L2CostFunction(), MinSize = 1, Jump = 1 };
var algo = new PELTAlgorithm(options);

// Detect change points
var breakpoints = algo.FitAndDetect(signal, 2); // breakpoints = [3, 6]
```

## Contributing

Contributions are welcome! If you have ideas, suggestions, or bug reports, feel free to open an issue or submit a pull request. 