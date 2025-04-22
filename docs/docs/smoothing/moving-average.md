# Moving Average

The @"SignalSharp.Smoothing.MovingAverage" class provides methods to calculate various types of moving averages on a signal. Moving averages are fundamental tools in signal processing and time series analysis, primarily used for smoothing data to reveal underlying trends, reduce noise, and highlight patterns by averaging out short-term fluctuations.

## How Moving Averages Work

Moving averages operate by creating a series of averages derived from different subsets of the full data set. A window of a fixed size slides over the data points, and at each position, an average is calculated for the data within that window. Different types of moving averages vary in how this average is calculated (e.g., simple arithmetic mean, weighted average, exponential weighting).

## Types of Moving Averages

SignalSharp provides implementations for the following common types:

### Simple Moving Average (SMA)

The Simple Moving Average (SMA) is the unweighted mean of the previous $k$ data points, where $k$ is the window size. It gives equal weight to all data points within the window.

The @"SignalSharp.Smoothing.MovingAverage.SimpleMovingAverage(double[],int,SignalSharp.Common.Models.Padding,double)?text=SimpleMovingAverage" method calculates the SMA using an efficient O(N) sliding window algorithm.

#### Parameters

-   **signal**: `double[]` - The input signal array.
-   **windowSize**: `int` - The number of points to include in each average calculation. Must be positive.
-   **padding**: @"SignalSharp.Common.Models.Padding" - The padding mode to use. Determines how boundaries are handled and the length of the output array. Default is `Padding.None`.
-   **paddedValue**: `double` - The value used for padding if `Padding.Constant` is selected. Default is 0.

#### Padding Behavior

-   **`Padding.None`**: The output array length is `signal.Length - windowSize + 1`. Only averages where the window fully overlaps the original signal are computed ('valid' mode). `windowSize` must not exceed `signal.Length`.
-   **Other Modes (`Constant`, `Mirror`, etc.)**: The output array length is the same as `signal.Length`. Padding is applied internally before calculation to compute averages near the boundaries ('same' mode length).

#### Example

```csharp
using SignalSharp.Smoothing.MovingAverage;
using SignalSharp.Common.Models;
using System;

double[] signal = {1, 2, 3, 4, 5};
int windowSize = 3;

// No padding ('valid' mode)
double[] smaValid = MovingAverage.SimpleMovingAverage(signal, windowSize, Padding.None);
Console.WriteLine("SMA (Valid): " + string.Join(", ", smaValid));
// Output: SMA (Valid): 2, 3, 4 (Length = 5 - 3 + 1 = 3)

// Constant padding ('same' mode length)
double[] smaPadded = MovingAverage.SimpleMovingAverage(signal, windowSize, Padding.Constant, 0);
Console.WriteLine("SMA (Padded): " + string.Join(", ", smaPadded));
// Output: SMA (Padded): 1, 2, 3, 4, 3 (Length = 5)
// (Calculated on padded signal {0, 1, 2, 3, 4, 5, 0})
```

#### Computational Complexity

-   O(N), where N is the length of the signal, due to the efficient sliding window implementation.

### Exponential Moving Average (EMA)

The Exponential Moving Average (EMA) applies weighting factors that decrease exponentially. Recent observations are given more weight than older observations. The degree of weighting decrease is expressed as a smoothing factor $\alpha$.

The formula is recursive:
$$ \text{EMA}_t = \alpha \cdot \text{signal}_t + (1 - \alpha) \cdot \text{EMA}_{t-1} $$
where $\text{EMA}_0$ is typically initialized as $\text{signal}_0$.

The @"SignalSharp.Smoothing.MovingAverage.ExponentialMovingAverage(double[],double)?text=ExponentialMovingAverage" method calculates the EMA directly on the input signal. Padding is not applicable to the standard EMA calculation.

#### Parameters

-   **signal**: `double[]` - The input signal array.
-   **alpha**: `double` - The smoothing factor. Must be in the range (0, 1]. A higher alpha places more weight on recent observations.

#### Example

