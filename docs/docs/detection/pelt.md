# PELT Algorithm

The PELT (Pruned Exact Linear Time) algorithm is a powerful and efficient method for detecting multiple change points within time series data. It identifies the points in time where the statistical properties of the signal change significantly. PELT scales well with the size of the data due to its pruning technique, making it suitable for various applications.

## How PELT Works

PELT uses dynamic programming to find the optimal segmentation of a time series. It aims to partition the data into segments where the statistical properties are homogeneous *within* each segment but differ *between* segments.

The algorithm minimizes a total cost, which is the sum of the costs for each segment plus a penalty for each change point introduced. The objective function to minimize is:

$$ \text{TotalCost} = \min_{t_1, \dots, t_m} \sum_{i=0}^{m} \left( \mathcal{C}(y_{t_i+1 : t_{i+1}}) + \beta \right) $$

Where:
- $y$ is the time series data of length $N$.
- $t_0 = 0 < t_1 < \dots < t_m < t_{m+1} = N$ are the change point indices. $m$ is the total number of change points detected.
- $y_{a:b}$ represents the segment of the time series from index $a$ up to (and including) index $b$.
- $\mathcal{C}(y_{a:b})$ is the **cost** of the segment $y_{a:b}$, calculated using a chosen cost function. This measures how well the segment fits a model (e.g., how close points are to the segment mean for L2 cost).
- $\beta$ is the **penalty** value applied for introducing each change point.

PELT efficiently finds the optimal set of change points $(t_1, \dots, t_m)$ by iteratively calculating the minimum cost to segment the series up to each time point $t$, using a pruning step to keep the computation time close to linear in $N$.

## Configuration (@"SignalSharp.Detection.PELT.PELTOptions")

The behavior of the PELT algorithm is controlled via the @"SignalSharp.Detection.PELT.PELTOptions" record:

-   **`CostFunction`**: An instance implementing `IPELTCostFunction` (e.g., @"SignalSharp.CostFunctions.Cost.L1CostFunction", @"SignalSharp.CostFunctions.Cost.L2CostFunction", @"SignalSharp.CostFunctions.Cost.RBFCostFunction"). This determines how the cost of each potential segment is calculated. The choice depends on the data characteristics (see Cost Functions below). *Default: @"SignalSharp.CostFunctions.Cost.L2CostFunction"*.
-   **`MinSize`**: The minimum allowable number of data points within a segment. Ensures that segments are not too small. Must be greater than or equal to 1 (often >= 2 is practical). *Default: 2*.
-   **`Jump`**: Controls the step size when considering previous potential change points. *Default: 5*.
    -   If `Jump = 1`, PELT checks every possible prior change point, guaranteeing an **exact** solution (finding the true minimum of the objective function).
    *   If `Jump > 1`, PELT only considers previous change points at intervals of `Jump`. This significantly speeds up computation, especially for large datasets, but the solution becomes **approximate**. The quality of the approximation is generally good but not guaranteed to be optimal. 

### How Penalty ($\beta$) Affects the Algorithm

The penalty term ($\beta$) controls the trade-off between the goodness of fit (low segment costs) and the number of change points (model complexity):

-   **High Penalty**: Fewer change points are detected. The algorithm requires stronger evidence (larger changes in the data) to justify adding a segment boundary. This helps prevent overfitting and detects only major shifts.
-   **Low Penalty**: More change points are detected. The algorithm becomes more sensitive to smaller fluctuations in the data. This can be useful for capturing subtle changes but risks identifying noise as change points (overfitting).

Choosing the appropriate penalty value is crucial and often requires domain knowledge or experimentation (e.g., using methods like BIC/AIC, elbow plots, or cross-validation if applicable).

### Cost Functions

SignalSharp provides several built-in cost functions suitable for different data types:

-   @"SignalSharp.CostFunctions.Cost.L1CostFunction": Measures the sum of absolute deviations from the segment median. Robust against outliers and non-Gaussian noise.
-   @"SignalSharp.CostFunctions.Cost.L2CostFunction": Measures the sum of squared deviations from the segment mean (related to variance). Optimal for data that is approximately normally distributed within segments and sensitive to outliers. Computationally very efficient (O(1) per segment after O(N) precomputation).
-   @"SignalSharp.CostFunctions.Cost.GaussianLikelihoodCostFunction": Based on the Gaussian negative log-likelihood. Assumes data within segments is normally distributed and is sensitive to changes in *both* mean and variance. Computationally efficient (O(1) per segment after O(N) precomputation).
-   @"SignalSharp.CostFunctions.Cost.RBFCostFunction": Uses a Radial Basis Function kernel to measure segment dissimilarity. Suitable for detecting changes in more complex, potentially non-linear patterns within the data. Can be computationally more intensive.

## Usage Examples

Here are practical examples demonstrating how to use PELT:

*(Note: The specific penalty values below are illustrative; optimal values depend on the data scale and desired sensitivity.)*

### Example 1: Basic Usage with L2 Cost (Default)

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

