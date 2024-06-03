# Savitzky-Golay Filter

The Savitzky-Golay filter is a widely used digital filter for smoothing data. It is particularly effective at preserving the features of a dataset, such as relative maxima, minima, and width, which are usually flattened by other smoothing techniques.

The Savitzky-Golay filter smooths a signal by fitting successive sub-sets of adjacent data points with a low-degree polynomial using the method of linear least squares. This technique reduces noise while preserving the shape and features of the original signal.

It works by fitting a polynomial of a specified degree to a set of data points within a moving window of a specified size. The polynomial coefficients are computed using least squares minimization. The fitted polynomial is then used to estimate the value of the signal at the central point of the window. This process is repeated for each point in the signal, resulting in a smoothed version of the original data.

### Parameters

- **Window Length**: The number of data points used in each fitting window. Must be an odd number to ensure a central point.
- **Polynomial Order**: The degree of the polynomial used for fitting. Must be less than the window length.
- **Derivative Order**: The order of the derivative to compute. Default is 0 (no derivative).
- **Padding**: The padding to apply to the signal. Default is `None`.
- **Padded Value**: The value to use when `Padding.Constant` is set. Default is 0.

### Padding Modes

The @"SignalSharp.Common.Models.Padding" enum provides various modes to handle the edges of the signal during filtering:

- **None**: No additional values are added to the signal at the boundaries.
- **Constant**: Pads the signal with a specified constant value.
- **Mirror**: Mirrors the values at the boundary to the other side, creating a symmetric padding.
- **Nearest**: Replicates the first value at the lower boundary and the last value at the upper boundary.
- **Periodic**: Treats the signal as periodic, wrapping the end around to the start.

## Usage Examples

Here are some practical examples demonstrating how to use the Savitzky-Golay filter in different scenarios:

### Example 1: Basic Smoothing of a Noisy Signal

```csharp
double[] signal = {1.0, 2.5, 2.0, 3.5, 4.0, 5.0, 4.5, 6.0};
double[] smoothedSignal = SavitzkyGolayFilter.Apply(signal, 5, 2);
Console.WriteLine("Smoothed Signal: " + string.Join(", ", smoothedSignal));
```

### Example 2: Smoothing Sensor Data with Padding

Assume you have temperature readings from a sensor:

```csharp
double[] temperatureReadings = {22.1, 22.3, 22.5, 23.0, 23.1, 23.3, 23.7, 24.0, 24.1};
double[] smoothedTemperature = SavitzkyGolayFilter.Apply(temperatureReadings, 7, 2, padding: Padding.Mirror);
Console.WriteLine("Smoothed Temperature: " + string.Join(", ", smoothedTemperature));
```

### Example 3: Financial Data Smoothing

Smooth stock prices to identify trends:

```csharp
double[] stockPrices = {150.0, 152.0, 151.5, 153.0, 154.5, 155.0, 156.0, 157.5};
double[] smoothedStockPrices = SavitzkyGolayFilter.Apply(stockPrices, 5, 2);
Console.WriteLine("Smoothed Stock Prices: " + string.Join(", ", smoothedStockPrices));
```

### Example 4: Biomedical Signal Processing

Smooth ECG data for better analysis:

```csharp
double[] ecgData = {0.1, 0.2, 0.15, 0.3, 0.35, 0.25, 0.4, 0.45, 0.5};
double[] smoothedEcg = SavitzkyGolayFilter.Apply(ecgData, 5, 3);
Console.WriteLine("Smoothed ECG Data: " + string.Join(", ", smoothedEcg));
```

## Advantages and Limitations

### Advantages

- **Preserves Data Features**: Unlike other smoothing methods, the Savitzky-Golay filter maintains the original shape and features of the signal, such as peaks and valleys.
- **Customizable**: The window length and polynomial order can be adjusted to suit specific needs, providing flexibility in the degree of smoothing.

### Limitations

- **Edge Effects**: The filter may introduce artifacts at the beginning and end of the signal due to the lack of data points in these regions. Using padding modes such as `Mirror`, `Nearest`, or `Periodic` can help mitigate these effects.

## API References

- @`SignalSharp.Smoothing.SavitzkyGolay.SavitzkyGolayFilter`
