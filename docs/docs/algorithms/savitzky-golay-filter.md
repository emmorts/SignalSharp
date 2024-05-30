# Savitzky-Golay Filter

The Savitzky-Golay filter is a widely used digital filter for smoothing data. It is particularly effective at preserving the features of a dataset, such as relative maxima, minima, and width, which are usually flattened by other smoothing techniques. 

The Savitzky-Golay filter smooths a signal by fitting successive sub-sets of adjacent data points with a low-degree polynomial using the method of linear least squares. This technique reduces noise while preserving the shape and features of the original signal.

It works by fitting a polynomial of a specified degree to a set of data points within a moving window of a specified size. The polynomial coefficients are computed using least squares minimization. The fitted polynomial is then used to estimate the value of the signal at the central point of the window. This process is repeated for each point in the signal, resulting in a smoothed version of the original data.

### Parameters

- **Window Length**: The number of data points used in each fitting window. Must be an odd number to ensure a central point.
- **Polynomial Order**: The degree of the polynomial used for fitting. Must be less than the window length.

## Usage Examples

Here are some practical examples demonstrating how to use the Savitzky-Golay filter in different scenarios:

### Example 1: Basic Smoothing of a Noisy Signal

```csharp
double[] signal = {1.0, 2.5, 2.0, 3.5, 4.0, 5.0, 4.5, 6.0};
var savitzkyGolay = new SavitzkyGolay(windowLength: 5, polynomialOrder: 2);
double[] smoothedSignal = savitzkyGolay.Filter(signal);
Console.WriteLine("Smoothed Signal: " + string.Join(", ", smoothedSignal));
```

### Example 2: Smoothing Sensor Data

Assume you have temperature readings from a sensor:

```csharp
double[] temperatureReadings = {22.1, 22.3, 22.5, 23.0, 23.1, 23.3, 23.7, 24.0, 24.1};
var savitzkyGolay = new SavitzkyGolay(windowLength: 7, polynomialOrder: 2);
double[] smoothedTemperature = savitzkyGolay.Filter(temperatureReadings);
Console.WriteLine("Smoothed Temperature: " + string.Join(", ", smoothedTemperature));
```

### Example 3: Financial Data Smoothing

Smooth stock prices to identify trends:

```csharp
double[] stockPrices = {150.0, 152.0, 151.5, 153.0, 154.5, 155.0, 156.0, 157.5};
var savitzkyGolay = new SavitzkyGolay(windowLength: 5, polynomialOrder: 2);
double[] smoothedStockPrices = savitzkyGolay.Filter(stockPrices);
Console.WriteLine("Smoothed Stock Prices: " + string.Join(", ", smoothedStockPrices));
```

### Example 4: Biomedical Signal Processing

Smooth ECG data for better analysis:

```csharp
double[] ecgData = {0.1, 0.2, 0.15, 0.3, 0.35, 0.25, 0.4, 0.45, 0.5};
var savitzkyGolay = new SavitzkyGolay(windowLength: 5, polynomialOrder: 3);
double[] smoothedEcg = savitzkyGolay.Filter(ecgData);
Console.WriteLine("Smoothed ECG Data: " + string.Join(", ", smoothedEcg));
```

## Advantages and Limitations

### Advantages

- **Preserves Data Features**: Unlike other smoothing methods, the Savitzky-Golay filter maintains the original shape and features of the signal, such as peaks and valleys.
- **Customizable**: The window length and polynomial order can be adjusted to suit specific needs, providing flexibility in the degree of smoothing.

### Limitations

- **Edge Effects**: The filter may introduce artifacts at the beginning and end of the signal due to the lack of data points in these regions.

## API References

- @"SignalSharp.Filters.SavitzkyGolay.SavitzkyGolay"