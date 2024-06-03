# Moving Average

The @"SignalSharp.Smoothing.MovingAverage" class provides methods to calculate various types of moving averages on a signal. Moving averages are widely used for smoothing data to highlight trends and patterns over time by reducing the effect of random fluctuations.

## Types of Moving Averages

### Simple Moving Average (SMA)

The Simple Moving Average (SMA) is calculated by taking the average of a fixed number of points in the signal, defined by the window size, and then sliding the window along the signal.

#### Parameters

- **signal**: The input signal to calculate the moving average on.
- **windowSize**: The number of points to include in each average calculation. Must be greater than zero and less than or equal to the length of the signal.
- **padding**: The padding mode to use when calculating the moving average. Default is `Padding.None`.
- **paddedValue**: The value to use for padding if `Padding.Constant` is selected. Default is 0.

#### Example

```csharp
double[] signal = {1, 2, 3, 4, 5};
int windowSize = 3;
double[] sma = MovingAverage.SimpleMovingAverage(signal, windowSize);
// sma will be {2, 3, 4}
```

### Exponential Moving Average (EMA)

The Exponential Moving Average (EMA) is calculated using a smoothing factor, alpha, which gives more weight to recent data points. The formula is: $$ \text{EMA}_t = \alpha \cdot \text{signal}_t + (1 - \alpha) \cdot \text{EMA}_{t-1} $$.

#### Parameters

- **signal**: The input signal to calculate the moving average on.
- **alpha**: The smoothing factor, must be in the range (0, 1].
- **padding**: The padding mode to use when calculating the moving average. Default is `Padding.None`.
- **paddedValue**: The value to use for padding if `Padding.Constant` is selected. Default is 0.

#### Example

```csharp
double[] signal = {1, 2, 3, 4, 5};
double alpha = 0.5;
double[] ema = MovingAverage.ExponentialMovingAverage(signal, alpha);
// ema will be {1, 1.5, 2.25, 3.125, 4.0625}
```

### Weighted Moving Average (WMA)

The Weighted Moving Average (WMA) is calculated by applying a set of weights to the points within the window size. Each point in the signal is multiplied by the corresponding weight, and the results are summed.

#### Parameters

- **signal**: The input signal to calculate the moving average on.
- **weights**: The weights to apply to the points within the window size.
- **padding**: The padding mode to use when calculating the moving average. Default is `Padding.None`.
- **paddedValue**: The value to use for padding if `Padding.Constant` is selected. Default is 0.

#### Example

```csharp
double[] signal = {1, 2, 3, 4, 5};
double[] weights = {0.1, 0.3, 0.6};
double[] wma = MovingAverage.WeightedMovingAverage(signal, weights);
// wma will be {2.5, 3.5, 4.5}
```

## Padding Modes

The @"SignalSharp.Common.Models.Padding" enum provides various modes to handle the edges of the signal during filtering:

- **None**: No additional values are added to the signal at the boundaries.
- **Constant**: Pads the signal with a specified constant value.
- **Mirror**: Mirrors the values at the boundary to the other side, creating a symmetric padding.
- **Nearest**: Replicates the first value at the lower boundary and the last value at the upper boundary.
- **Periodic**: Treats the signal as periodic, wrapping the end around to the start.

## API References

- @"SignalSharp.Smoothing.MovingAverage"