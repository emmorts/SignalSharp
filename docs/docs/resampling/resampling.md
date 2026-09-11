# Resampling

Resampling is a critical process in signal processing and data analysis. It involves altering the sampling rate of a signal to either reduce (downsample) or increase (upsample) the number of samples. This is particularly useful when dealing with signals of different sampling rates, reducing data size, or preparing data for further analysis. SignalSharp provides various methods for resampling signals, including downsampling and segment statistics.

## Overview

The @"SignalSharp.Resampling.Resampling" class includes methods for:
- **Downsampling**: Reducing the number of samples in a signal.
- **Segment Statistics**: Computing statistics (mean, median, max, min) for segments of a signal.

### Why Resampling?

Resampling is necessary in various scenarios:
- **Data Reduction**: Downsampling helps reduce the size of large datasets, making them easier to manage and analyze.
- **Rate Matching**: When combining or comparing signals recorded at different sampling rates, resampling ensures consistency.
- **Feature Extraction**: Segment-based statistics can summarize and simplify signals, making it easier to extract meaningful features for further analysis.

## Usage Examples

Here are some practical examples demonstrating how to use the resampling methods.

### Example 1: Downsampling Heart Rate Data

In wearable devices, heart rate data is often collected at high sampling rates. Downsampling can reduce the data size for storage and further analysis.

```csharp
double[] heartRateData = {75, 76, 77, 78, 75, 74, 76, 78, 79, 77, 76, 75};
int factor = 3;
double[] downsampledHeartRate = Resampling.Downsample(heartRateData, factor);
Console.WriteLine("Downsampled Heart Rate: " + string.Join(", ", downsampledHeartRate));
```

### Example 2: Computing Segment Statistics for Temperature Data

Segment statistics are useful for summarizing long-term trends in environmental data, such as temperature readings from weather stations.

#### Segment Mean

```csharp
double[] temperatureReadings = {20.1, 20.3, 20.5, 21.0, 21.2, 21.3, 21.5, 22.0, 22.1, 22.3};
int factor = 3;
double[] segmentMeans = Resampling.SegmentMean(temperatureReadings, factor);
Console.WriteLine("Segment Means: " + string.Join(", ", segmentMeans));
```

#### Segment Median

```csharp
double[] temperatureReadings = {20.1, 20.3, 20.5, 21.0, 21.2, 21.3, 21.5, 22.0, 22.1, 22.3};
int factor = 3;
double[] segmentMedians = Resampling.SegmentMedian(temperatureReadings, factor, true);
Console.WriteLine("Segment Medians: " + string.Join(", ", segmentMedians));
```

## Moving Averages

Smoothing a signal with a moving average is provided by the @"SignalSharp.Smoothing.MovingAverage.MovingAverage" class, not by `Resampling`. Use `SimpleMovingAverage` for an unweighted average, `WeightedMovingAverage` for custom weights, or `ExponentialMovingAverage` for exponential weighting:

```csharp
using SignalSharp.Smoothing.MovingAverage;

double[] stockPrices = {150, 152, 153, 155, 158, 157, 156, 158, 160, 162, 161, 159};
int windowSize = 3;
double[] smoothedStockPrices = MovingAverage.SimpleMovingAverage(stockPrices, windowSize);
Console.WriteLine("Smoothed Stock Prices: " + string.Join(", ", smoothedStockPrices));
```

With the default `Padding.None`, the output length is `signal.Length - windowSize + 1`: the averages are taken over sliding, overlapping windows. The padded modes instead return one value per input sample, as does `ExponentialMovingAverage` (which has no window and therefore no valid-mode trimming). See the [Moving Average](../smoothing/moving-average.md) documentation for the full set of methods and padding modes.

So if you want a moving average that *reduces* the sample count:

| Goal                                             | Call                                                                                            | Output length                                             |
|:-------------------------------------------------|:------------------------------------------------------------------------------------------------|:----------------------------------------------------------|
| Sliding averages over the signal                  | `MovingAverage.SimpleMovingAverage(signal, windowSize)` (default `Padding.None`); `WeightedMovingAverage` behaves the same way | `signal.Length - windowSize + 1`                          |
| One average per non-overlapping block             | `Resampling.SegmentMean(signal, factor)` (and `SegmentMedian`, `SegmentMax`, `SegmentMin`)       | one value per block of `factor` samples, final block included |
| Every `factor`-th sample, without averaging       | `Resampling.Downsample(signal, factor)`                                                          | `signal.Length / factor` samples, rounded up              |

The removed `Resampling.MovingAverage(signal, windowSize)` behaved like the first row: `SimpleMovingAverage(signal, windowSize)` is its direct replacement.

> **Note:** SignalSharp 0.1.1 and earlier exposed `Resampling.MovingAverage` and `Resampling.ChebyshevApproximation`. Both were removed in 0.1.2: the moving average now lives in `SignalSharp.Smoothing.MovingAverage`, and Chebyshev approximation is no longer part of the library.

## Understanding the `useQuickSelect` argument in `SegmentMedian`

The `useQuickSelect` argument in the `SegmentMedian` method determines the algorithm used for median computation:

- **QuickSelect Algorithm (useQuickSelect = true)**:
  - **Efficiency**: Has an average-case time complexity of O(n), making it efficient for larger datasets.
  - **Purpose**: Suitable for processing large signals quickly.
  - **Usage**: Preferred when performance is critical, especially with large segments.
  - **Mechanism**: Finds the k-th smallest element in an unordered list, adapted to find the median by selecting the middle element.

- **Sort-and-Select Method (useQuickSelect = false)**:
  - **Simplicity**: Has a time complexity of O(n log n) due to the sorting step.
  - **Purpose**: Simpler to understand and implement.
  - **Usage**: Suitable for smaller datasets or when algorithmic complexity is less of a concern.
  - **Mechanism**: Sorts the segment and selects the middle element (or the average of the two middle elements for even-sized segments).

## API References

- @"SignalSharp.Resampling.Resampling"
- @"SignalSharp.Smoothing.MovingAverage.MovingAverage"
