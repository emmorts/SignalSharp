# PELT Algorithm

The PELT (Pruned Exact Linear Time) algorithm is a powerful and efficient method for detecting multiple change points
within time series data. It identifies the points in time where the statistical properties of the signal change
significantly. PELT scales well with the size of the data due to its pruning technique, making it suitable for various
applications.

## How PELT Works

PELT uses dynamic programming to find the optimal segmentation of a time series. It aims to partition the data into
segments where the statistical properties are homogeneous *within* each segment but differ *between* segments.

The algorithm minimizes a total cost, which is the sum of the costs for each segment plus a penalty for each change
point introduced. The objective function to minimize is:

$$ \text{TotalCost} = \min_{t_1, \dots, t_m} \sum_{i=0}^{m} \left( \mathcal{C}(y_{t_i+1 : t_{i+1}}) + \beta \right) $$

Where:

- $y$ is the time series data of length $N$.
- $t_0 = 0 < t_1 < \dots < t_m < t_{m+1} = N$ are the change point indices. $m$ is the total number of change points
  detected.
- $y_{a:b}$ represents the segment of the time series from index $a$ up to (and including) index $b$.
- $\mathcal{C}(y_{a:b})$ is the **cost** of the segment $y_{a:b}$, calculated using a chosen cost function. This
  measures how well the segment fits a model (e.g., how close points are to the segment mean for L2 cost).
- $\beta$ is the **penalty** value applied for introducing each change point.

PELT efficiently finds the optimal set of change points $(t_1, \dots, t_m)$ by iteratively calculating the minimum cost
to segment the series up to each time point $t$, using a pruning step to keep the computation time close to linear
in $N$.

## Configuration (@"SignalSharp.Detection.PELT.PELTOptions?text=PELTOptions")

The behavior of the PELT algorithm is controlled via the @"SignalSharp.Detection.PELT.PELTOptions" record:

