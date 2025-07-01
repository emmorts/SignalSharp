# Holt's Linear Trend Method

Holt's Linear Trend Method, also known as **Double Exponential Smoothing**, is a powerful forecasting algorithm for univariate time series data that exhibits a trend but no seasonality. It extends Simple Exponential Smoothing by adding a second smoothing parameter to model the trend component. This allows the method to produce forecasts that are not flat but follow the observed trend.

## How Holt's Method Works

The method operates by updating two components at each time step: a **level** ($L_t$) and a **trend** ($T_t$).

* The **level** is a smoothed estimate of the value of the signal at time `t`.
* The **trend** is a smoothed estimate of the rate of growth or decline of the signal at time `t`.

The core of the method lies in its smoothing equations, which can be of two types: **additive** or **multiplicative**.

### Additive Trend

The additive model is suitable for time series where the trend is linear and its magnitude does not depend on the level of the series. The equations are:

$L_t = \alpha y_t + (1 - \alpha) (L_{t-1} + T_{t-1})$
$T_t = \beta (L_t - L_{t-1}) + (1 - \beta) T_{t-1}$

Where:
* $y_t$ is the actual observation at time $t$.
* $\alpha$ is the smoothing factor for the level.
* $\beta$ is the smoothing factor for the trend.

The forecast for `h` steps into the future is then:

$\hat{y}_{t+h} = L_t + h \cdot T_t$

### Multiplicative Trend

The multiplicative model is better for series where the trend's magnitude is proportional to the level of the series (e.g., exponential growth). For this model to work, all signal values must be positive. The equations are:

$L_t = \alpha y_t + (1 - \alpha) (L_{t-1} \cdot T_{t-1})$
$T_t = \beta \frac{L_t}{L_{t-1}} + (1 - \beta) T_{t-1}$

The forecast for `h` steps into the future is:

$\hat{y}_{t+h} = L_t \cdot T_t^h$

### Damped Trend

Holt's method can sometimes over-forecast, especially for long horizons. To mitigate this, a **damping parameter**, $\phi$ (Phi), can be introduced. This parameter dampens the trend over time, causing it to approach a constant value in the future.

The damped additive equations are:

$L_t = \alpha y_t + (1 - \alpha) (L_{t-1} + \phi T_{t-1})$
$T_t = \beta (L_t - L_{t-1}) + (1 - \beta) \phi T_{t-1}$
$\hat{y}_{t+h} = L_t + (\sum_{i=1}^{h} \phi^i) T_t$

A value of $\phi$ between 0 and 1 dampens the trend. A value of 1 is equivalent to the standard, undamped Holt's method.

## Automatic Parameter Optimization

A key feature of the `HoltMethodExtrapolator` is its ability to automatically find the best smoothing parameters (`Alpha`, `Beta`, `Phi`) if they are not provided. When you call `Fit()` without specifying these values, the extrapolator performs a **grid search** to find the combination of parameters that minimizes the Sum of Squared Errors (SSE) for one-step-ahead forecasts on the historical data. This can lead to more accurate forecasts but comes at the cost of increased computation time during the fitting process.

## Configuration (`HoltMethodOptions`)

The behavior of the algorithm is controlled by the `HoltMethodOptions` record:

* **`Alpha`** (`double?`): The smoothing factor for the level (between 0 and 1). If `null`, it will be optimized.
* **`Beta`** (`double?`): The smoothing factor for the trend (between 0 and 1). If `null`, it will be optimized.
* **`TrendType`** (`HoltMethodTrendType`): The type of trend, either `Additive` (default) or `Multiplicative`. Multiplicative trend requires all signal values to be positive.
* **`DampTrend`** (`bool`): If `true`, a damped trend is used. Defaults to `false`.
* **`Phi`** (`double?`): The damping parameter (between 0 and 1). Required if `DampTrend` is `true` and you are not optimizing it. If `null` and `DampTrend` is `true`, it will be optimized.
* **`InitialLevel`** (`double?`): An optional initial value for the level. If not provided, it's estimated from the first data point.
* **`InitialTrend`** (`double?`): An optional initial value for the trend. If not provided, it's estimated from the first two data points.
* **`OptimizationGridSteps`** (`int`): The number of steps for the grid search when optimizing parameters. Default is 10.

## Usage Example

This example demonstrates how to use the `HoltMethodExtrapolator` for a signal with a clear trend.

```csharp
using SignalSharp.Extrapolation.ExponentialSmoothing;
using System;

// Sample signal with an upward trend
double[] signal = { 10, 12, 14, 16, 18, 20, 22, 24 };

// 1. Configure Holt's method with fixed parameters (Additive Trend)
var options = new HoltMethodOptions 
{ 
    Alpha = 0.4, 
    Beta = 0.3, 
    TrendType = HoltMethodTrendType.Additive 
};
var extrapolator = new HoltMethodExtrapolator<double>(options);

// 2. Fit the model and extrapolate the next 4 data points
double[] forecast = extrapolator.FitAndExtrapolate(signal, 4);

Console.WriteLine("Signal: " + string.Join(", ", signal));
Console.WriteLine("Forecast (Additive): " + string.Join(", ", forecast.Select(v => v.ToString("F2"))));

// 3. Configure with automatic parameter optimization and a damped trend
var optimizationOptions = new HoltMethodOptions
{
    Alpha = null, // Optimize Alpha
    Beta = null,  // Optimize Beta
    Phi = null,   // Optimize Phi
    DampTrend = true,
    TrendType = HoltMethodTrendType.Additive
};
var optimizingExtrapolator = new HoltMethodExtrapolator<double>(optimizationOptions);

// 4. Fit and extrapolate with optimization
double[] dampedForecast = optimizingExtrapolator.FitAndExtrapolate(signal, 4);

Console.WriteLine("Forecast (Damped, Optimized): " + string.Join(", ", dampedForecast.Select(v => v.ToString("F2"))));

/*
Expected Output (values for optimized forecast may vary slightly):

Signal: 10, 12, 14, 16, 18, 20, 22, 24
Forecast (Additive): 25.87, 27.81, 29.76, 31.70
Forecast (Damped, Optimized): 26.00, 28.00, 30.00, 32.00
*/
````

## When to Use Holt's Method

### Strengths

  * **Trend Handling**: Excellent for forecasting data with a clear, persistent trend.
  * **Flexibility**: Supports both additive and multiplicative trends, with an optional damping parameter for better long-term forecasts.
  * **Automatic Tuning**: Can automatically find optimal smoothing parameters, simplifying the configuration process.

### Weaknesses

  * **No Seasonality**: The standard Holt's method does not account for seasonal patterns. For seasonal data, the Holt-Winters method is more appropriate.
  * **Parameter Sensitivity**: The forecast quality can be sensitive to the choice of smoothing parameters, though automatic optimization helps mitigate this.

## API References

  * **Extrapolator**: `SignalSharp.Extrapolation.ExponentialSmoothing.HoltMethodExtrapolator<T>`
  * **Options**: `SignalSharp.Extrapolation.ExponentialSmoothing.HoltMethodOptions`
  * **Interface**: `SignalSharp.Extrapolation.IExtrapolator<T>`