// Sample signal with clear mean shifts
double[] signal = { 1, 1, 1, 5, 5, 5, 1, 1, 1 };

// Configure PELT (using default L2 cost, MinSize=2, Jump=5)
var options = new PELTOptions();
var pelt = new PELTAlgorithm(options);

// Fit the algorithm to the data and detect change points with a penalty
// Using FitAndDetect convenience method
int[] changePoints = pelt.FitAndDetect(signal, penalty: 2.0);

Console.WriteLine("Change Points (L2 Cost, Default Opts): " + string.Join(", ", changePoints));
// Expected: [3, 6] (Indices are the first point *after* the change)
```

### Example 2: Detecting Change Points in Network Traffic (L1 Cost Function)

Network traffic data might have spikes (outliers). L1 cost is more robust.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

double[] networkTraffic = {100, 102, 101, 500, 105, 98, 99, 300, 310, 100}; // Spikes at index 3 and shift starting at 7
var options = new PELTOptions
{
    CostFunction = new L1CostFunction(),
    MinSize = 2,
    Jump = 1 // Use exact PELT for potentially better outlier handling
};
var pelt = new PELTAlgorithm(options);

int[] changePoints = pelt.FitAndDetect(networkTraffic, penalty: 50.0); // Penalty needs tuning based on data scale
Console.WriteLine("Change Points in Network Traffic (L1 Cost): " + string.Join(", ", changePoints));
// Expected (example, depends heavily on penalty): [3, 7] or similar
```

### Example 3: Detecting Regime Shifts with Variance Change (Gaussian Likelihood Cost)

This cost function is ideal when both mean and variance might shift.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

// Signal where first half has low variance, second half has high variance
double[] signalWithVarianceChange = { 0, 0.1, -0.1, 0, 0.1, 3.0, -2.0, 1.0, -3.0, 2.5 };
var options = new PELTOptions
{
    CostFunction = new GaussianLikelihoodCostFunction(),
    MinSize = 3
};
var pelt = new PELTAlgorithm(options);

int[] changePoints = pelt.FitAndDetect(signalWithVarianceChange, penalty: 3.0); // Penalty adjusted for likelihood scale
Console.WriteLine("Change Points (Gaussian Likelihood Cost): " + string.Join(", ", changePoints));
// Expected: Around index 5 where variance increases significantly
```

### Example 4: Detecting Complex Patterns in Financial Data (RBF Cost Function)

Financial data can have complex patterns. RBF might capture non-linear shifts.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

double[] financialData = {100, 102, 101, 105, 110, 112, 108, 95, 90, 88}; // Gradual rise then fall
var options = new PELTOptions
{
    CostFunction = new RBFCostFunction(gamma: 0.1), // Specify gamma or let it be auto-calculated
    MinSize = 3
};
var pelt = new PELTAlgorithm(options);

pelt.Fit(financialData); // Separate Fit and Detect calls
int[] changePoints = pelt.Detect(penalty: 1.0); // RBF costs might be smaller scale
Console.WriteLine("Change Points in Financial Data (RBF Cost): " + string.Join(", ", changePoints));
// Expected (example): Possibly around index 3 or 7 depending on gamma and penalty
```

### Example 5: Impact of Penalty on Sensor Data (L1 Cost)

Demonstrating how penalty changes sensitivity.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

double[] sensorData = {15.0, 15.2, 15.3, 20.0, 19.5, 20.1, 25.0, 24.5, 25.2};
var options = new PELTOptions { CostFunction = new L1CostFunction() };
var pelt = new PELTAlgorithm(options);
pelt.Fit(sensorData);

// High Penalty: Less sensitive, detects major shifts
int[] changePointsHighPenalty = pelt.Detect(penalty: 8.0);
Console.WriteLine("Change Points with High Penalty (L1): " + string.Join(", ", changePointsHighPenalty));
// Expected: [3, 6] (Detecting the two major level shifts)

// Low Penalty: More sensitive, detects smaller shifts
int[] changePointsLowPenalty = pelt.Detect(penalty: 1.0);
Console.WriteLine("Change Points with Low Penalty (L1): " + string.Join(", ", changePointsLowPenalty));
// Expected: Potentially more points like [3, 4, 6, 7] if small dips/rises are considered significant with low penalty
```

### Example 6: Detecting Change Points in Multidimensional Time Series Data (Gaussian Cost)

PELT supports multidimensional data. Gaussian cost handles mean/variance changes in multiple dimensions simultaneously.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

// 2 dimensions, 9 time points
double[,] multiSeriesSignal = {
    { 1.0, 1.1, 1.0,   5.0, 5.1, 4.9,   1.0, 1.1, 0.9 }, // Dimension 1: Mean shifts
    { 10.0, 10.1, 9.9, 10.0, 9.8, 10.1, 20.0, 19.8, 20.2 } // Dimension 2: Mean shifts at different points
}; // Combined change points expected around index 3 and 6

var options = new PELTOptions
{
    CostFunction = new GaussianLikelihoodCostFunction(), // Or L2CostFunction if variance assumed constant
    MinSize = 2
};
var pelt = new PELTAlgorithm(options);

int[] changePoints = pelt.FitAndDetect(multiSeriesSignal, penalty: 5.0); // Adjust penalty based on combined cost scale
Console.WriteLine("Change Points in Multidimensional Signal (Gaussian Cost): " + string.Join(", ", changePoints));
// Expected: [3, 6] (Reflecting the points where properties change in either dimension)
```

