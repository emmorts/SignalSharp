# Getting Started

## Installation

To install SignalSharp, you can use NuGet Package Manager:

```sh
dotnet add package SignalSharp
```

Alternatively, you can add SignalSharp to your project via the NuGet Package Manager UI in Visual Studio, JetBrains Rider or any other IDE of your choice.

## Basic Usage

SignalSharp offers a variety of algorithms for different signal processing tasks. Here's a basic example to demonstrate how to use the library.

### Example: Applying the Savitzky-Golay Filter

The [Savitzky-Golay](./smoothing/savitzky-golay-filter.md) filter smooths a signal by fitting successive sub-sets of adjacent data points with a low-degree polynomial.

1. **Initialize the Filter**:
    ```csharp
    var savitzkyGolay = new SavitzkyGolay(windowLength: 5, polynomialOrder: 2);
    ```

2. **Apply the Filter**:
    ```csharp
    double[] signal = {1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0};
    double[] smoothedSignal = savitzkyGolay.Filter(signal);
    
    Console.WriteLine("Smoothed Signal: " + string.Join(", ", smoothedSignal));
    ```

For more examples and algorithms, please refer to the algorithm documentation page.