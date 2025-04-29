# Getting Started

Welcome to SignalSharp! This guide will help you install the library and get started with basic signal processing tasks.

## Installation

To install SignalSharp, you can use the .NET CLI:

```sh
dotnet add package SignalSharp
```

Alternatively, you can add SignalSharp to your project via the NuGet Package Manager UI in Visual Studio, JetBrains Rider, or your preferred IDE.

## Library Overview

SignalSharp provides a range of tools organized into several categories:

*   **Change Point Detection:** Algorithms like [PELT](./detection/pelt.md) and [CUSUM](./detection/cusum.md) find points in time where signal properties change significantly.
*   **Smoothing:** Techniques such as [Moving Average](./smoothing/moving-average.md) and the [Savitzky-Golay Filter](./smoothing/savitzky-golay-filter.md) help reduce noise and highlight underlying trends.
*   **Resampling:** Includes methods for [downsampling](./resampling/resampling.md) signals and computing [segment statistics](./resampling/resampling.md) (mean, median, etc.).
*   **Cost Functions:** Various functions (L1, L2, RBF, Likelihood-based) used by algorithms like PELT to quantify segment characteristics. See the [PELT documentation](./detection/pelt.md#cost-functions) for details.

## Basic Usage Example: Applying the Savitzky-Golay Filter

Let's demonstrate a common use case: smoothing a noisy signal using the Savitzky-Golay filter. This filter fits polynomial segments to the data, which can reduce noise while preserving the shape of features like peaks better than a simple moving average.

1.  **Prepare your data:** Start with an array representing your signal.

    ```csharp
    // Sample noisy signal
    double[] noisySignal = {1.0, 2.5, 2.0, 3.5, 4.0, 5.0, 4.5, 6.0};
    ```

2.  **Apply the filter:** Use `SavitzkyGolayFilter.Apply` method. You need to specify the `windowLength` (odd number, how many points to use for each polynomial fit) and the `polynomialOrder` (degree of the polynomial, must be less than `windowLength`).

    ```csharp
    using SignalSharp.Smoothing.SavitzkyGolay; // Import the necessary namespace
    using System; // For Console.WriteLine

    // Sample noisy signal
    double[] noisySignal = {1.0, 2.5, 2.0, 3.5, 4.0, 5.0, 4.5, 6.0};

    // Define filter parameters
    int windowLength = 5; // Use 5 points for each polynomial fit
    int polynomialOrder = 2; // Fit a 2nd degree (quadratic) polynomial

    // Apply the filter
    double[] smoothedSignal = SavitzkyGolayFilter.Apply(
        inputSignal: noisySignal,
        windowLength: windowLength,
        polynomialOrder: polynomialOrder
    );

    // Display the results
    Console.WriteLine("Original Signal: " + string.Join(", ", noisySignal));
    Console.WriteLine("Smoothed Signal: " + string.Join(", ", smoothedSignal));
    ```

    *(Note: The exact output depends on the filter parameters and implementation details, but it will be a smoothed version of the input.)*

## Next Steps

This example shows just one capability of SignalSharp. To learn more about specific algorithms and see detailed usage examples:

*   Explore the documentation pages for [Detection](./detection/pelt.md), [Smoothing](./smoothing/moving-average.md), and [Resampling](./resampling/resampling.md).
*   Check the API Reference section linked within each algorithm's documentation page.

Dive into the specific documentation relevant to your task to understand the available options and best practices.