- **`CostFunction`**: An instance implementing `IPELTCostFunction` (e.g., @"
  SignalSharp.CostFunctions.Cost.L1CostFunction", @"SignalSharp.CostFunctions.Cost.L2CostFunction", @"
  SignalSharp.CostFunctions.Cost.RBFCostFunction"). This determines how the cost of each potential segment is
  calculated. The choice depends on the data characteristics (see Cost Functions below). *Default: @"
  SignalSharp.CostFunctions.Cost.L2CostFunction"*.
- **`MinSize`**: The minimum allowable number of data points within a segment. Ensures that segments are not too small.
  Must be greater than or equal to 1 (often >= 2 is practical). *Default: 2*.
- **`Jump`**: Controls the step size when considering previous potential change points. *Default: 5*.
    - If `Jump = 1`, PELT checks every possible prior change point, guaranteeing an **exact** solution (finding the true
      minimum of the objective function).

    * If `Jump > 1`, PELT only considers previous change points at intervals of `Jump`. This significantly speeds up
      computation, especially for large datasets, but the solution becomes **approximate**. The quality of the
      approximation is generally good but not guaranteed to be optimal.

### How Penalty ($\beta$) Affects the Algorithm

The penalty term ($\beta$) controls the trade-off between the goodness of fit (low segment costs) and the number of
change points (model complexity):

- **High Penalty**: Fewer change points are detected. The algorithm requires stronger evidence (larger changes in the
  data) to justify adding a segment boundary. This helps prevent overfitting and detects only major shifts.
- **Low Penalty**: More change points are detected. The algorithm becomes more sensitive to smaller fluctuations in the
  data. This can be useful for capturing subtle changes but risks identifying noise as change points (overfitting).

Choosing the appropriate penalty value is crucial and often requires domain knowledge or experimentation (e.g., using
methods like BIC/AIC, elbow plots, or cross-validation if applicable).

### Cost Functions

The choice of cost function is critical as it defines what constitutes a "change" in the data. SignalSharp provides
several options, each suited for different types of data characteristics and change patterns.

#### @"SignalSharp.CostFunctions.Cost.L1CostFunction?text=L1CostFunction"

The L1 cost function measures the sum of absolute deviations from the segment median:

$$ \mathcal{C}(y_{a:b}) = \sum_{i=a}^{b-1} | y_i - \text{median}(y_{a:b}) | $$

This function detects changes in central tendency, specifically the median. It focuses on the bulk of the data and is
less influenced by extreme values compared to L2. The L1 cost function is highly robust to outliers and non-Gaussian
noise, making it suitable for time series with known or suspected outliers, spikes, or heavy-tailed noise distributions
such as Laplace or Cauchy.

The current implementation precomputes medians for all possible segments, which has a high precomputation cost (roughly
O(N² * D * log N)). The segment cost calculation then involves lookup and summation over the segment length (O(
segment_length * D)).

L1 is ideal for financial data with jumps or sensor data with transient errors where the median provides a more stable
representation of the segment's typical value than the mean. It supports multidimensional data and requires a minimum
segment size of at least 1.

#### @"SignalSharp.CostFunctions.Cost.L2CostFunction?text=L2CostFunction"

The L2 cost function measures the sum of squared deviations from the segment mean:

$$ \mathcal{C}(y_{a:b}) = \sum_{i=a}^{b-1} ( y_i - \text{mean}(y_{a:b}) )^2 $$

This is equivalent to minimizing the within-segment variance, assuming variance is constant across segments. The L2
function is highly sensitive to changes in the mean, as well as to outliers since large deviations are squared. It
performs optimally when data within segments is approximately normally distributed with constant variance.

The implementation uses prefix sums of the data and the squared data, calculated during the `Fit` phase (O(N * D)
precomputation). This allows subsequent segment cost calculations in constant time per dimension (O(D)), making it
computationally efficient.

L2 is best suited for relatively clean signals where changes in the average level are the primary focus, and the
variance is assumed to be stable across different regimes. It works well for data that resembles Gaussian noise around
potentially different means. L2 supports multidimensional data and requires a minimum segment size of at least 1.

#### @"SignalSharp.CostFunctions.Cost.GaussianLikelihoodCostFunction?text=GaussianLikelihoodCostFunction"

The Gaussian likelihood cost function measures the Gaussian negative log-likelihood. The cost is primarily driven by the
term:

$$ \mathcal{C}(y_{a:b}) \approx (b-a) \times \log(\hat{\sigma}^2_{a:b}) $$

where $\hat{\sigma}^2_{a:b}$ is the Maximum Likelihood Estimate (MLE) of the variance for the segment $y_{a:b}$.
Constant terms related to $(b-a)$ and $\pi$ are often omitted as they don't affect the location of change points when
using a penalty.

This function detects changes in both mean and variance. Changes in either will affect the estimated variance and thus
the cost. It's sensitive to shifts in the mean (which affects the variance calculation relative to the segment mean) and
directly sensitive to changes in the spread or volatility of the data. Like L2, it's sensitive to outliers and assumes
data within segments follows a Gaussian distribution.

The implementation is similar to L2, using prefix sums of data and squared data (O(N * D) precomputation). Segment cost
calculation is O(D). It includes a small epsilon to handle segments with zero variance numerically.

This cost function is best for situations where the signal might exhibit changes in both its average level and its
variability (volatility). It's commonly used in financial time series (market regimes), biological signals, or process
control data where both setpoint and noise levels might change. It supports multidimensional data and requires a minimum
segment size of at least 1.

#### @"SignalSharp.CostFunctions.Cost.PoissonLikelihoodCostFunction?text=PoissonLikelihoodCostFunction"

The Poisson likelihood cost function is based on the Poisson negative log-likelihood, suitable for count data. The cost
is derived from:

$$ \mathcal{C}(y_{a:b}) = 2 \left[ S - S \log(S) + S \log(n) \right] $$

where $S = \sum_{i=a}^{b-1} y_i$ is the sum of counts in the segment, and $n = b-a$ is the segment length. It
assumes $0 \log(0) = 0$.

This function detects changes in the underlying rate ($\lambda$) of events. It assumes that counts within a segment
follow a Poisson distribution with a constant rate, estimated by the sample mean $\hat{\lambda} = S/n$. It is sensitive
to changes in the average count level.

The implementation uses prefix sums of the data (O(N * D) precomputation). Segment cost calculation is O(D). Input data
must be non-negative counts (small negative values close to zero might be tolerated).

This cost function is ideal for data representing counts of events per interval, such as website hits per day, defects
per batch, or calls per hour, where you expect the average rate of events to change over time. It supports
multidimensional data and requires a minimum segment size of at least 1.

#### @"SignalSharp.CostFunctions.Cost.BernoulliLikelihoodCostFunction?text=BernoulliLikelihoodCostFunction"

The Bernoulli likelihood cost function is derived from the Binomial negative log-likelihood for binary (0/1) data. The
cost is:

$$ \mathcal{C}(y_{a:b}) = -2 \left[ S \log(S) + (n-S) \log(n-S) - n \log(n) \right] $$

where $S = \sum_{i=a}^{b-1} y_i$ is the number of successes (sum of 1s), $n = b-a$ is the segment length, and $n-S$ is
the number of failures (sum of 0s). It assumes $0 \log(0) = 0$.

This function detects changes in the probability of success ($p$) in a sequence of Bernoulli trials. It assumes data
within a segment consists of independent 0/1 outcomes with a constant success probability, estimated by the sample
proportion $\hat{p} = S/n$.

The implementation uses prefix sums of the data (O(N * D) precomputation). Segment cost calculation is O(D). Input data
must be strictly 0 or 1 (or numerically very close within a small tolerance).

This cost function is suited for binary time series data, such as machine status (up/down), test results (pass/fail), or
presence/absence indicators, where the underlying probability of the '1' outcome is expected to change. It supports
multidimensional data and requires a minimum segment size of at least 1.

#### @"SignalSharp.CostFunctions.Cost.BinomialLikelihoodCostFunction?text=BinomialLikelihoodCostFunction"

The Binomial likelihood cost function is used for data where each time point represents $k$ successes out of $n$ trials.
The cost function is:

$$ \mathcal{C}(y_{a:b}) = - \left[ K \log(K) + (N-K) \log(N-K) - N \log(N) \right] $$

where $K = \sum_{i=a}^{b-1} k_i$ is the total number of successes, $N = \sum_{i=a}^{b-1} n_i$ is the total number of
trials in the segment. It assumes $0 \log(0) = 0$.

This function detects changes in the underlying success probability ($p$) when the number of trials ($n_i$) might vary
at each time point. It assumes data within a segment follows a Binomial distribution with a constant success
probability, estimated by $\hat{p} = K/N$.

The implementation requires a 2D input array where row 0 contains the successes ($k_i$) and row 1 contains the
trials ($n_i$). It uses prefix sums for both $k$ and $n$ (O(N) precomputation, as D=2). Segment cost calculation is O(
1). Input $k_i$ and $n_i$ must be non-negative integers with $0 \le k_i \le n_i$ and $n_i \ge 1$.

This cost function is ideal when your data consists of success counts out of a known (possibly varying) number of trials
at each time point, such as the number of successful conversions per marketing campaign or the number of defective items
per batch of varying size. It supports only this specific 2D input format and requires a minimum segment size of at
least 1.

#### @"SignalSharp.CostFunctions.Cost.RBFCostFunction?text=RBFCostFunction"

The RBF cost function measures segment (in)homogeneity using a Radial Basis Function (RBF) kernel. The cost reflects how
dissimilar points within the segment are from each other in the feature space induced by the kernel:

$$ \mathcal{K}(x_i, x_j) = \exp(-\gamma \| x_i - x_j \|^2) $$
$$ \mathcal{C}(y_{a:b}) \approx (b-a) - \frac{1}{b-a} \sum_{i=a}^{b-1} \sum_{j=a}^{b-1} \mathcal{K}(y_i, y_j) $$

A low cost indicates points are 'close' in the kernel space, suggesting homogeneity. This function can detect more
complex patterns beyond simple mean/variance shifts, including potential changes in the shape of the data distribution,
clustering, or non-linear dynamics. The exact nature depends heavily on the `gamma` parameter.

The sensitivity is highly dependent on the choice of the `gamma` parameter, which controls the locality of the kernel. A
small `gamma` considers broader relationships, while a large `gamma` focuses on local density. If `gamma` is not
provided, it's estimated using the median heuristic (based on pairwise distances).

The implementation requires computing a pairwise distance matrix and the corresponding kernel (Gram) matrix (O(N² * D)).
Then, prefix sums are computed on the kernel matrix (O(N² * D)). Precomputation is computationally expensive, but
segment cost calculation uses the prefix sums and is O(D).

RBF is best for exploratory analysis when the type of change is unknown, or when changes involve complex, non-linear
shifts in data structure or distribution, such as identifying different phases in physical systems or changes in signal
texture. It supports multidimensional data and requires a minimum segment size of at least 1.

#### @"SignalSharp.CostFunctions.Cost.ARCostFunction?text=ARCostFunction"

The AR cost function measures the goodness-of-fit of an Autoregressive (AR) model to the segment. The cost is the
Residual Sum of Squares (RSS) after fitting the model:

$$ y_t = c + \sum_{k=1}^{p} a_k y_{t-k} + \epsilon_t $$
$$ \mathcal{C}(y_{a:b}) = \sum_{t=a+p}^{b-1} \hat{\epsilon}_t^2 $$

where $p$ is the AR order, $c$ is an optional intercept, $a_k$ are the fitted coefficients, and $\hat{\epsilon}_t$ are
the residuals.

This function detects changes in the underlying linear dynamics or autocorrelation structure of the time series, as
captured by the AR coefficients ($a_k$) and intercept ($c$). It's sensitive to changes in how past values predict the
current value. Changes in periodicity, decay rates, or feedback mechanisms captured by the AR model will increase the
cost if a single model is fit across the change.

The implementation fits an AR model using Ordinary Least Squares (OLS) via QR decomposition for each segment evaluation.
It doesn't involve significant precomputation, but the segment cost calculation is computationally more intensive than
other methods, roughly O(segment_length * order²).

The AR cost function is best for univariate time series where the phenomenon of interest is a change in the temporal
dependencies or dynamics. Examples include detecting shifts in economic regimes, changes in process feedback control, or
identifying different states in physiological signals based on their predictability. Currently, it supports univariate
data only and requires a minimum segment length sufficient to estimate the AR parameters reliably:
`MinSize >= max(order + 1, 2 * order + k)`, where `k=1` if an intercept is included, `k=0` otherwise.

### Choosing the Right Cost Function

Selecting the most appropriate cost function is crucial for obtaining meaningful results from PELT. Here are key
considerations for your decision:

**Data Type**: What kind of data do you have?

* Continuous measurements? (L1, L2, Gaussian, RBF, AR)
* Counts of events? (Poisson)
* Binary outcomes (0/1)? (Bernoulli)
* Successes out of N trials? (Binomial)

**Nature of Expected Change**: Consider what type of change you're looking for. Are you interested in shifts in average
level (mean/median)? Changes in volatility (variance)? Alterations in both? Changes in event rate or success
probability? Changes in the underlying process dynamics (autocorrelation)? Or perhaps more complex pattern changes?

**Data Characteristics**: Evaluate your data's properties. Is it noisy? Does it contain outliers or spikes? Can the data
within segments be reasonably approximated by a specific distribution (Normal, Poisson, Bernoulli/Binomial)? Is the data
univariate or multivariate?

**Computational Budget**: Assess the size of your dataset and available computing resources. Can you afford O(N²)
precomputation (L1, RBF) or per-segment OLS fitting (AR), or do you need the speed of O(N) precomputation and O(1) or O(
D) segment costs (L2, Gaussian, Poisson, Bernoulli, Binomial)?

The following table provides a comparison of the key features of each cost function:

| Feature                  | L1                              | L2                               | Gaussian-Likelihood             | Poisson-Likelihood        | Bernoulli-Likelihood      | Binomial-Likelihood            | RBF                                         | AR                                         |
|:-------------------------|:--------------------------------|:---------------------------------|:--------------------------------|:--------------------------|:--------------------------|:-------------------------------|:--------------------------------------------|:-------------------------------------------|
| **Data Type**            | Continuous                      | Continuous                       | Continuous                      | Non-neg Counts            | Binary (0/1)              | Success/Trial Counts (k/n)     | Continuous                                  | Continuous                                 |
| **Detects Changes In**   | Median                          | Mean (assuming const. variance)  | Mean AND Variance               | Event Rate ($\lambda$)    | Success Probability ($p$) | Success Probability ($p$)      | Distribution shape, complex patterns        | Autocorrelation, dynamics                  |
| **Robustness**           | Robust to outliers              | Sensitive to outliers            | Sensitive to outliers           | Model assumptions         | Model assumptions         | Model assumptions              | Moderate (depends on data/gamma)            | Sensitive to model misspecification        |
| **Precomputation Cost**  | O(N²*D*logN) (approx)           | O(N*D) (fast)                    | O(N*D) (fast)                   | O(N*D) (fast)             | O(N*D) (fast)             | O(N) (fast)                    | O(N²*D) (slow)                              | O(1) (very fast)                           |
| **Segment Cost**         | O(seg_len*D) (approx)           | O(D) (very fast)                 | O(D) (very fast)                | O(D) (very fast)          | O(D) (very fast)          | O(1) (very fast)               | O(D) (fast, after precomp)                  | ~O(seg_len*order²) (relatively slow)       |
| **Data Assumptions**     | Outliers okay, non-Gaussian     | Approx. Normal, const. variance  | Approx. Normal                  | Poisson distribution      | Bernoulli trials          | Binomial distribution          | Non-linear patterns, distribution changes   | Stationarity within segment (for AR model) |
| **Dimensionality**       | Multi                           | Multi                            | Multi                           | Multi                     | Multi                     | Specific 2D (k/n)              | Multi                                       | Univariate ONLY                            |
| **Typical Use Case**     | Noisy signals, financial spikes | Clean signals, mean level shifts | Signals with varying volatility | Event counts, web traffic | Binary state changes      | Conversion rates, defect rates | Regime shifts, complex system state changes | Economic time series, process control data |
| **Minimum Segment Size** | >= 1                            | >= 1                             | >= 1                            | >= 1                      | >= 1                      | >= 1                           | >= 1                                        | `max(p+1, 2p+k)` where `p`=order, `k`=0/1  |

### Practical Guide to Choosing a Cost Function

Here's how to approach choosing the right cost function for your specific needs:

**Start with the data type**:
*   If you have counts (website hits, error counts), use **Poisson Likelihood**.
*   If you have binary data (success/failure, on/off), use **Bernoulli Likelihood**.
*   If you have successes out of N trials (conversions/visitors), use **Binomial Likelihood**.

**For continuous data**:
*   **For everyday change detection**, start with **L2**. It's fast, intuitive, and works well on clean data with mean shifts. When in doubt, this is your go-to option. You'll get results quickly and can always try alternatives if needed.
*   **When your data is messy with outliers**, switch to **L1**. Those spikes in sensor data, network traffic anomalies, or financial flash crashes won't throw off your results. L1 focuses on the median, giving you more reliable change points in noisy real-world data.
*   **For financial or environmental data** where both the level and volatility matter, use **Gaussian-Likelihood**. It captures those periods where not just the average changes, but also how wildly the values fluctuate – perfect for detecting market regime shifts or climate pattern changes.
*   **Working with time series that show patterns or cycles?** If you're analyzing a *univariate* series where the relationship between consecutive values matters more than their absolute levels, **AR** is your best bet. Remember to choose an order that matches your data's memory.
*   **When dealing with complex, non-linear changes** that you can't quite define, **RBF** offers flexibility. It's computationally heavier but can detect subtle pattern shifts that other functions might miss.

**Don't be afraid to experiment**. The best approach often involves trying 2-3 candidate cost functions (appropriate for your data type) on a subset of your data and comparing the results. Remember that penalty values need adjusting for each cost function – what works for L2 won't necessarily work for a likelihood-based cost function due to different cost scales.

In my experience, I end up using L2 about 60% of the time for its speed and simplicity, Gaussian Likelihood 25% of the
time (especially with financial data), and the others for specific cases. Let the characteristics of your data and what
changes you care about guide your choice.

## Usage Examples

Here are practical examples demonstrating how to use PELT:

*(Note: The specific penalty values below are illustrative; optimal values depend on the data scale and desired
sensitivity.)*

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

### Example 4: Detecting Changes in Event Rate (Poisson Likelihood Cost)

Useful for count data like website hits or error logs.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

// Number of errors per hour
double[] errorCounts = { 2, 1, 3, 2, 1, 0, 1, 8, 10, 9, 12, 7, 2, 1, 3 }; // Rate increases around index 7
var options = new PELTOptions
{
    CostFunction = new PoissonLikelihoodCostFunction(),
    MinSize = 3
};
var pelt = new PELTAlgorithm(options);

int[] changePoints = pelt.FitAndDetect(errorCounts, penalty: 4.0); // Adjust penalty for likelihood scale
Console.WriteLine("Change Points in Error Counts (Poisson Cost): " + string.Join(", ", changePoints));
// Expected: Around index 7 where the average count increases, possibly another around index 12
```

### Example 5: Detecting Changes in Binary State Probability (Bernoulli Likelihood Cost)

Suitable for 0/1 data like machine uptime/downtime.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

// Machine status (1=up, 0=down)
double[] machineStatus = { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0 }; // Change points around 4, 7, 12
var options = new PELTOptions
{
    CostFunction = new BernoulliLikelihoodCostFunction(),
    MinSize = 2
};
var pelt = new PELTAlgorithm(options);

int[] changePoints = pelt.FitAndDetect(machineStatus, penalty: 1.5); // Adjust penalty
Console.WriteLine("Change Points in Machine Status (Bernoulli Cost): " + string.Join(", ", changePoints));
// Expected: Around indices 4, 7, 12
```

### Example 6: Detecting Changes in Conversion Rate (Binomial Likelihood Cost)

When you have successes (k) out of trials (n) at each point.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

// Conversion data: Row 0 = conversions (k), Row 1 = visitors (n)
double[,] conversionData = {
    { 5, 6, 7, 6,   15, 18, 16, 17,   8, 9, 7, 8 }, // Conversions (k)
    { 100, 105, 98, 102, 100, 95, 103, 101, 100, 105, 98, 102 } // Visitors (n)
}; // Rate increases around index 4, decreases around index 8

var options = new PELTOptions
{
    CostFunction = new BinomialLikelihoodCostFunction(),
    MinSize = 3
};
var pelt = new PELTAlgorithm(options);

int[] changePoints = pelt.FitAndDetect(conversionData, penalty: 10.0); // Adjust penalty
Console.WriteLine("Change Points in Conversion Rate (Binomial Cost): " + string.Join(", ", changePoints));
// Expected: Around indices 4, 8
```

### Example 7: Detecting Complex Patterns in Financial Data (RBF Cost Function)

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

### Example 8: Detecting Changes in Autocorrelation (AR Cost Function)

The AR cost function is useful when the underlying dynamics (like autocorrelation) change.

```csharp
using SignalSharp.Detection.PELT;
using SignalSharp.CostFunctions.Cost;
using System;

// Generate AR(1) data with a change in coefficient
// y[t] = 0.8*y[t-1] + noise for first 50 points
// y[t] = 0.2*y[t-1] + noise for next 50 points
// (Helper function GenerateAR1Data assumed to exist for this example)
// double[] arSignal = GenerateAR1Data(50, 0.8, 0, 0.1).Concat(GenerateAR1Data(50, 0.2, 0, 0.1, seed: 43)).ToArray();

// Or a simpler manual example:
// Segment 1: High positive correlation
double[] signalPart1 = { 1.0, 0.9, 0.85, 0.82, 0.81 };
// Segment 2: Lower/negative correlation
double[] signalPart2 = { 0.2, -0.1, 0.05, -0.02, 0.01 };
double[] arSignal = signalPart1.Concat(signalPart2).ToArray(); // Length 10

var options = new PELTOptions
{
    // Use AR(1) cost, assuming changes in first-order autocorrelation
    CostFunction = new ARCostFunction(order: 1, includeIntercept: true),
    // AR(1)+intercept requires at least 2*1+1 = 3 points per segment (check ARCostFunction docs for exact min length)
    MinSize = 3,
    Jump = 1 // Use exact for better results, but can be > 1 for speed
};
var pelt = new PELTAlgorithm(options);

// Penalty needs tuning based on RSS scale
int[] changePoints = pelt.FitAndDetect(arSignal, penalty: 0.05); // Example penalty
Console.WriteLine("Change Points in AR Signal (AR Cost): " + string.Join(", ", changePoints));
// Expected: Around index 5 where the AR dynamics change
```

### Example 9: Impact of Penalty on Sensor Data (L1 Cost)

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

### Example 10: Detecting Change Points in Multidimensional Time Series Data (Gaussian Cost)

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

PELT is a widely used algorithm for change point detection, offering significant benefits but also requiring careful
consideration of its parameters and assumptions.

### Advantages

1. **Computational Efficiency for Large Datasets**:
    * PELT employs a pruning strategy that significantly reduces computational complexity, often achieving near-linear
      time performance (O(N)) relative to the time series length (N), especially with optimized cost functions like L2.
      This makes it feasible for analyzing very long time series where quadratic or even N*log(N) algorithms might be
      prohibitively slow.

2. **Guaranteed Optimality (Exact Mode)**:
    * When configured with `Jump = 1`, PELT guarantees finding the segmentation that globally minimizes the defined
      objective function (sum of segment costs plus penalties). This provides mathematically rigorous results, which is
      essential in applications demanding verifiable optimality.

3. **Flexibility in Defining "Change" (Cost Functions)**:
    * The algorithm's behavior can be adapted to detect different types of statistical changes by selecting an
      appropriate cost function tailored to the data type and expected change pattern (see table above).

4. **Simultaneous Analysis of Multiple Time Series (Multidimensional Support)**:
    * PELT can process multivariate time series (provided as `double[,]` where rows represent dimensions/channels and
      columns represent time points). It identifies common breakpoints where the statistical properties change across
      *all* dimensions considered together, providing insights into systemic shifts.

### Limitations and Practical Considerations

1. **Sensitivity to Parameter Selection (Penalty and `MinSize`)**:
    * The quality of the segmentation is highly dependent on the chosen `penalty` value. This parameter directly
      controls the trade-off between fitting the data closely (more change points) and model simplicity (fewer change
      points). Selecting an optimal penalty often requires experimentation, domain expertise, or the use of external
      model selection criteria (e.g., BIC, cross-validation). An inappropriate penalty can lead to significant under- or
      over-segmentation.
    * The `MinSize` parameter, defining the minimum segment length, also influences results. It prevents trivial
      segmentations but can mask short-lived events if set too high.

2. **Dependence on Appropriate Cost Function Choice**:
    * The algorithm detects changes *as defined by the cost function*. Selecting a cost function that does not align
      with the true nature of the changes in the data will lead to suboptimal or misleading results. For instance, using
      an L2 cost function might fail to effectively detect changes primarily characterized by outliers if an L1 cost
      would have been more suitable. Careful consideration of the data's properties and expected change patterns is
      necessary.

3. **Approximation Introduced by `Jump > 1`**:
    * While using `Jump > 1` significantly improves computational speed, it means the algorithm only evaluates a subset
      of potential previous change points. This introduces an approximation; the resulting segmentation is not
      guaranteed to be globally optimal according to the objective function. While often providing good results in
      practice, this loss of guaranteed optimality must be acceptable for the specific application. Use `Jump = 1` if
      exactness is required.

4. **Interpretation Requires Post-Processing**:
    * PELT identifies the *locations* of change points but does not inherently characterize the nature of the change (
      e.g., magnitude of mean shift, change in variance). Further analysis of the data within the identified segments is
      typically required to understand *what* changed and *why*.

5. **Underlying Model Assumptions (Additive Costs)**:
    * The algorithm optimizes a cost function based on the sum of costs of independent segments plus penalties. It does
      not explicitly model complex dependencies or transitions *between* segment states. While sufficient for many
      standard segmentation tasks, this assumption might be limiting for systems with strong temporal dependencies
      between regimes.

6. **Cost Function Computational Cost**:
    * Overall runtime depends on PELT's pruning and the segment cost calculation:
        * O(1) or O(D) segment cost (fast): L2, Gaussian, Poisson, Bernoulli, Binomial, RBF (after precomputation)
        * Slower segment cost: L1 (O(seg_len*D)), AR (O(seg_len*order²))
        * Slow precomputation: L1 (O(N²*D*logN)), RBF (O(N²*D))

## API References

- @"SignalSharp.Detection.PELT.PELTAlgorithm"
- @"SignalSharp.Detection.PELT.PELTOptions"
- @"SignalSharp.CostFunctions.Cost.IPELTCostFunction"
- @"SignalSharp.CostFunctions.Cost.L1CostFunction"
- @"SignalSharp.CostFunctions.Cost.L2CostFunction"
- @"SignalSharp.CostFunctions.Cost.GaussianLikelihoodCostFunction"
- @"SignalSharp.CostFunctions.Cost.PoissonLikelihoodCostFunction"
- @"SignalSharp.CostFunctions.Cost.BernoulliLikelihoodCostFunction"
- @"SignalSharp.CostFunctions.Cost.BinomialLikelihoodCostFunction"
- @"SignalSharp.CostFunctions.Cost.RBFCostFunction"
- @"SignalSharp.CostFunctions.Cost.ARCostFunction"