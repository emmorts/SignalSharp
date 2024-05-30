SignalSharp is a C# library designed for efficient signal processing and time series analysis. Whether you're dealing with noise reduction, state estimation, or change point detection, SignalSharp provides the tools to help you get the job done.

## Features

- **PELT Algorithm**: Efficiently detects multiple change points in time series data.
  - **L1 Cost Function**: Robust to outliers and non-Gaussian noise.
  - **L2 Cost Function**: Suitable for normally distributed data.
  - **RBF Cost Function**: Handles non-linear relationships between data points.
- **Savitzky-Golay Filter**: Smooths data to reduce noise while preserving the shape of the signal.
- **Kalman Filter**: Estimates the state of a linear dynamic system from a series of noisy measurements.

## Future Plans

- [ ] Implement most important signal processing algorithms.
  - [x] PELT
    - [x] L1 Cost Function
    - [x] L2 Cost Function
    - [x] RBF Cost Function
  - [x] Savitzky-Golay Filter
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

Contributions are welcome! If you have ideas, suggestions, or bug reports, feel free to open an issue or submit a pull request. We appreciate your help in making SignalSharp better.

## License

SignalSharp is licensed under the MIT License. See the [LICENSE](./LICENSE) file for more details.
