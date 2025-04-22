using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

[TestFixture]
public class BinomialLikelihoodCostFunctionTests
{
    private const double Tolerance = 1e-9;

    [Test]
    public void Fit_NullSignal_ThrowsArgumentNullException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        Assert.Throws<ArgumentNullException>(() => costFunc.Fit((double[,])null!));
    }

    [Test]
    public void Fit_IncorrectNumberOfRows_ThrowsArgumentException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] signal1Row = { { 1.0, 2.0, 3.0 } };
        double[,] signal3Rows = { { 1.0, 2.0 }, { 10.0, 10.0 }, { 0.0, 0.0 } };

        Assert.Throws<ArgumentException>(() => costFunc.Fit(signal1Row));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(signal3Rows));
    }

    [Test]
    public void Fit_ValidData_Success()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] validData = {
            { 1.0, 5.0, 10.0 }, // k
            { 10.0, 10.0, 10.0 }, // n
        };
        Assert.DoesNotThrow(() => costFunc.Fit(validData));

        double[,] validDataVaryingN = {
            { 1.0, 5.0, 8.0 },  // k
            { 5.0, 10.0, 15.0 }, // n
        };
         Assert.DoesNotThrow(() => costFunc.Fit(validDataVaryingN));

        double[,] validDataBoundary = {
            { 0.0, 10.0 }, // k = 0, k = n
            { 10.0, 10.0 }, // n
        };
         Assert.DoesNotThrow(() => costFunc.Fit(validDataBoundary));
    }

    [Test]
    public void Fit_InvalidData_KLessThanZero_ThrowsArgumentException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] invalidData = { { -1.0 }, { 10.0 } };
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidData));
    }

    [Test]
    public void Fit_InvalidData_NLessThanOne_ThrowsArgumentException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] invalidDataNZero = { { 0.0 }, { 0.0 } };
        double[,] invalidDataNNeg = { { 0.0 }, { -5.0 } };

        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidDataNZero));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidDataNNeg));
    }

    [Test]
    public void Fit_InvalidData_KGreaterThanN_ThrowsArgumentException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] invalidData = { { 11.0 }, { 10.0 } };
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidData));
    }

    [Test]
    public void Fit_InvalidData_NonIntegerValues_ThrowsArgumentException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] invalidDataK = { { 5.5 }, { 10.0 } };
        double[,] invalidDataN = { { 5.0 }, { 10.5 } };

        // Values slightly off integer should still work due to tolerance
        double[,] nearIntData = { { 5.0000000001 }, { 10.00000000004 } };
         Assert.DoesNotThrow(() => costFunc.Fit(nearIntData));

        // Values far from integer should throw
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidDataK));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(invalidDataN));
    }

    [Test]
    public void Fit_InvalidData_NaNOrInfinity_ThrowsArgumentException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] nanDataK = { { double.NaN }, { 10.0 } };
        double[,] nanDataN = { { 5.0 }, { double.NaN } };
        double[,] infDataK = { { double.PositiveInfinity }, { 10.0 } };
        double[,] infDataN = { { 5.0 }, { double.PositiveInfinity } };

        Assert.Throws<ArgumentException>(() => costFunc.Fit(nanDataK));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(nanDataN));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(infDataK));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(infDataN));
    }

    [Test]
    public void Fit_OneDimensionalSignal_ThrowsNotSupportedException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[] signal1D = [1.0, 2.0, 3.0];
        Assert.Throws<NotSupportedException>(() => costFunc.Fit(signal1D));
    }

    [Test]
    public void ComputeCost_BeforeFit_ThrowsUninitializedDataException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        Assert.Throws<UninitializedDataException>(() => costFunc.ComputeCost());
    }

    [Test]
    public void ComputeCost_EmptyData_ReturnsZero()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        var emptyData = new double[2, 0];
        costFunc.Fit(emptyData);
        var cost = costFunc.ComputeCost();
        Assert.That(cost, Is.EqualTo(0.0));
    }

    [Test]
    public void ComputeCost_FullSegment_ValidData()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] data = {
            { 1.0, 2.0, 8.0, 9.0 }, // k: Sum K = 20
            { 10.0, 10.0, 10.0, 10.0 }, // n: Sum N = 40
        };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(); // Full segment [0, 4)

        // Expected: K=20, N=40. p_hat = 0.5
        // Cost = -[ 20*log(20) + (40-20)*log(40-20) - 40*log(40) ]
        // Cost = -[ 20*log(20) + 20*log(20) - 40*log(40) ]
        // Cost = -[ 40*log(20) - 40*log(40) ]
        // Cost = 40 * [ log(40) - log(20) ] = 40 * log(40/20) = 40 * log(2)
        var expectedCost = 40.0 * Math.Log(2.0);
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

     [Test]
    public void ComputeCost_SubsetSegment_ValidData()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] data = {
            { 1.0, 2.0, 8.0, 9.0 }, // k
            { 10.0, 10.0, 10.0, 10.0 }, // n
        };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(1, 3); // Segment [1, 3) -> indices 1, 2

        // Expected: k = {2, 8}, n = {10, 10}. K=10, N=20. p_hat = 0.5
        // Cost = -[ 10*log(10) + (20-10)*log(20-10) - 20*log(20) ]
        // Cost = -[ 10*log(10) + 10*log(10) - 20*log(20) ]
        // Cost = -[ 20*log(10) - 20*log(20) ]
        // Cost = 20 * [ log(20) - log(10) ] = 20 * log(20/10) = 20 * log(2)
        var expectedCost = 20.0 * Math.Log(2.0);
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

     [Test]
    public void ComputeCost_SinglePointSegment()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] data = {
            { 3.0 }, // k = 3
            { 10.0 }, // n = 10
        };
        costFunc.Fit(data);
        var cost = costFunc.ComputeCost(0, 1); // Segment [0, 1)

        // Expected: K=3, N=10. p_hat = 0.3
        // Cost = -[ 3*log(3) + (10-3)*log(10-3) - 10*log(10) ]
        // Cost = -[ 3*log(3) + 7*log(7) - 10*log(10) ]
        var expectedCost = -(3.0 * Math.Log(3.0) + 7.0 * Math.Log(7.0) - 10.0 * Math.Log(10.0));
        Assert.That(cost, Is.EqualTo(expectedCost).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_Segment_AllSuccesses_ReturnsZero()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] data = {
            { 10.0, 10.0, 5.0 }, // k
            { 10.0, 10.0, 5.0 }, // n
        };
        costFunc.Fit(data);

        var costFull = costFunc.ComputeCost(); // K=25, N=25 -> p_hat=1
        Assert.That(costFull, Is.EqualTo(0.0).Within(Tolerance));

        var costPartial = costFunc.ComputeCost(0, 2); // K=20, N=20 -> p_hat=1
        Assert.That(costPartial, Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_Segment_AllFailures_ReturnsZero()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] data = {
            { 0.0, 0.0, 0.0 }, // k
            { 10.0, 15.0, 5.0 }, // n
        };
        costFunc.Fit(data);

        var costFull = costFunc.ComputeCost(); // K=0, N=30 -> p_hat=0
        Assert.That(costFull, Is.EqualTo(0.0).Within(Tolerance));

        var costPartial = costFunc.ComputeCost(1, 3); // K=0, N=20 -> p_hat=0
        Assert.That(costPartial, Is.EqualTo(0.0).Within(Tolerance));
    }

     [Test]
     public void ComputeCost_ChangePointDetection()
     {
        var costFunc = new BinomialLikelihoodCostFunction();
        // Segment 1: low success rate (p approx 0.1)
        // Segment 2: high success rate (p approx 0.9)
        double[,] data = {
            { 1.0, 1.0, 2.0, 1.0, 1.0,   /* Change -> */   9.0, 8.0, 9.0, 10.0, 9.0 }, // k
            { 10.0, 10.0, 10.0, 10.0, 10.0, /*        */   10.0, 10.0, 10.0, 10.0, 10.0 }, // n
        };
        costFunc.Fit(data);

        // Cost segment 1 [0, 5): K=6, N=50, p_hat=0.12
        var cost1 = costFunc.ComputeCost(0, 5);
        var expectedCost1 = -(6.0 * Math.Log(6.0) + 44.0 * Math.Log(44.0) - 50.0 * Math.Log(50.0));
        Assert.That(cost1, Is.EqualTo(expectedCost1).Within(Tolerance));

        // Cost segment 2 [5, 10): K=45, N=50, p_hat=0.9
        var cost2 = costFunc.ComputeCost(5, 10);
        var expectedCost2 = -(45.0 * Math.Log(45.0) + 5.0 * Math.Log(5.0) - 50.0 * Math.Log(50.0));
        Assert.That(cost2, Is.EqualTo(expectedCost2).Within(Tolerance));

        // Cost across change point [0, 10): K=51, N=100, p_hat=0.51
        var costTotal = costFunc.ComputeCost(0, 10);
        var expectedCostTotal = -(51.0 * Math.Log(51.0) + 49.0 * Math.Log(49.0) - 100.0 * Math.Log(100.0));
        Assert.That(costTotal, Is.EqualTo(expectedCostTotal).Within(Tolerance));

        // Check if splitting is cheaper (Cost1 + Cost2 < CostTotal)
        // This is fundamental for change point detection algorithms like PELT
        Assert.That(cost1 + cost2, Is.LessThan(costTotal), "Sum of costs for homogeneous segments should be less than the cost of the combined inhomogeneous segment.");
     }

    [Test]
    public void ComputeCost_InvalidIndices_ThrowsArgumentOutOfRangeException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] data = { { 1.0 }, { 10.0 } };
        costFunc.Fit(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(0, 2));
    }

    [Test]
    public void ComputeCost_InvalidSegmentLength_ThrowsSegmentLengthException()
    {
        var costFunc = new BinomialLikelihoodCostFunction();
        double[,] data = { { 1.0, 2.0 }, { 10.0, 10.0 } };
        costFunc.Fit(data);

        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(0, 0)); // Length 0
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(1, 1)); // Length 0
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(2, 1)); // Length -1 (indices invalid too)
    }
}