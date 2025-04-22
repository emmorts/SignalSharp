using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

[TestFixture]
public class BernoulliLikelihoodCostFunctionTests
{
    private const double Tolerance = 1e-9;

    [Test]
    public void Fit_NullSignalMatrix_ThrowsArgumentNullException()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        Assert.Throws<ArgumentNullException>(() => costFunc.Fit((double[,])null!));
    }

     [Test]
     public void Fit_NullSignalArray_ThrowsArgumentNullException()
     {
         var costFunc = new BernoulliLikelihoodCostFunction();
         // CostFunctionBase handles null check for 1D Fit before converting
         Assert.Throws<ArgumentNullException>(() => costFunc.Fit((double[])null!));
     }

    [Test]
    public void Fit_InvalidValues_ThrowsArgumentException()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] invalidData1 = { { 0.0, 1.0, 0.5 } }; // Contains 0.5
        double[,] invalidData2 = { { 0.0, -1.0, 1.0 } }; // Contains -1.0
        double[,] invalidData3 = { { 0.0, 1.0, 2.0 } }; // Contains 2.0

        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidData1));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidData2));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidData3));
    }

    [Test]
    public void Fit_NearZeroOneValues_Success()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] nearZeroOneData = {
            { 0.0, 1.0, Tolerance / 2.0, 1.0 - Tolerance / 2.0, -Tolerance / 2.0 },
        };
        // Values within Epsilon tolerance of 0 or 1 should be accepted and clamped internally

        Assert.DoesNotThrow(() => costFunc.Fit(nearZeroOneData));
    }

     [Test]
    public void Fit_ExactlyZeroOneValues_Success()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] validData = { { 0.0, 1.0, 0.0, 1.0, 1.0 } };
        Assert.DoesNotThrow(() => costFunc.Fit(validData));
    }

    [Test]
    public void Fit_EmptyData_Success()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        var emptyData = new double[1, 0]; // 1 dimension, 0 points
        var emptyDataMultiDim = new double[2, 0]; // 2 dimensions, 0 points

        Assert.DoesNotThrow(() => costFunc.Fit(emptyData));
        Assert.DoesNotThrow(() => costFunc.Fit(emptyDataMultiDim));

        costFunc.Fit(emptyData);
        
        Assert.That(costFunc.ComputeCost(), Is.EqualTo(0.0));
    }

    [Test]
    public void Fit_MultidimensionalData_Success()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] validMultiData = {
            { 0.0, 1.0, 0.0 },
            { 1.0, 1.0, 0.0 },
        };
        Assert.DoesNotThrow(() => costFunc.Fit(validMultiData));
    }

     [Test]
     public void Fit_OneDimensionalData_Success()
     {
         var costFunc = new BernoulliLikelihoodCostFunction();
         double[] valid1DData = [0.0, 1.0, 0.0, 1.0, 1.0];
         Assert.DoesNotThrow(() => costFunc.Fit(valid1DData));
     }

    [Test]
    public void ComputeCost_BeforeFit_ThrowsUninitializedDataException()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        Assert.Throws<UninitializedDataException>(() => costFunc.ComputeCost());
    }

    [Test]
    public void ComputeCost_AfterFittingEmptyData_ReturnsZero()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        var emptyData = new double[1, 0];
        costFunc.Fit(emptyData);
        Assert.That(costFunc.ComputeCost(), Is.EqualTo(0.0));

        var emptyDataMultiDim = new double[2, 0];
        costFunc.Fit(emptyDataMultiDim);
         Assert.That(costFunc.ComputeCost(), Is.EqualTo(0.0));
    }

    [Test]
    public void ComputeCost_FullSegment_AllZeros_ReturnsZero()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = { { 0.0, 0.0, 0.0, 0.0 } };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(); // S=0, n=4
        Assert.That(cost, Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_FullSegment_AllOnes_ReturnsZero()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = { { 1.0, 1.0, 1.0 } };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(); // S=3, n=3
        Assert.That(cost, Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_FullSegment_Mixed_CalculatesCorrectCost()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = { { 0.0, 1.0, 0.0, 1.0 } }; // n=4, S=2
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost();

        // Expected cost = -2 * [ S*log(S) + (n-S)*log(n-S) - n*log(n) ]
        // S=2, n=4, n-S=2
        // Cost = -2 * [ 2*log(2) + 2*log(2) - 4*log(4) ]
        // Cost = -2 * [ 4*log(2) - 4*log(2^2) ]
        // Cost = -2 * [ 4*log(2) - 4*2*log(2) ]
        // Cost = -2 * [ 4*log(2) - 8*log(2) ]
        // Cost = -2 * [ -4*log(2) ]
        // Cost = 8 * log(2)
        var expectedCost = 8.0 * Math.Log(2.0);
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_SubsetSegment_Mixed_CalculatesCorrectCost()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = { { 0.0, 1.0, 0.0, 1.0, 1.0 } };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(1, 4); // Segment [1, 4): {1, 0, 1} => n=3, S=2

        // Expected cost = -2 * [ S*log(S) + (n-S)*log(n-S) - n*log(n) ]
        // S=2, n=3, n-S=1
        // Cost = -2 * [ 2*log(2) + 1*log(1) - 3*log(3) ]
        // Cost = -2 * [ 2*log(2) + 0 - 3*log(3) ]
        // Cost = -4*log(2) + 6*log(3)
        var expectedCost = -4.0 * Math.Log(2.0) + 6.0 * Math.Log(3.0);
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_SinglePointSegment_ReturnsZero()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = { { 0.0, 1.0, 0.0 } };
        costFunc.Fit(data);

        // Single point segment [0, 1): {0} => n=1, S=0. Cost = 0
        var cost0 = costFunc.ComputeCost(0, 1);
        Assert.That(cost0, Is.EqualTo(0.0).Within(Tolerance));

        // Single point segment [1, 2): {1} => n=1, S=1. Cost = 0
        var cost1 = costFunc.ComputeCost(1, 2);
         Assert.That(cost1, Is.EqualTo(0.0).Within(Tolerance));
    }

     [Test]
     public void ComputeCost_NearZeroOneValues_TreatedAsZeroOne()
     {
         var costFunc = new BernoulliLikelihoodCostFunction();
         // These values should be clamped to 0, 1, 0, 1 internally by Fit
         double[,] nearData = { { Tolerance / 2.0, 1.0 - Tolerance / 2.0, -Tolerance / 2.0, 0.9999999999 } };
         costFunc.Fit(nearData); // Should result in effective data {0, 1, 0, 1}

         // Cost should be the same as for {0, 1, 0, 1}: n=4, S=2 => 8*log(2)
         var cost = costFunc.ComputeCost();
         var expectedCost = 8.0 * Math.Log(2.0);
         Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
     }

    [Test]
    public void ComputeCost_Multidimensional_CalculatesCorrectCost()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = {
            { 0.0, 1.0, 0.0 }, // Dim 0: n=3, S=1 => Cost = -2*[1*log(1)+2*log(2)-3*log(3)] = -4*log(2)+6*log(3)
            { 1.0, 1.0, 0.0 }, // Dim 1: n=3, S=2 => Cost = -2*[2*log(2)+1*log(1)-3*log(3)] = -4*log(2)+6*log(3)
        };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost();

        var costPerDim = -4.0 * Math.Log(2.0) + 6.0 * Math.Log(3.0);
        var expectedTotalCost = costPerDim * 2.0;
        Assert.That(cost, Is.EqualTo(expectedTotalCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_ChangePointDetection()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        // Segment 1: mostly 0s (p low)
        // Segment 2: mostly 1s (p high)
        double[,] data = { { 0.0, 0.0, 1.0, 0.0, 0.0,   /* Change -> */   1.0, 1.0, 0.0, 1.0, 1.0 } };
        costFunc.Fit(data);

        // Cost segment 1 [0, 5): n=5, S=1
        // Cost = -2*[1*log(1)+4*log(4)-5*log(5)] = -8*log(4)+10*log(5) = -16*log(2)+10*log(5)
        var cost1 = costFunc.ComputeCost(0, 5);
        var expectedCost1 = -16.0 * Math.Log(2.0) + 10.0 * Math.Log(5.0);
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance));

        // Cost segment 2 [5, 10): n=5, S=4
        // Cost = -2*[4*log(4)+1*log(1)-5*log(5)] = -8*log(4)+10*log(5) = -16*log(2)+10*log(5)
        var cost2 = costFunc.ComputeCost(5, 10);
        var expectedCost2 = -16.0 * Math.Log(2.0) + 10.0 * Math.Log(5.0);
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance));

        // Cost across change point [0, 10): n=10, S=5
        // Cost = -2*[5*log(5)+5*log(5)-10*log(10)] = -20*log(5)+20*log(10) = 20*log(10/5) = 20*log(2)
        var costTotal = costFunc.ComputeCost(0, 10);
        var expectedCostTotal = 20.0 * Math.Log(2.0);
        Assert.That(costTotal, Is.EqualTo(expectedCostTotal).Within(Tolerance));

        // Check if splitting is cheaper (Cost1 + Cost2 < CostTotal)
        // 2 * (-16*log(2)+10*log(5)) vs 20*log(2)
        // -32*log(2) + 20*log(5) vs 20*log(2)
        // 20*log(5) vs 52*log(2)
        // 20*1.609 vs 52*0.693
        // 32.18 vs 36.03 -> Splitting is cheaper, good.
        Assert.That(cost1 + cost2, Is.LessThan(costTotal), "Sum of costs for homogeneous segments should be less than the cost of the combined inhomogeneous segment.");
    }

    [Test]
    public void ComputeCost_InvalidIndices_ThrowsArgumentOutOfRangeException()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = { { 0.0, 1.0 } };
        costFunc.Fit(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(0, 3));
    }

    [Test]
    public void ComputeCost_InvalidSegmentLength_ThrowsSegmentLengthException()
    {
        var costFunc = new BernoulliLikelihoodCostFunction();
        double[,] data = { { 0.0, 1.0 } };
        costFunc.Fit(data);

        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(0, 0)); // Length 0
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(1, 1)); // Length 0
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(2, 1)); // Length -1 (indices invalid too)
    }
}