```csharp
using SignalSharp.Smoothing.MovingAverage;
using System;

double[] signal = {1, 2, 3, 4, 5};
double alpha = 0.5; // Smoothing factor (equal weight to current point and previous EMA)

double[] ema = MovingAverage.ExponentialMovingAverage(signal, alpha);
Console.WriteLine("EMA: " + string.Join(", ", ema));
// Output: EMA: 1, 1.5, 2.25, 3.125, 4.0625
// (Length is the same as the input signal)
```

#### Computational Complexity

-   O(N), where N is the length of the signal, as it involves a single pass through the data.

### Weighted Moving Average (WMA)

The Weighted Moving Average (WMA) assigns different weights to data points within the window. Typically, more recent points are given higher weights, but arbitrary weights can be used. The length of the `weights` array determines the window size (`W`).

The calculation is:
$$ \text{WMA}_i = \frac{\sum_{j=0}^{W-1} \text{signal}_{i+j} \cdot \text{weights}_j}{\sum_{j=0}^{W-1} \text{weights}_j} $$
(Assuming `Padding.None`, index `i` refers to the start of the window in the original signal).

The @"SignalSharp.Smoothing.MovingAverage.WeightedMovingAverage(double[],double[],SignalSharp.Common.Models.Padding,double)?text=WeightedMovingAverage" method calculates the WMA.

#### Parameters

-   **signal**: `double[]` - The input signal array.
-   **weights**: `double[]` - The weights to apply. The length determines the window size (`W`). Must not be empty, and the sum of weights must not be zero.
-   **padding**: @"SignalSharp.Common.Models.Padding" - The padding mode to use. Determines how boundaries are handled and the length of the output array. Default is `Padding.None`.
-   **paddedValue**: `double` - The value used for padding if `Padding.Constant` is selected. Default is 0.

#### Padding Behavior

-   **`Padding.None`**: The output array length is `signal.Length - W + 1`. Only averages where the window fully overlaps the original signal are computed ('valid' mode). `W` must not exceed `signal.Length`.
-   **Other Modes (`Constant`, `Mirror`, etc.)**: The output array length is the same as `signal.Length`. Padding is applied internally before calculation ('same' mode length).

#### Example

```csharp
using SignalSharp.Smoothing.MovingAverage;
using SignalSharp.Common.Models;
using System;

double[] signal = {1, 2, 3, 4, 5};
double[] weights = {0.1, 0.3, 0.6}; // Window size W=3, Sum = 1.0

// No padding ('valid' mode)
double[] wmaValid = MovingAverage.WeightedMovingAverage(signal, weights, Padding.None);
Console.WriteLine("WMA (Valid): " + string.Join(", ", wmaValid));
// Output: WMA (Valid): 2.5, 3.5, 4.5 (Length = 5 - 3 + 1 = 3)

// Constant padding ('same' mode length)
double[] wmaPadded = MovingAverage.WeightedMovingAverage(signal, weights, Padding.Constant, 0);
Console.WriteLine("WMA (Padded): " + string.Join(", ", wmaPadded));
// Output: WMA (Padded): 1.5, 2.5, 3.5, 4.5, 1.9 (Length = 5)
// (Calculated on padded signal {0, 1, 2, 3, 4, 5, 0})
```

#### Computational Complexity

-   O(N * W), where N is the signal length and W is the window size (weights length). The weighted sum is recalculated for each output point.

## Padding Modes for SMA and WMA

When using SMA or WMA, the `padding` parameter affects how the calculations are performed near the signal boundaries. The @"SignalSharp.Common.Models.Padding" enum provides these options:

-   **`None`**: Computes the moving average only where the window fits entirely within the original signal. The output is shorter than the input.
-   **`Constant`**: Pads the signal boundaries with a specified `paddedValue` before computing the moving average. The output has the same length as the input.
-   **`Mirror`**: Pads by reflecting signal values symmetrically around the boundaries. Output length matches input length.
-   **`Nearest`**: Pads by repeating the first and last signal values at the boundaries. Output length matches input length.
-   **`Periodic`**: Pads by wrapping the signal around (treating it as periodic). Output length matches input length.

The choice of padding mode depends on the specific application and assumptions about the signal's behavior beyond its observed range.

## API References

-   @"SignalSharp.Smoothing.MovingAverage"
-   @"SignalSharp.Common.Models.Padding"
-   @"SignalSharp.Common.SignalPadding" (Used internally by SMA/WMA when padding is applied)