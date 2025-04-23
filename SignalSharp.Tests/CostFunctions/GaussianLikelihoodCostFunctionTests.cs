using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Utilities;

namespace SignalSharp.Tests.CostFunctions;

[TestFixture]
public class GaussianLikelihoodCostFunctionTests
{
    // Use a tolerance appropriate for double comparisons
    private const double Tolerance = 1e-9;

    // Use the library's variance epsilon for testing zero-variance cases
    private static readonly double VarianceEpsilon = NumericUtils.GetVarianceEpsilon<double>();

    [Test]
    public void Fit_NullSignalMatrix_ThrowsArgumentNullException()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        Assert.Throws<ArgumentNullException>(() => costFunction.Fit((double[,])null!), "Fitting with null 2D signal should throw ArgumentNullException.");
    }

    [Test]
    public void Fit_NullSignalArray_ThrowsArgumentNullException()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        Assert.Throws<ArgumentNullException>(() => costFunction.Fit((double[])null!), "Fitting with null 1D signal should throw ArgumentNullException.");
    }

    [Test]
    public void Fit_ValidData_Success()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        double[,] data2D =
        {
            { 1.0, 2.0, 3.0 },
        };
        double[] data1D = [1.0, 2.0, 3.0];

        Assert.DoesNotThrow(() => costFunction.Fit(data2D), "Fitting with valid 2D data should succeed.");
        // Reset internal state by creating a new instance or ensure Fit overwrites
        costFunction = new GaussianLikelihoodCostFunction();
        Assert.DoesNotThrow(() => costFunction.Fit(data1D), "Fitting with valid 1D data should succeed.");
    }

    [Test]
    public void ComputeCost_BeforeFit_ThrowsUninitializedDataException()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        Assert.Throws<UninitializedDataException>(() => costFunction.ComputeCost(), "ComputeCost before Fit should throw UninitializedDataException.");
    }

    [Test]
    public void ComputeLikelihoodMetric_BeforeFit_ThrowsUninitializedDataException()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        // Assuming start and end are valid, the check for initialization should happen first
        Assert.Throws<UninitializedDataException>(
            () => costFunction.ComputeLikelihoodMetric(0, 1),
            "ComputeLikelihoodMetric before Fit should throw UninitializedDataException."
        );
    }

    [Test]
    public void GetSegmentParameterCount_BeforeFit_ThrowsUninitializedDataException()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        Assert.Throws<UninitializedDataException>(
            () => costFunction.GetSegmentParameterCount(1),
            "GetSegmentParameterCount before Fit should throw UninitializedDataException."
        );
    }

    [Test]
    public void SupportsInformationCriteria_ReturnsTrue()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        Assert.That(costFunction.SupportsInformationCriteria, Is.True, "SupportsInformationCriteria should return true.");
    }

    [Test]
    public void GetSegmentParameterCount_ReturnsCorrectValue()
    {
        var costFunction = new GaussianLikelihoodCostFunction();
        double[,] data1D =
        {
            { 1.0, 2.0, 3.0 },
        }; // 1 dimension
        double[,] data2D =
        {
            { 1.0, 2.0 },
            { 3.0, 4.0 },
        }; // 2 dimensions

        costFunction.Fit(data1D);
        Assert.That(costFunction.GetSegmentParameterCount(3), Is.EqualTo(2), "Parameter count for 1D signal should be 2."); // 1 dim * 2 params (mean, var)

        costFunction.Fit(data2D);
        Assert.That(costFunction.GetSegmentParameterCount(2), Is.EqualTo(4), "Parameter count for 2D signal should be 4."); // 2 dims * 2 params
    }

    [Test]
    public void ComputeCostAndLikelihood_MeanChangeOnly()
    {
        // Segments have same variance (0), different means
        double[,] data =
        {
            { 1.0, 1.0, 1.0, 5.0, 5.0, 5.0 },
        };
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 3): Var=0 -> clamped. Cost ~ 3 * log(VarianceEpsilon)
        var cost1 = costFunction.ComputeCost(0, 3);
        var likelihood1 = costFunction.ComputeLikelihoodMetric(0, 3);
        // Variance MLE is 0. Clamped variance is VarianceEpsilon. Metric = n * log(clamped_var) = 3 * log(VarianceEpsilon)
        var expectedCost1 = 3.0 * Math.Log(VarianceEpsilon);
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance), "Cost1 calculation");
        Assert.That(likelihood1, Is.EqualTo(expectedCost1).Within(Tolerance), "Likelihood1 calculation");

        // Cost for segment [3, 6): Var=0 -> clamped. Cost ~ 3 * log(VarianceEpsilon)
        var cost2 = costFunction.ComputeCost(3, 6);
        var likelihood2 = costFunction.ComputeLikelihoodMetric(3, 6);
        var expectedCost2 = 3.0 * Math.Log(VarianceEpsilon);
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance), "Cost2 calculation");
        Assert.That(likelihood2, Is.EqualTo(expectedCost2).Within(Tolerance), "Likelihood2 calculation");

        // Cost for the whole signal [0, 6): Var > 0
        var costTotal = costFunction.ComputeCost(0, 6);
        var likelihoodTotal = costFunction.ComputeLikelihoodMetric(0, 6);
        // Manual calculation: n=6, sum=18, sumSq=78
        // sumSqDev = 78 - (18*18)/6 = 78 - 54 = 24
        // varMLE = 24 / 6 = 4
        // Cost = 6 * log(4)
        var expectedTotal = 6.0 * Math.Log(4.0);
        Assert.That(costTotal, Is.EqualTo(expectedTotal).Within(Tolerance), "CostTotal calculation");
        Assert.That(likelihoodTotal, Is.EqualTo(expectedTotal).Within(Tolerance), "LikelihoodTotal calculation");

        Assert.That(cost1 + cost2, Is.LessThan(costTotal), "Sum of segment costs should be less than total cost for mean change.");
    }

    [Test]
    public void ComputeCostAndLikelihood_VarianceChangeOnly()
    {
        // Segments have same mean (0), different variance
        double[,] data =
        {
            { -0.1, 0.0, 0.1, -2.0, 0.0, 2.0 },
        }; // Mean=0 for both halves
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 3): n=3, sum=0, sumSq=0.02
        // sumSqDev = 0.02. varMLE = 0.02 / 3. Cost = 3 * log(0.02 / 3)
        var cost1 = costFunction.ComputeCost(0, 3);
        var likelihood1 = costFunction.ComputeLikelihoodMetric(0, 3);
        var expectedCost1 = 3.0 * Math.Log(0.02 / 3.0);
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance), "Cost1 calculation");
        Assert.That(likelihood1, Is.EqualTo(expectedCost1).Within(Tolerance), "Likelihood1 calculation");

        // Cost for segment [3, 6): n=3, sum=0, sumSq=8
        // sumSqDev = 8. varMLE = 8 / 3. Cost = 3 * log(8 / 3)
        var cost2 = costFunction.ComputeCost(3, 6);
        var likelihood2 = costFunction.ComputeLikelihoodMetric(3, 6);
        var expectedCost2 = 3.0 * Math.Log(8.0 / 3.0);
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance), "Cost2 calculation");
        Assert.That(likelihood2, Is.EqualTo(expectedCost2).Within(Tolerance), "Likelihood2 calculation");

        // Cost for whole segment [0, 6): n=6, sum=0, sumSq=8.02
        // sumSqDev = 8.02. varMLE = 8.02 / 6. Cost = 6 * log(8.02 / 6)
        var costTotal = costFunction.ComputeCost(0, 6);
        var likelihoodTotal = costFunction.ComputeLikelihoodMetric(0, 6);
        var expectedTotal = 6.0 * Math.Log(8.02 / 6.0);
        Assert.That(costTotal, Is.EqualTo(expectedTotal).Within(Tolerance), "CostTotal calculation");
        Assert.That(likelihoodTotal, Is.EqualTo(expectedTotal).Within(Tolerance), "LikelihoodTotal calculation");

        Assert.That(cost1 + cost2, Is.LessThan(costTotal), "Sum of segment costs should be less than total cost for variance change.");
    }

    [Test]
    public void ComputeCostAndLikelihood_SegmentWithZeroVariance()
    {
        double[,] data =
        {
            { 2.0, 2.0, 2.0, 2.0 },
        };
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 4): n=4, sum=8, sumSq=16
        // sumSqDev = 16 - (8*8)/4 = 0
        // varMLE = max(0, VarianceEpsilon) = VarianceEpsilon
        // Cost = n * log(varMLE) = 4 * log(VarianceEpsilon)
        var cost = costFunction.ComputeCost(0, 4);
        var likelihood = costFunction.ComputeLikelihoodMetric(0, 4);
        var expectedCost = 4.0 * Math.Log(VarianceEpsilon);

        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance), "Cost calculation for zero variance segment");
        Assert.That(likelihood, Is.EqualTo(expectedCost).Within(Tolerance), "Likelihood calculation for zero variance segment");
    }

    [Test]
    public void ComputeCostAndLikelihood_Subset()
    {
        double[,] data =
        {
            { 1.0, 1.5, 2.0, 2.5, 3.0 },
        };
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [1, 4): n=3, values={1.5, 2.0, 2.5}, sum=6, sumSq=12.5
        // sumSqDev = 12.5 - (6*6)/3 = 0.5
        // varMLE = 0.5 / 3
        // Cost = 3 * log(0.5 / 3)
        var cost = costFunction.ComputeCost(1, 4);
        var likelihood = costFunction.ComputeLikelihoodMetric(1, 4);
        var expectedCost = 3.0 * Math.Log(0.5 / 3.0);

        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance), "Cost calculation for subset");
        Assert.That(likelihood, Is.EqualTo(expectedCost).Within(Tolerance), "Likelihood calculation for subset");
    }

    [Test]
    public void ComputeCostAndLikelihood_SinglePoint()
    {
        double[,] data =
        {
            { 5.0 },
        };
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 1): n=1, sum=5, sumSq=25
        // sumSqDev = 25 - (5*5)/1 = 0
        // varMLE = max(0, VarianceEpsilon) = VarianceEpsilon
        // Cost = 1 * log(VarianceEpsilon)
        var cost = costFunction.ComputeCost(0, 1);
        var likelihood = costFunction.ComputeLikelihoodMetric(0, 1);
        var expectedCost = Math.Log(VarianceEpsilon);

        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance), "Cost calculation for single point");
        Assert.That(likelihood, Is.EqualTo(expectedCost).Within(Tolerance), "Likelihood calculation for single point");
    }

    [Test]
    public void ComputeCostAndLikelihood_EmptyData()
    {
        double[,] data =
        {
            { },
        }; // 0 dimensions, 0 points
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);
        var cost = costFunction.ComputeCost();
        // Need valid indices for ComputeLikelihoodMetric, but Fit handles empty data.
        // Let's test ComputeCost which defaults to full range (0, 0).
        Assert.That(cost, Is.EqualTo(0), "Cost for empty data should be 0.");
        // Test ComputeLikelihoodMetric with explicit empty range
        Assert.That(costFunction.ComputeLikelihoodMetric(0, 0), Is.EqualTo(0), "Likelihood for empty data segment should be 0.");
    }

    [Test]
    public void ComputeCostAndLikelihood_EmptyDataNonZeroDim()
    {
        var data = new double[2, 0]; // 2 dimensions, 0 points
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);
        var cost = costFunction.ComputeCost();
        Assert.That(cost, Is.EqualTo(0), "Cost for empty data (non-zero dims) should be 0.");
        Assert.That(costFunction.ComputeLikelihoodMetric(0, 0), Is.EqualTo(0), "Likelihood for empty data segment (non-zero dims) should be 0.");
    }

    [Test]
    public void ComputeCostAndLikelihood_MultiDimensional()
    {
        double[,] data =
        {
            { 1.0, 1.0, 5.0, 5.0 }, // Dim 0: Var=0 in halves
            { -0.1, 0.1, -2.0, 2.0 }, // Dim 1: Var > 0 in halves
        };
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        // --- Segment [0, 2) ---
        // Dim 0: n=2, sum=2, sumSq=2. sumSqDev=0. varMLE=VarianceEpsilon. cost_dim0 = 2*log(VarEps)
        // Dim 1: n=2, sum=0, sumSq=0.02. sumSqDev=0.02. varMLE=0.01. cost_dim1 = 2*log(0.01)
        var cost1 = costFunction.ComputeCost(0, 2);
        var likelihood1 = costFunction.ComputeLikelihoodMetric(0, 2);
        var expectedCost1 = (2.0 * Math.Log(VarianceEpsilon)) + (2.0 * Math.Log(0.01));
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance), "Cost1 calculation (multi-dim)");
        Assert.That(likelihood1, Is.EqualTo(expectedCost1).Within(Tolerance), "Likelihood1 calculation (multi-dim)");

        // --- Segment [2, 4) ---
        // Dim 0: n=2, sum=10, sumSq=50. sumSqDev=0. varMLE=VarianceEpsilon. cost_dim0 = 2*log(VarEps)
        // Dim 1: n=2, sum=0, sumSq=8. sumSqDev=8. varMLE=4. cost_dim1 = 2*log(4)
        var cost2 = costFunction.ComputeCost(2, 4);
        var likelihood2 = costFunction.ComputeLikelihoodMetric(2, 4);
        var expectedCost2 = (2.0 * Math.Log(VarianceEpsilon)) + (2.0 * Math.Log(4.0));
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance), "Cost2 calculation (multi-dim)");
        Assert.That(likelihood2, Is.EqualTo(expectedCost2).Within(Tolerance), "Likelihood2 calculation (multi-dim)");

        // --- Full Segment [0, 4) ---
        // Dim 0: n=4, sum=12, sumSq=52. sumSqDev = 52 - 144/4 = 52-36=16. varMLE=16/4=4. cost_dim0 = 4*log(4)
        // Dim 1: n=4, sum=0, sumSq=8.02. sumSqDev=8.02. varMLE=8.02/4=2.005. cost_dim1 = 4*log(2.005)
        var costTotal = costFunction.ComputeCost(0, 4);
        var likelihoodTotal = costFunction.ComputeLikelihoodMetric(0, 4);
        var expectedCostTotal = (4.0 * Math.Log(4.0)) + (4.0 * Math.Log(2.005));
        Assert.That(costTotal, Is.EqualTo(expectedCostTotal).Within(Tolerance), "CostTotal calculation (multi-dim)");
        Assert.That(likelihoodTotal, Is.EqualTo(expectedCostTotal).Within(Tolerance), "LikelihoodTotal calculation (multi-dim)");

        Assert.That(cost1 + cost2, Is.LessThan(costTotal), "Sum of multi-dim segment costs should be less than total cost.");
    }

    [Test]
    public void ComputeCost_InvalidSegment_Throws()
    {
        double[,] data =
        {
            { 1, 2, 3, 4, 5 },
        };
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeCost(-1, 3), "Negative start index should throw.");
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeCost(0, 6), "End index out of bounds should throw.");
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeCost(3, 2), "Start index > end index should throw.");
        Assert.Throws<SegmentLengthException>(() => costFunction.ComputeCost(2, 2), "Zero length segment should throw.");
    }

    [Test]
    public void ComputeLikelihoodMetric_InvalidSegment_Throws()
    {
        double[,] data =
        {
            { 1, 2, 3, 4, 5 },
        };
        var costFunction = (ILikelihoodCostFunction)new GaussianLikelihoodCostFunction().Fit(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeLikelihoodMetric(-1, 3), "Negative start index should throw.");
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeLikelihoodMetric(0, 6), "End index out of bounds should throw.");
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeLikelihoodMetric(3, 2), "Start index > end index should throw.");
        Assert.Throws<SegmentLengthException>(() => costFunction.ComputeLikelihoodMetric(2, 2), "Zero length segment should throw.");
    }
}
