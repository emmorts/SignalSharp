# Linear Extrapolation

Linear Extrapolation is a method that forecasts future values by extending a straight line fitted to a set of historical data points. It's a straightforward approach based on the assumption that a recent linear trend will continue into the short-term future.

## How Linear Extrapolation Works

The algorithm performs a simple linear regression (least-squares fit) on a window of the most recent data points to determine a `slope` and `intercept`.

The core steps are:
1.  **Select a Window**: The extrapolator looks at a specified number of recent data points from the end of the signal. This is controlled by the `WindowSize` option. If no window is specified, the entire signal is used.
2.  **Fit a Line**: A straight line, $y = mx + c$, is fitted to the points in the window, where `m` is the slope and `c` is the intercept.
3.  **Extrapolate**: The forecast is calculated by extending this line from the **last value** of the original signal. The prediction for `h` steps into the future is calculated as: `Forecast(h) = LastSignalValue + h * Slope`.

This method is fast and easy to interpret, making it suitable for short-term forecasting on data with a clear, consistent trend.

## Configuration (`LinearExtrapolationOptions`)

The behavior of the `LinearExtrapolator` is controlled by the `LinearExtrapolationOptions` record:

* **`WindowSize`** (`int?`): The number of recent historical data points to use for fitting the linear trend. This value must be at least 2. If `null` (the default), the entire signal history provided to `Fit()` will be used.
    * **Trade-offs**: Using a smaller window focuses on the most recent trend but is more sensitive to noise. Using the entire history provides a more stable, long-term trend estimate but might miss recent changes in the trend.

## Usage Example

Here’s how to use the `LinearExtrapolator` in C#.

```csharp
using SignalSharp.Extrapolation.Linear;
using System;

// Sample signal with a clear positive trend
double[] signal = { 2.0, 4.0, 6.0, 8.0, 10.0 };

// 1. Configure the extrapolator. We'll use the default options,
// which means the entire signal will be used to fit the trend.
var options = new LinearExtrapolationOptions();
var extrapolator = new LinearExtrapolator<double>(options);

// 2. Fit the model and extrapolate the next 3 data points
int horizon = 3;
double[] forecast = extrapolator.FitAndExtrapolate(signal, horizon);

// The trend from {2,4,6,8,10} has a slope of 2.0.
// The last value is 10.0.
// Forecast(1) = 10.0 + 1 * 2.0 = 12.0
// Forecast(2) = 10.0 + 2 * 2.0 = 14.0
// Forecast(3) = 10.0 + 3 * 2.0 = 16.0

Console.WriteLine("Signal: " + string.Join(", ", signal));
Console.WriteLine("Forecast: " + string.Join(", ", forecast));

// Example using a smaller window
var windowedOptions = new LinearExtrapolationOptions { WindowSize = 3 };
var windowedExtrapolator = new LinearExtrapolator<double>(windowedOptions);

// The trend will be fitted only on {6.0, 8.0, 10.0}, which also has a slope of 2.0
double[] windowedForecast = windowedExtrapolator.FitAndExtrapolate(signal, horizon);
Console.WriteLine("Forecast (Window=3): " + string.Join(", ", windowedForecast));

/*
Expected Output:

Signal: 2, 4, 6, 8, 10
Forecast: 12, 14, 16
Forecast (Window=3): 12, 14, 16
*/
````

## When to Use Linear Extrapolation

### Strengths

  * **Simplicity**: The method is very easy to understand and implement.
  * **Fast**: It is computationally inexpensive, making it suitable for real-time applications.
  * **Effective for Linear Trends**: It performs well for short-term forecasting when the underlying data has a strong linear trend.

### Weaknesses

  * **Trend Continuation**: It assumes the linear trend will continue indefinitely, which is often not the case in reality. This makes it unreliable for long-term forecasting.
  * **Sensitivity to Noise**: The trend calculation, especially with a small window, can be heavily influenced by noise in the data.
  * **No Seasonality**: It does not account for seasonal patterns or other complex behaviors.

## API References

  * **Extrapolator**: `SignalSharp.Extrapolation.Linear.LinearExtrapolator<T>`
  * **Options**: `SignalSharp.Extrapolation.Linear.LinearExtrapolationOptions`
  * **Interface**: `SignalSharp.Extrapolation.IExtrapolator<T>`
