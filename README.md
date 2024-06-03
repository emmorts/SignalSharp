## SignalSharp

C# library designed for efficient signal processing and time series analysis. 

## Algorithms

### Change Point Detection
- **PELT**: Pruned Exact Linear Time method detects multiple change points with high efficiency, using a pruning technique to improve computation speed without sacrificing accuracy.
- **CUSUM**: Detects shifts in the mean value of a signal by accumulating deviations from a target value over time.

### Cost Functions
- **L1 Cost Function**: Robust against outliers and non-Gaussian noise.
- **L2 Cost Function**: Ideal for normally distributed data.
- **RBF Cost Function**: Handles non-linear relationships between data points.

### Smoothing and Filtering
- **Savitzky-Golay Filter**: Smooths data to reduce noise while preserving the shape of the signal.
- **Moving Average**: Smoothing a signal using a moving average filter.
- **Kalman Filter**: Estimates the state of a linear dynamic system from a series of noisy measurements.

### Resampling
- **Downsampling**: Reducing the number of samples in a signal.
- **Segment Statistics**: Computing statistics (mean, median, max, min) for segments of a signal.

## Installation

To install SignalSharp, you can use NuGet Package Manager:

```sh
dotnet add package SignalSharp
```

## Usage

Refer to [documentation](https://emmorts.github.io/SignalSharp/) for examples and API documentation.

## Contributing

Contributions are welcome! If you have ideas, suggestions, or bug reports, feel free to open an issue or submit a pull request. 