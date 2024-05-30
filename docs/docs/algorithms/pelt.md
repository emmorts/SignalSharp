# PELT Algorithm

The PELT (Pruned Exact Linear Time) algorithm is a powerful method for detecting multiple change points in time series data. It is particularly efficient and scales well with the size of the data, making it suitable for a variety of applications where change point detection is crucial. This document provides detailed information about the PELT algorithm, including its mathematical foundation, common use cases, and practical examples.

## Overview

PELT algorithm is a dynamic programming approach that aims to partition a time series into segments where the statistical properties are homogenous within each segment but differ between segments. It uses a cost function to measure the fit of the segments and a penalty term to control the number of change points. The penalty term helps prevent overfitting by discouraging too many segments.

PELT works by minimizing the following objective function:

$$ C(y) = \sum_{i=1}^{m+1} \left[ C(y_{t_{i-1}:t_i}) + \beta \right] $$

where $ C(y_{t_{i-1}:t_i}) $ is the cost of segment $ y_{t_{i-1}:t_i} $ and $ \beta $ is the penalty term. The algorithm iteratively prunes the search space to find the optimal set of change points in linear time.

### How Penalty Affects the Algorithm

The penalty term ($ \beta $) in PELT algorithm controls the trade-off between the goodness of fit and the complexity of the model. Here’s how changing the penalty affects the behavior and results of the algorithm:

- **High Penalty**: Fewer change points are detected, and only significant changes in the data are recognized. This setting is useful when you want to avoid detecting too many false positives.
- **Low Penalty**: More change points are detected, making the algorithm sensitive to even small changes in the data. This setting can be useful when it is crucial to capture all potential changes, but it may result in overfitting.

By adjusting the penalty value, you can fine-tune the sensitivity of the PELT algorithm to match the specific requirements of your analysis.

### Cost Functions

PELT supports various cost functions to evaluate the quality of segments:
- **L1 Cost Function**: Robust against outliers and non-Gaussian noise.
- **L2 Cost Function**: Ideal for normally distributed data.
- **RBF Cost Function**: Handles non-linear data relationships.

## Usage Examples

Here are some practical examples demonstrating how to use PELT with different cost functions in various scenarios:

### Example 1: Detecting Change Points in Network Traffic (L1 Cost Function)

Network traffic data can often contain outliers due to anomalies or attacks. The L1 cost function is robust against such outliers.

```csharp
double[] networkTraffic = {100, 102, 101, 200, 150, 140, 400, 130, 110};
var options = new PELTOptions
{
    CostFunction = new L1CostFunction()
};
var pelt = new PELTAlgorithm(options);
pelt.Fit(networkTraffic);
int[] changePoints = pelt.Predict(pen: 10.0);
Console.WriteLine("Change Points in Network Traffic: " + string.Join(", ", changePoints));
```

### Example 2: Detecting Change Points in Manufacturing Processes (L2 Cost Function)

Manufacturing process data is often normally distributed, making the L2 cost function suitable for detecting shifts in process parameters.

```csharp
double[] processMeasurements = {5.0, 5.1, 5.2, 6.0, 6.1, 6.3, 7.0, 7.1, 7.2};
var options = new PELTOptions
{
    CostFunction = new L2CostFunction()
};
var pelt = new PELTAlgorithm(options);
pelt.Fit(processMeasurements);
int[] changePoints = pelt.Predict(pen: 5.0);
Console.WriteLine("Change Points in Manufacturing Process: " + string.Join(", ", changePoints));
```

### Example 3: Detecting Regime Shifts in Financial Data (RBF Cost Function)

Financial time series data can exhibit complex, non-linear patterns. The RBF cost function is well-suited for capturing such non-linear relationships.

```csharp
double[] financialData = {1000, 1020, 1015, 1050, 1100, 1200, 1150, 1300, 1250};
var options = new PELTOptions
{
    CostFunction = new RBFCostFunction(gamma: 0.1)
};
var pelt = new PELTAlgorithm(options);
pelt.Fit(financialData);
int[] changePoints = pelt.Predict(pen: 15.0);
Console.WriteLine("Change Points in Financial Data: " + string.Join(", ", changePoints));
```

### Example 4: Detecting Environmental Changes in Sensor Data (L1 Cost Function)

Environmental sensor data, such as temperature or pollution levels, may contain outliers due to sensor malfunctions or extreme events. Changing the penalty value can significantly affect the sensitivity of the PELT algorithm to detect change points.

#### High Penalty Value

A higher penalty value reduces the number of detected change points, making the algorithm less sensitive to smaller changes but preventing overfitting.

```csharp
double[] sensorData = {15.0, 15.2, 15.3, 20.0, 19.5, 20.1, 25.0, 24.5, 25.2};
var options = new PELTOptions
{
    CostFunction = new L1CostFunction()
};
var pelt = new PELTAlgorithm(options);
pelt.Fit(sensorData);
int[] changePointsHighPenalty = pelt.Predict(pen: 20.0);
Console.WriteLine("Change Points with High Penalty: " + string.Join(", ", changePointsHighPenalty));
```

#### Low Penalty Value

A lower penalty value increases the number of detected change points, making the algorithm more sensitive to small changes but potentially leading to overfitting.

```csharp
double[] sensorData = {15.0, 15.2, 15.3, 20.0, 19.5, 20.1, 25.0, 24.5, 25.2};
var options = new PELTOptions
{
    CostFunction = new L1CostFunction()
};
var pelt = new PELTAlgorithm(options);
pelt.Fit(sensorData);
int[] changePointsLowPenalty = pelt.Predict(pen: 5.0);
Console.WriteLine("Change Points with Low Penalty: " + string.Join(", ", changePointsLowPenalty));
```

## Advantages and Limitations

### Advantages

- **Efficiency**: The PELT algorithm is computationally efficient and scales linearly with the size of the data.
- **Flexibility**: Supports various cost functions to suit different types of data and noise characteristics.
- **Accuracy**: Provides accurate detection of multiple change points in time series data.

### Limitations

- **Parameter Sensitivity**: The performance of the algorithm can be sensitive to the choice of the penalty term.
- **Cost Function Choice**: Selecting the appropriate cost function based on data characteristics is crucial for optimal performance.

## API References

- @"SignalSharp.Detection.PELT.PELTAlgorithm"
- @"SignalSharp.Detection.PELT.Cost.L1CostFunction"
- @"SignalSharp.Detection.PELT.Cost.L2CostFunction"
- @"SignalSharp.Detection.PELT.Cost.RBFCostFunction"