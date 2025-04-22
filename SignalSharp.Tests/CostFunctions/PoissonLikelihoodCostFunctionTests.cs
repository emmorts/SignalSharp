using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

[TestFixture]
public class PoissonLikelihoodCostFunctionTests
{
    private const double Tolerance = 1e-9;

    [Test]
    public void Fit_NullSignalMatrix_ThrowsArgumentNullException()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        Assert.Throws<ArgumentNullException>(() => costFunc.Fit((double[,])null!));
    }

    [Test]
    public void Fit_NullSignalArray_ThrowsArgumentNullException()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        
        Assert.Throws<ArgumentNullException>(() => costFunc.Fit((double[])null!));
    }

    [Test]
    public void Fit_InvalidValues_NegativeOutsideTolerance_ThrowsArgumentException()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] invalidData1 = { { 5.0, 8.0, -1.0 } }; // Contains -1.0
        double[,] invalidData2 = { { 5.0, 8.0, -Tolerance * 2 } }; // Negative, outside tolerance

        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidData1));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidData2));
    }

    [Test]
    public void Fit_NearZeroNegativeValues_InsideTolerance_Success()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] nearZeroData = { { 5.0, 8.0, -Tolerance / 2.0, 0.0 } };

        Assert.DoesNotThrow(() => costFunc.Fit(nearZeroData));
    }

    [Test]
    public void Fit_NonNegativeValues_Success()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] validData = { { 0.0, 5.0, 10.0, 0.0, 3.0 } };
        Assert.DoesNotThrow(() => costFunc.Fit(validData));

        double[,] validFloatData = { { 0.0, 5.0000000001, 9.9999999999 } };
        Assert.DoesNotThrow(() => costFunc.Fit(validFloatData));
    }

    [Test]
    public void Fit_EmptyData_Success()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
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
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] validMultiData = {
            { 0.0, 10.0, 5.0 },
            { 2.0, 1.0, 0.0 },
        };
        Assert.DoesNotThrow(() => costFunc.Fit(validMultiData));
    }

    [Test]
    public void Fit_OneDimensionalData_Success()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[] valid1DData = [0.0, 5.0, 10.0, 0.0, 3.0];
        Assert.DoesNotThrow(() => costFunc.Fit(valid1DData));
    }

    [Test]
    public void ComputeCost_BeforeFit_ThrowsUninitializedDataException()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        Assert.Throws<UninitializedDataException>(() => costFunc.ComputeCost());
    }

    [Test]
    public void ComputeCost_AfterFittingEmptyData_ReturnsZero()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
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
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = { { 0.0, 0.0, 0.0, 0.0 } };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(); // S=0, n=4
        Assert.That(cost, Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_FullSegment_ConstantNonZero_CalculatesCorrectCost()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = { { 5.0, 5.0, 5.0 } }; // n=3, S=15
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost();

        // Expected cost = 2 * [ S - S*log(S) + S*log(n) ]
        // S=15, n=3
        // Cost = 2 * [ 15 - 15*log(15) + 15*log(3) ]
        // Cost = 30 * [ 1 - log(15) + log(3) ]
        // Cost = 30 * [ 1 - log(15/3) ] = 30 * [ 1 - log(5) ]
        var expectedCost = 30.0 * (1.0 - Math.Log(5.0));
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_FullSegment_Mixed_CalculatesCorrectCost()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = { { 2.0, 3.0, 4.0, 1.0 } }; // n=4, S=10
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost();

        // Expected cost = 2 * [ S - S*log(S) + S*log(n) ]
        // S=10, n=4
        // Cost = 2 * [ 10 - 10*log(10) + 10*log(4) ]
        // Cost = 20 * [ 1 - log(10) + log(4) ]
        // Cost = 20 * [ 1 - log(10/4) ] = 20 * [ 1 - log(2.5) ]
        var expectedCost = 20.0 * (1.0 - Math.Log(2.5));
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_SubsetSegment_Mixed_CalculatesCorrectCost()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = { { 2.0, 3.0, 4.0, 1.0, 5.0 } };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(1, 4); // Segment [1, 4): {3, 4, 1} => n=3, S=8

        // Expected cost = 2 * [ S - S*log(S) + S*log(n) ]
        // S=8, n=3
        // Cost = 2 * [ 8 - 8*log(8) + 8*log(3) ]
        // Cost = 16 * [ 1 - log(8) + log(3) ]
        // Cost = 16 * [ 1 - log(8.0/3.0) ]
        var expectedCost = 16.0 * (1.0 - Math.Log(8.0 / 3.0));
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_SinglePointSegment_CalculatesCorrectCost()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = { { 0.0, 5.0, 0.0 } };
        costFunc.Fit(data);

        // Single point segment [0, 1): {0} => n=1, S=0. Cost = 0
        var cost0 = costFunc.ComputeCost(0, 1);
        Assert.That(cost0, Is.EqualTo(0.0).Within(Tolerance));

        // Single point segment [1, 2): {5} => n=1, S=5.
        // Cost = 2 * [ 5 - 5*log(5) + 5*log(1) ] = 10 * [1 - log(5) + 0] = 10 * (1 - log(5))
        var cost1 = costFunc.ComputeCost(1, 2);
        var expectedCost1 = 10.0 * (1.0 - Math.Log(5.0));
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance));
    }

     [Test]
     public void ComputeCost_NearZeroValues_TreatedAsZero()
     {
         var costFunc = new PoissonLikelihoodCostFunction();
         // These values should be clamped to 0 internally by Fit
         double[,] nearData = { { 5.0, Tolerance / 2.0, 2.0, -Tolerance / 2.0 } };
         costFunc.Fit(nearData); // Should result in effective data {5, 0, 2, 0}

         // Cost for segment [0, 4): n=4, S=7
         // Cost = 2 * [ 7 - 7*log(7) + 7*log(4) ] = 14 * [ 1 - log(7/4) ] = 14 * [ 1 - log(1.75) ]
         var cost = costFunc.ComputeCost();
         var expectedCost = 14.0 * (1.0 - Math.Log(1.75));
         Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));

         // Cost for segment [1, 2): { ~0 } => n=1, S=0. Cost = 0
         var costNearZeroSeg = costFunc.ComputeCost(1, 2);
         Assert.That(costNearZeroSeg, Is.EqualTo(0.0).Within(Tolerance));
     }

    [Test]
    public void ComputeCost_Multidimensional_CalculatesCorrectCost()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = {
            { 2.0, 3.0 }, // Dim 0: n=2, S=5 => Cost0 = 10*[1-log(5/2)] = 10*[1-log(2.5)]
            { 4.0, 1.0 }, // Dim 1: n=2, S=5 => Cost1 = 10*[1-log(5/2)] = 10*[1-log(2.5)]
        };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost();

        var costPerDim = 10.0 * (1.0 - Math.Log(2.5));
        var expectedTotalCost = costPerDim * 2.0;
        Assert.That(cost, Is.EqualTo(expectedTotalCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_ChangePointDetection()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        // Segment 1: low rate (mean approx 2)
        // Segment 2: high rate (mean approx 8)
        double[,] data = { { 1.0, 3.0, 2.0, 2.0,   /* Change -> */   7.0, 9.0, 8.0, 8.0 } };
        costFunc.Fit(data);

        // Cost segment 1 [0, 4): n=4, S=8
        // Cost1 = 16 * [ 1 - log(8/4) ] = 16 * [ 1 - log(2) ]
        var cost1 = costFunc.ComputeCost(0, 4);
        var expectedCost1 = 16.0 * (1.0 - Math.Log(2.0));
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance));

        // Cost segment 2 [4, 8): n=4, S=32
        // Cost2 = 64 * [ 1 - log(32/4) ] = 64 * [ 1 - log(8) ]
        var cost2 = costFunc.ComputeCost(4, 8);
        var expectedCost2 = 64.0 * (1.0 - Math.Log(8.0));
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance));

        // Cost across change point [0, 8): n=8, S=40
        // CostTotal = 80 * [ 1 - log(40/8) ] = 80 * [ 1 - log(5) ]
        var costTotal = costFunc.ComputeCost(0, 8);
        var expectedCostTotal = 80.0 * (1.0 - Math.Log(5.0));
        Assert.That(costTotal, Is.EqualTo(expectedCostTotal).Within(Tolerance));

        // Check if splitting is cheaper (Cost1 + Cost2 < CostTotal)
        // 16*(1-log(2)) + 64*(1-log(8)) vs 80*(1-log(5))
        // 16 - 16*log(2) + 64 - 64*log(8) vs 80 - 80*log(5)
        // 80 - 16*log(2) - 64*log(2^3) vs 80 - 80*log(5)
        // 80 - 16*log(2) - 192*log(2) vs 80 - 80*log(5)
        // 80 - 208*log(2) vs 80 - 80*log(5)
        // -208*log(2) vs -80*log(5)
        // 208*log(2) vs 80*log(5)  (Multiply by -1, flip inequality)
        // 208*0.693 vs 80*1.609
        // 144.1 vs 128.7 -> Splitting IS cheaper. Good.
        Assert.That(cost1 + cost2, Is.LessThan(costTotal), "Sum of costs for homogeneous segments should be less than the cost of the combined inhomogeneous segment.");
    }

    [Test]
    public void ComputeCost_InvalidIndices_ThrowsArgumentOutOfRangeException()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = { { 0.0, 1.0 } };
        costFunc.Fit(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(2, 1)); // Length -1 (indices invalid too)
    }

    [Test]
    public void ComputeCost_InvalidSegmentLength_ThrowsSegmentLengthException()
    {
        var costFunc = new PoissonLikelihoodCostFunction();
        double[,] data = { { 0.0, 1.0 } };
        costFunc.Fit(data);

        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(0, 0)); // Length 0
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(1, 1)); // Length 0
    }
}