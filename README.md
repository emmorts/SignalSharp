SignalSharp is a C# library designed for efficient signal processing and time series analysis. Whether you're dealing with noise reduction, state estimation, or change point detection, SignalSharp provides robust tools to help you get the job done.

## Overview

Signal processing is a critical component in various applications, including telecommunications, audio processing, finance, and biomedical engineering. While other ecosystems, such as Python, have a plethora of signal processing libraries, C# lacks comprehensive open-source alternatives, often offering only paid or closed-source solutions. SignalSharp aims to fill this void by providing a free, open-source library for the C# community, enabling developers to work with advanced signal processing algorithms with ease.

### Why Use SignalSharp?

- **Performance**: SignalSharp is optimized for performance, ensuring that your signal processing tasks are executed efficiently.
- **Flexibility**: With a range of algorithms and configurable options, SignalSharp can be tailored to meet the specific requirements of your projects.
- **Ease of Use**: The library is designed with user-friendliness in mind, providing clear documentation and practical examples to help you get started.

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

## License

SignalSharp is licensed under the MIT License. See the [LICENSE](./LICENSE) file for more details.
