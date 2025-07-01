# Simple Exponential Smoothing

Simple Exponential Smoothing (SES) is a forecasting method for univariate time series data that does not exhibit a clear trend or seasonal pattern. It's a straightforward yet effective technique that computes future values based on a weighted average of past observations, with the weights decaying exponentially over time. This means that more recent observations are given more weight than older ones.

## How Simple Exponential Smoothing Works

The core of SES is the smoothing equation, which updates the smoothed level at each time step:

$S\_t = \\alpha y\_t + (1 - \\alpha) S\_{t-1}$

Where:

  * $S\_t$ is the smoothed value (the level) at time $t$.
  * $y\_t$ is the actual observation at time $t$.
  * $S\_{t-1}$ is the smoothed value from the previous time step.
  * $\\alpha$ is the smoothing factor, where $0 \< \\alpha \\le 1$.

A higher value of `alpha` makes the model react more quickly to recent changes, while a smaller `alpha` results in a smoother forecast as it gives more weight to past observations.

Once the model has processed the entire signal, the forecast for all future time points is the last calculated level ($S\_n$, where $n$ is the length of the signal).

-----

## Configuration (`SimpleExponentialSmoothingOptions`)

The behavior of the SES algorithm is controlled by the `SimpleExponentialSmoothingOptions` record:

  * **`Alpha`** (`double`): The smoothing factor for the level. This is a **required** parameter and must be between 0 and 1, inclusive. Higher values give more weight to recent observations.
  * **`InitialLevel`** (`double?`): An optional initial value for the smoothed level. If you don't provide one, the first data point of the signal is used as the initial level.

-----

## Usage Examples

Here are some practical examples of how to use the `SimpleExponentialSmoothingExtrapolator` in C\#.

### Example 1: Basic Extrapolation

This example demonstrates the basic usage of the `SimpleExponentialSmoothingExtrapolator` with a given `Alpha`.

```csharp
using SignalSharp.Extrapolation.ExponentialSmoothing;
using System;

// Sample signal with no clear trend or seasonality
double[] signal = { 10.0, 12.0, 15.0, 11.0, 13.0 };

// 1. Configure SES with an Alpha value
var options = new SimpleExponentialSmoothingOptions { Alpha = 0.3 };

// 2. Create the extrapolator instance
var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);

// 3. Fit the model to the historical data
extrapolator.Fit(signal);

// 4. Extrapolate to predict the next 3 data points
int horizon = 3;
double[] forecast = extrapolator.Extrapolate(horizon);

Console.WriteLine("Signal: " + string.Join(", ", signal));
Console.WriteLine("Forecast: " + string.Join(", ", forecast));
// The forecast will be an array of 3 elements, all having the value of the last calculated level.
```

### Example 2: Using an Initial Level

You can also provide a specific starting level for the smoothing process.

```csharp
using SignalSharp.Extrapolation.ExponentialSmoothing;
using System;

double[] signal = { 10.0, 12.0, 15.0 };

// 1. Configure SES with an Alpha and a specific initial level
var options = new SimpleExponentialSmoothingOptions { Alpha = 0.2, InitialLevel = 8.0 };

// 2. Create the extrapolator and use the FitAndExtrapolate convenience method
var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
int horizon = 2;
double[] forecast = extrapolator.FitAndExtrapolate(signal, horizon);

Console.WriteLine("Signal: " + string.Join(", ", signal));
Console.WriteLine("Forecast with Initial Level: " + string.Join(", ", forecast));
// L_initial = 8.0
// L1 = 0.2*10.0 + 0.8*8.0 = 2.0 + 6.4 = 8.4
// L2 = 0.2*12.0 + 0.8*8.4 = 2.4 + 6.72 = 9.12
// L3 = 0.2*15.0 + 0.8*9.12 = 3.0 + 7.296 = 10.296
// Expected forecast: [10.296, 10.296]
```

### Example 3: Impact of Alpha

This example shows how different `Alpha` values affect the forecast. An `Alpha` of 1 means the forecast will be the last value of the signal.

```csharp
using SignalSharp.Extrapolation.ExponentialSmoothing;
using System;

double[] signal = { 10.0, 12.0, 15.0 };

// High Alpha: The model gives most weight to the last observation.
var highAlphaOptions = new SimpleExponentialSmoothingOptions { Alpha = 1.0 };
var highAlphaExtrapolator = new SimpleExponentialSmoothingExtrapolator<double>(highAlphaOptions);
double[] highAlphaForecast = highAlphaExtrapolator.FitAndExtrapolate(signal, 1);

Console.WriteLine("Forecast with Alpha=1.0: " + string.Join(", ", highAlphaForecast));
// Expected: [15.0]

// Low Alpha: The model is smoothed more, relying more on past values.
var lowAlphaOptions = new SimpleExponentialSmoothingOptions { Alpha = 0.1 };
var lowAlphaExtrapolator = new SimpleExponentialSmoothingExtrapolator<double>(lowAlphaOptions);
double[] lowAlphaForecast = lowAlphaExtrapolator.FitAndExtrapolate(signal, 1);

Console.WriteLine("Forecast with Alpha=0.1: " + string.Join(", ", lowAlphaForecast));
// L_initial = 10.0
// L1 = 0.1*10 + 0.9*10 = 10
// L2 = 0.1*12 + 0.9*10 = 1.2 + 9 = 10.2
// L3 = 0.1*15 + 0.9*10.2 = 1.5 + 9.18 = 10.68
// Expected: [10.68]
```

-----

## API References

  * **Extrapolator**: `SignalSharp.Extrapolation.ExponentialSmoothing.SimpleExponentialSmoothingExtrapolator<T>`
  * **Options**: `SignalSharp.Extrapolation.ExponentialSmoothing.SimpleExponentialSmoothingOptions`
  * **Interface**: `SignalSharp.Extrapolation.IExtrapolator<T>`