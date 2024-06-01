using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

public class L1CostFunctionTests
{
    [Test]
    public void ComputeCost_SimpleCase()
    {
        double[,] data = { { 1.0, 2.0, 3.0 } };

        var l1CostFunction = new L1CostFunction();
        var cost = l1CostFunction.Fit(data).ComputeCost();

        const double expected = 2.0;
        
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_LargerCase()
    {
        double[,] data = { { 1.0, 1.5, 2.0, 2.5, 3.0 } };

        var l1CostFunction = new L1CostFunction();
        var cost = l1CostFunction.Fit(data).ComputeCost();

        const double expected = 3.0;
        
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_Subset()
    {
        double[,] data = { { 1.0, 1.5, 2.0, 2.5, 3.0 } };

        var l1CostFunction = new L1CostFunction();
        var cost = l1CostFunction.Fit(data).ComputeCost(1, 4);

        // Expected result for subset [1.5, 2.0, 2.5]
        const double expected = 1.0;
        
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_PartialComputation()
    {
        double[,] data = { { 1.0, 1.5, 2.0, 2.5, 3.0 } };

        var l1CostFunction = new L1CostFunction().Fit(data);
        
        var costOneToThree = l1CostFunction.ComputeCost(1, 3);
        const double expectedCostOneToThree = 0.5;
        Assert.That(costOneToThree, Is.EqualTo(expectedCostOneToThree).Within(1e-6));
        
        var costZeroToFour = l1CostFunction.ComputeCost(0, 4);
        const double expectedCostZeroToFour = 2.0;
        Assert.That(costZeroToFour, Is.EqualTo(expectedCostZeroToFour).Within(1e-6));
    }

    [Test]
    public void ComputeCost_SinglePoint()
    {
        double[,] data = { { 1.0 } };

        var l1CostFunction = new L1CostFunction();
        var cost = l1CostFunction.Fit(data).ComputeCost();

        // For a single point, the cost should be zero as there is no deviation
        Assert.That(cost, Is.EqualTo(0));
    }

    [Test]
    public void ComputeCost_NoPoints()
    {
        double[,] data = { {  } };

        var l1CostFunction = new L1CostFunction();
        var cost = l1CostFunction.Fit(data).ComputeCost();

        Assert.That(cost, Is.EqualTo(0));
    }

    [Test]
    public void ComputeCost_InvalidStart()
    {
        double[,] data = { { 1, 2, 3, 4, 5 } };

        var l1CostFunction = new L1CostFunction();

        Assert.Throws<ArgumentOutOfRangeException>(() => l1CostFunction.Fit(data).ComputeCost(-4));
    }

    [Test]
    public void ComputeCost_InvalidEnd()
    {
        double[,] data = { { 1, 2, 3, 4, 5 } };

        var l1CostFunction = new L1CostFunction();

        Assert.Throws<ArgumentOutOfRangeException>(() => l1CostFunction.Fit(data).ComputeCost(0, 18));
    }

    [Test]
    public void ComputeCost_InvalidSegment()
    {
        double[,] data = { { 1, 2, 3, 4, 5 } };

        var l1CostFunction = new L1CostFunction();

        Assert.Throws<SegmentLengthException>(() => l1CostFunction.Fit(data).ComputeCost(0, 0));
    }
}