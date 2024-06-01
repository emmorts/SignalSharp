# CUSUM Algorithm

The CUSUM (Cumulative Sum Control Chart) algorithm is a robust method for detecting change points in time series data. It is especially useful for monitoring shifts in the statistical properties of a process over time, such as the mean or variance. The CUSUM algorithm is widely applied in quality control, finance, and environmental monitoring due to its sensitivity and reliability.

CUSUM is a sequential analysis technique that accumulates the deviations of data points from an expected mean value. It uses cumulative sums to detect shifts in the process, maintaining high and low sums to identify positive and negative changes respectively.

## How CUSUM Works

CUSUM aims to identify points in time where the statistical properties of the data change. The algorithm calculates cumulative sums of deviations from the expected mean and compares these sums to a threshold to detect changes.

The algorithm minimizes the following objective function:

\[ S_t = \max(0, S_{t-1} + (x_t - \mu - k)) \]

where \( S_t \) is the cumulative sum at time \( t \), \( x_t \) is the data point at time \( t \), \( \mu \) is the expected mean, and \( k \) is the slack factor.

CUSUM uses two cumulative sums:
- **High Sum**: Detects upward shifts (positive changes) in the data.
- **Low Sum**: Detects downward shifts (negative changes) in the data.

When either cumulative sum exceeds a predefined threshold, a change point is identified.

### Parameters and Their Effects

- **Expected Mean (\( \mu \))**: The average value around which the time series data is expected to fluctuate.
- **Expected Standard Deviation (\( \sigma \))**: Represents the expected variability in the data.
- **Slack Factor (\( k \))**: Determines the allowed slack before a change is detected. It is multiplied by the expected standard deviation to compute the slack value.
- **Threshold Factor**: Sets the sensitivity of the algorithm. A higher threshold factor reduces sensitivity, detecting fewer changes. A lower threshold factor increases sensitivity, detecting more changes but potentially increasing false positives.

### Data Normalization

Normalization of data is crucial for the CUSUM algorithm to function effectively, especially when dealing with data of varying scales. The library provides built-in normalization functions such as [0,1] normalization and Z-score normalization, available in @"StatisticalFunctions.Normalize" and @"StatisticalFunctions.ZScoreNormalization".

#### Normalization Functions
- **Min-Max Normalization**: Scales the data to the [0,1] range.
- **Z-score Normalization**: Standardizes the data to have a mean of 0 and a standard deviation of 1.

## Usage Examples

Here are practical examples demonstrating how to use CUSUM in various scenarios:

### Example 1: Detecting Change Points in Network Traffic

Network traffic data can fluctuate due to anomalies or attacks. The CUSUM algorithm can detect significant changes in traffic patterns.

```csharp
double[] networkTraffic = {100, 102, 101, 200, 150, 140, 400, 130, 110};
var options = new CUSUMOptions
{
    ExpectedMean = 100,
    ExpectedStandardDeviation = 10,
    ThresholdFactor = 1.5,
    SlackFactor = 0.5
};
var cusum = new CUSUMAlgorithm(options);
int[] changePoints = cusum.Detect(networkTraffic);
Console.WriteLine("Change Points in Network Traffic: " + string.Join(", ", changePoints));
```

### Example 2: Detecting Change Points in Manufacturing Processes

Manufacturing process data is often normally distributed. The CUSUM algorithm can detect shifts in process parameters.

```csharp
double[] processMeasurements = {5.0, 5.1, 5.2, 6.0, 6.1, 6.3, 7.0, 7.1, 7.2};
var options = new CUSUMOptions
{
    ExpectedMean = 5.0,
    ExpectedStandardDeviation = 0.5,
    ThresholdFactor = 2.0,
    SlackFactor = 0.1
};
var cusum = new CUSUMAlgorithm(options);
int[] changePoints = cusum.Detect(processMeasurements);
Console.WriteLine("Change Points in Manufacturing Process: " + string.Join(", ", changePoints));
```

### Example 3: Detecting Regime Shifts in Financial Data

Financial time series data can exhibit shifts due to market changes. The CUSUM algorithm can capture these shifts effectively.

```csharp
double[] financialData = {1000, 1020, 1015, 1050, 1100, 1200, 1150, 1300, 1250};
var options = new CUSUMOptions
{
    ExpectedMean = 1050,
    ExpectedStandardDeviation = 100,
    ThresholdFactor = 1.5,
    SlackFactor = 0.2
};
var cusum = new CUSUMAlgorithm(options);
int[] changePoints = cusum.Detect(financialData);
Console.WriteLine("Change Points in Financial Data: " + string.Join(", ", changePoints));
```

## Advantages and Limitations

### Advantages

- **Sensitivity**: The CUSUM algorithm is highly sensitive to small changes in the data, making it ideal for early detection of shifts in processes.
- **Real-Time Detection**: Suitable for online monitoring and real-time detection of changes, allowing for prompt response to anomalies.
- **Flexibility**: Easily adjustable for various types of data and noise levels by tuning parameters such as the expected mean, standard deviation, slack factor, and threshold factor.
- **Versatility**: Applicable to a wide range of domains, including manufacturing, finance, network traffic, and environmental monitoring.

### Limitations

- **Parameter Sensitivity**: The algorithm's performance can be highly sensitive to the choice of parameters. Incorrect parameter settings can lead to false positives or missed detections.
- **Assumption of Normality**: The algorithm assumes a normal distribution of data points around the expected mean, which may not hold true for all datasets. Proper data preprocessing and normalization are essential for optimal performance.

## API References

- @"SignalSharp.Detection.CUSUM.CUSUMAlgorithm"