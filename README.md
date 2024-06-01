## SignalSharp

C# library designed for efficient signal processing and time series analysis. 

## Algorithms

### Change Point Detection
- **PELT Algorithm**: Efficiently detects multiple change points in time series data.
  - **L1 Cost Function**: Robust against outliers and non-Gaussian noise.
  - **L2 Cost Function**: Ideal for normally distributed data.
  - **RBF Cost Function**: Handles non-linear relationships between data points.

### Smoothing and Filtering
- **Savitzky-Golay Filter**: Smooths data to reduce noise while preserving the shape of the signal.
- **Kalman Filter**: Estimates the state of a linear dynamic system from a series of noisy measurements.

### Resampling
- **Downsampling**: Reducing the number of samples in a signal.
- **Segment Statistics**: Computing statistics (mean, median, max, min) for segments of a signal.
- **Moving Average**: Smoothing a signal using a moving average filter.
- **Chebyshev Approximation**: Approximating a signal using Chebyshev polynomials.

## Future Plans

- [ ] Implement most important signal processing algorithms.
  - [x] PELT
    - [x] L1 Cost Function
    - [x] L2 Cost Function
    - [x] RBF Cost Function
  - [x] CUSUM
  - [x] Savitzky-Golay Filter
  - [x] Resampling
    - [x] Downsampling
    - [x] Segment Statistics
    - [x] Moving Average
    - [x] Chebyshev Approximation
    - [ ] Upsampling
  - [ ] FFT
  - [ ] Wavelet Transform
  - [x] Kalman Filter
  - [ ] Autoregressive (AR) models
- [ ] Enhance the performance of existing algorithms.
- [ ] Provide more comprehensive examples and documentation.

## Installation

To install SignalSharp, you can use NuGet Package Manager:

```sh
dotnet add package SignalSharp
```

## Usage

Refer to [documentation](https://emmorts.github.io/SignalSharp/) for examples and API documentation.

## Contributing

Contributions are welcome! If you have ideas, suggestions, or bug reports, feel free to open an issue or submit a pull request. 