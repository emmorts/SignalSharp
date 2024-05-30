---
_layout: landing
---

# SignalSharp

SignalSharp is a C# library designed for efficient signal processing and time series analysis. Whether you're dealing with noise reduction, state estimation, or change point detection, SignalSharp provides tools to help you get the job done.

## Overview

Signal processing is a critical component in various applications, including telecommunications, audio processing, finance, and biomedical engineering. While other ecosystems, such as Python, have a plethora of signal processing libraries, C# lacks comprehensive open-source alternatives, often offering only paid or closed-source solutions. SignalSharp aims to fill this void by providing a free, open-source library for the C# community, enabling developers to work with advanced signal processing algorithms with ease.

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

## Getting Started

Ready to dive in? Check out our [Getting Started](./docs/getting-started.html) for detailed instructions on how to install and begin using SignalSharp. This guide will help you set up the library and start processing your data in no time.

Explore the documentation to learn more about each algorithm and find detailed usage examples that suit your specific needs. Whether you're working on academic research, developing new technologies, or just exploring signal processing, SignalSharp is here to support your journey.

---

If you have any questions or need help, don't hesitate to reach out to the community or open an issue on our [GitHub repository](https://github.com/emmorts/SignalSharp).
