using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

public class GaussianLikelihoodCostFunctionTests
{
    private const double Tolerance = 1e-9;
    private const double Epsilon = 1e-10; // Must match the epsilon in the implementation

    [Test]
    public void ComputeCost_MeanChangeOnly()
    {
        // Segments have same variance (approx 0), different means
        double[,] data = { { 1.0, 1.0, 1.0, 5.0, 5.0, 5.0 } };
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 3): Var=0 -> clamped. Cost ~ 3 * log(Epsilon/3)
        var cost1 = costFunction.ComputeCost(0, 3);
        var expectedCost1 = 3.0 * Math.Log(Epsilon / 3.0);
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance));

        // Cost for segment [3, 6): Var=0 -> clamped. Cost ~ 3 * log(Epsilon/3)
        var cost2 = costFunction.ComputeCost(3, 6);
        var expectedCost2 = 3.0 * Math.Log(Epsilon / 3.0);
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance));

        // Cost for the whole signal [0, 6): Var > 0
        var costTotal = costFunction.ComputeCost(0, 6);
        // Manual calculation: n=6, sum=18, sumSq=78
        // sumSqDev = 78 - (18*18)/6 = 78 - 54 = 24
        // varMLE = 24 / 6 = 4
        // Cost = 6 * log(4)
        var expectedTotal = 6.0 * Math.Log(4.0);
        Assert.That(costTotal, Is.EqualTo(expectedTotal).Within(Tolerance));

        // Expect cost(0,3) + cost(3,6) + penalty < cost(0,6) + penalty for PELT to split
        // 3*log(eps/3) + 3*log(eps/3) vs 6*log(4)
        // 6*log(eps/3) vs 6*log(4) => log(eps/3) vs log(4) => Very negative vs positive. Good.
    }

    [Test]
    public void ComputeCost_VarianceChangeOnly()
    {
        // Segments have same mean (0), different variance
        double[,] data = { { -0.1, 0.0, 0.1, -2.0, 0.0, 2.0 } }; // Mean=0 for both halves
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 3): n=3, sum=0, sumSq=0.02
        // sumSqDev = 0.02 - (0*0)/3 = 0.02
        // varMLE = 0.02 / 3
        // Cost = 3 * log(0.02 / 3)
        var cost1 = costFunction.ComputeCost(0, 3);
        var expectedCost1 = 3.0 * Math.Log(0.02 / 3.0);
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance));

        // Cost for segment [3, 6): n=3, sum=0, sumSq=8
        // sumSqDev = 8 - (0*0)/3 = 8
        // varMLE = 8 / 3
        // Cost = 3 * log(8 / 3)
        var cost2 = costFunction.ComputeCost(3, 6);
        var expectedCost2 = 3.0 * Math.Log(8.0 / 3.0);
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance));

        // Cost for whole segment [0, 6): n=6, sum=0, sumSq=8.02
        // sumSqDev = 8.02 - (0*0)/6 = 8.02
        // varMLE = 8.02 / 6
        // Cost = 6 * log(8.02 / 6)
        var costTotal = costFunction.ComputeCost(0, 6);
        var expectedTotal = 6.0 * Math.Log(8.02 / 6.0);
        Assert.That(costTotal, Is.EqualTo(expectedTotal).Within(Tolerance));

        // Expect cost1 + cost2 < costTotal for PELT to split
        // 3*log(0.02/3) + 3*log(8/3) vs 6*log(8.02/6)
        // 3 * [ log(0.02/3 * 8/3) ] vs 6 * log(8.02/6)
        // 3 * log(0.16/9) vs 6 * log(8.02/6)
        // 3 * log(0.01777) vs 6 * log(1.3366)
        // 3 * (-4.029) vs 6 * (0.290)
        // -12.08 vs 1.74 --> Cost1+Cost2 is lower, good.
    }

    [Test]
    public void ComputeCost_SegmentWithZeroVariance()
    {
        double[,] data = { { 2.0, 2.0, 2.0, 2.0 } };
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 4): n=4, sum=8, sumSq=16
        // sumSqDev = 16 - (8*8)/4 = 16 - 16 = 0
        // varMLE = max(0, Epsilon) / 4 = Epsilon / 4
        // Cost = 4 * log(Epsilon / 4)
        var cost = costFunction.ComputeCost(0, 4);
        var expectedCost = 4.0 * Math.Log(Epsilon / 4.0);

        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_Subset()
    {
        double[,] data = { { 1.0, 1.5, 2.0, 2.5, 3.0 } };
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [1, 4): n=3, values={1.5, 2.0, 2.5}, sum=6, sumSq=4.25+4+6.25=12.5
        // sumSqDev = 12.5 - (6*6)/3 = 12.5 - 12 = 0.5
        // varMLE = 0.5 / 3
        // Cost = 3 * log(0.5 / 3)
        var cost = costFunction.ComputeCost(1, 4);
        var expectedCost = 3.0 * Math.Log(0.5 / 3.0);

        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_SinglePoint()
    {
        double[,] data = { { 5.0 } };
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);

        // Cost for segment [0, 1): n=1, sum=5, sumSq=25
        // sumSqDev = 25 - (5*5)/1 = 0
        // varMLE = max(0, Epsilon) / 1 = Epsilon
        // Cost = 1 * log(Epsilon)
        var cost = costFunction.ComputeCost(0, 1);
        var expectedCost = Math.Log(Epsilon);

        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_EmptyData()
    {
        double[,] data = { { } };
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);
        var cost = costFunction.ComputeCost();
        Assert.That(cost, Is.EqualTo(0)); // No points, cost is 0
    }

    [Test]
    public void ComputeCost_MultiDimensional()
    {
         double[,] data = {
            { 1.0, 1.0, 5.0, 5.0 },     // Dim 0: Var change
            { -0.1, 0.1, -2.0, 2.0 },   // Dim 1: Var change
        };
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);

        // Segment [0, 2)
        // Dim 0: n=2, sum=2, sumSq=2. sumSqDev=2-(2*2)/2=0. varMLE=Eps/2. cost=2*log(Eps/2)
        // Dim 1: n=2, sum=0, sumSq=0.02. sumSqDev=0.02-(0*0)/2=0.02. varMLE=0.02/2=0.01. cost=2*log(0.01)
        var cost1 = costFunction.ComputeCost(0, 2);
        var expectedCost1 = (2.0 * Math.Log(Epsilon / 2.0)) + (2.0 * Math.Log(0.01));
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance));

        // Segment [2, 4)
        // Dim 0: n=2, sum=10, sumSq=50. sumSqDev=50-(10*10)/2=0. varMLE=Eps/2. cost=2*log(Eps/2)
        // Dim 1: n=2, sum=0, sumSq=8. sumSqDev=8-(0*0)/2=8. varMLE=8/2=4. cost=2*log(4)
        var cost2 = costFunction.ComputeCost(2, 4);
        var expectedCost2 = (2.0 * Math.Log(Epsilon / 2.0)) + (2.0 * Math.Log(4.0));
         Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_InvalidSegment_Throws()
    {
        double[,] data = { { 1, 2, 3, 4, 5 } };
        var costFunction = new GaussianLikelihoodCostFunction().Fit(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeCost(-1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunction.ComputeCost(0, 6));
        Assert.Throws<SegmentLengthException>(() => costFunction.ComputeCost(2, 2));
        Assert.Throws<SegmentLengthException>(() => costFunction.ComputeCost(3, 2));
    }

     [Test]
     public void ComputeCost_BeforeFit_ThrowsUninitializedDataException()
     {
         var costFunction = new GaussianLikelihoodCostFunction();
         Assert.Throws<UninitializedDataException>(() => costFunction.ComputeCost());
     }
}