## Advantages and Limitations

PELT is a widely used algorithm for change point detection, offering significant benefits but also requiring careful consideration of its parameters and assumptions.

### Advantages

1.  **Computational Efficiency for Large Datasets**:
    *   PELT employs a pruning strategy that significantly reduces computational complexity, often achieving near-linear time performance (O(N)) relative to the time series length (N), especially with optimized cost functions like L2. This makes it feasible for analyzing very long time series where quadratic or even N*log(N) algorithms might be prohibitively slow.

2.  **Guaranteed Optimality (Exact Mode)**:
    *   When configured with `Jump = 1`, PELT guarantees finding the segmentation that globally minimizes the defined objective function (sum of segment costs plus penalties). This provides mathematically rigorous results, which is essential in applications demanding verifiable optimality.

3.  **Flexibility in Defining "Change" (Cost Functions)**:
    *   The algorithm's behavior can be adapted to detect different types of statistical changes by selecting an appropriate cost function:
        *   `L2CostFunction`: Sensitive to changes in mean, assuming constant variance. Suitable for data segments approximating normality. Fast.
        *   `GaussianLikelihoodCostFunction`: Sensitive to changes in *both* mean and variance, assuming normality. Fast.
        *   `L1CostFunction`: Robust to outliers and non-Gaussian noise, focusing on changes in the median. Useful when data contains anomalies or heavy tails.
        *   `RBFCostFunction`: Can potentially detect more complex changes in data distribution or structure beyond simple level shifts, although potentially more computationally intensive and sensitive to parameterization (`gamma`).
    *   This allows tailoring the detection to the specific characteristics of the signal and the nature of the expected changes.

4.  **Simultaneous Analysis of Multiple Time Series (Multidimensional Support)**:
    *   PELT can process multivariate time series (provided as `double[,]` where rows represent dimensions/channels and columns represent time points). It identifies common breakpoints where the statistical properties change across *all* dimensions considered together, providing insights into systemic shifts.

### Limitations and Practical Considerations

1.  **Sensitivity to Parameter Selection (Penalty and `MinSize`)**:
    *   The quality of the segmentation is highly dependent on the chosen `penalty` value. This parameter directly controls the trade-off between fitting the data closely (more change points) and model simplicity (fewer change points). Selecting an optimal penalty often requires experimentation, domain expertise, or the use of external model selection criteria (e.g., BIC, cross-validation). An inappropriate penalty can lead to significant under- or over-segmentation.
    *   The `MinSize` parameter, defining the minimum segment length, also influences results. It prevents trivial segmentations but can mask short-lived events if set too high.
~~~~
2.  **Dependence on Appropriate Cost Function Choice**:
    *   The algorithm detects changes *as defined by the cost function*. Selecting a cost function that does not align with the true nature of the changes in the data will lead to suboptimal or misleading results. For instance, using an L2 cost function might fail to effectively detect changes primarily characterized by outliers if an L1 cost would have been more suitable. Careful consideration of the data's properties and expected change patterns is necessary.

3.  **Approximation Introduced by `Jump > 1`**:
    *   While using `Jump > 1` significantly improves computational speed, it means the algorithm only evaluates a subset of potential previous change points. This introduces an approximation; the resulting segmentation is not guaranteed to be globally optimal according to the objective function. While often providing good results in practice, this loss of guaranteed optimality must be acceptable for the specific application. Use `Jump = 1` if exactness is required.

4.  **Interpretation Requires Post-Processing**:
    *   PELT identifies the *locations* of change points but does not inherently characterize the nature of the change (e.g., magnitude of mean shift, change in variance). Further analysis of the data within the identified segments is typically required to understand *what* changed and *why*.

5.  **Underlying Model Assumptions (Additive Costs)**:
    *   The algorithm optimizes a cost function based on the sum of costs of independent segments plus penalties. It does not explicitly model complex dependencies or transitions *between* segment states. While sufficient for many standard segmentation tasks, this assumption might be limiting for systems with strong temporal dependencies between regimes.

## API References

- @"SignalSharp.Detection.PELT.PELTAlgorithm"
- @"SignalSharp.Detection.PELT.PELTOptions"
- @"SignalSharp.CostFunctions.Cost.L1CostFunction"
- @"SignalSharp.CostFunctions.Cost.L2CostFunction"
- @"SignalSharp.CostFunctions.Cost.RBFCostFunction"
- @"SignalSharp.CostFunctions.Cost.GaussianLikelihoodCostFunction"