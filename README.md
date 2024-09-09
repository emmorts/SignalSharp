## SignalSharp

C# library designed for efficient signal processing and time series analysis. 

## Features

### Change Point Detection

- **PELT (Pruned Exact Linear Time)**: Efficiently detects multiple change points in a signal using a pruning technique for improved speed without sacrificing accuracy.
- **CUSUM (Cumulative Sum)**: Detects shifts in the mean value of a signal by accumulating deviations from a target value over time.

### Signal Analysis

- **Cost Functions**:
    - L1: Robust against outliers and non-Gaussian noise.
    - L2: Ideal for normally distributed data.
    - RBF (Radial Basis Function): Handles non-linear relationships between data points.

### Signal Processing

- **Smoothing and Filtering**:
    - Savitzky-Golay Filter: Smooths data while preserving signal shape.
    - Moving Average: Simple smoothing using a moving window.
    - Kalman Filter: Estimates the state of a linear dynamic system from noisy measurements.

- **Resampling**:
    - Downsampling: Reduces the number of samples in a signal.
    - Segment Statistics: Computes statistics (mean, median, max, min) for signal segments.

## Installation

To install SignalSharp, you can use NuGet Package Manager:

```sh
dotnet add package SignalSharp
```

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