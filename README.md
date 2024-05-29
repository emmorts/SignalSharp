SignalSharp is a library designed for signal processing and time series analysis.

## Features
~~~~
- **PELT Algorithm**: Efficiently detects multiple change points in time series data.
  - **L1 Cost Function**: Robust to outliers and non-Gaussian noise.
  - **L2 Cost Function**: Suitable for normally distributed data.
  - **RBF Cost Function**: Handles non-linear relationships between data points.
- **Savitzky-Golay Filter**: Smooths data to reduce noise while preserving the shape of the signal.

## Future Plans****

- [ ] Implement additional signal processing algorithms.
- [ ] Add more cost functions for the PELT algorithm.
- [ ] Enhance the performance of existing algorithms.
- [ ] Provide more comprehensive examples and documentation.

## Installation

To install SignalSharp, you can use NuGet Package Manager:

```sh
dotnet add package SignalSharp
```

## Usage

### PELT Algorithm

The PELT algorithm can be used with different cost functions to detect change points in time series data.

#### Example: Using PELT with L2 Cost Function

```csharp
using SignalSharp;

double[] signal = { /* your time series data */ };
double penalty = 10.0;

var pelt = new PELTAlgorithm(new PELTOptions
{
    CostFunction = new L2CostFunction(),
    MinSize = 2,
    Jump = 5
});

int[] changePoints = pelt.FitPredict(signal, penalty);

Console.WriteLine("Change Points: " + string.Join(", ", changePoints));
```

#### Example: Using PELT with RBF Cost Function

```csharp
using SignalSharp;

double[] signal = { /* your time series data */ };
double penalty = 10.0;

var pelt = new PELTAlgorithm(new PELTOptions
{
    CostFunction = new RBFCostFunction(gamma: 0.5),
    MinSize = 2,
    Jump = 5
});

int[] changePoints = pelt.FitPredict(signal, penalty);

Console.WriteLine("Change Points: " + string.Join(", ", changePoints));
```

### Smoothing Filters

The Savitzky-Golay filter can be used to smooth a noisy signal.

#### Example: Using Savitzky-Golay Filter

```csharp
using SignalSharp;

double[] signal = { /* your noisy signal data */ };
int windowSize = 5;
int polynomialOrder = 2;

double[] smoothedSignal = SavitzkyGolay.Filter(signal, windowSize, polynomialOrder);

Console.WriteLine("Smoothed Signal: " + string.Join(", ", smoothedSignal));
```

## License

SignalSharp is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.
