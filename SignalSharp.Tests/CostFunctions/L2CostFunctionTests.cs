using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

public class L2CostFunctionTests
{
    [Test]
    public void ComputeCost_SimpleCase()
    {
        double[,] data = { { 1.0, 2.0, 3.0 } };

        var l2CostFunction = new L2CostFunction();
        var cost = l2CostFunction.Fit(data).ComputeCost();

        const double expected = 2.0;
        
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_LargerCase()
    {
        double[,] data = { { 1.0, 1.5, 2.0, 2.5, 3.0 } };

        var l2CostFunction = new L2CostFunction();
        var cost = l2CostFunction.Fit(data).ComputeCost();

        const double expected = 2.5;
        
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_Subset()
    {
        double[,] data = { { 1.0, 1.5, 2.0, 2.5, 3.0 } };

        var l2CostFunction = new L2CostFunction();
        var cost = l2CostFunction.Fit(data).ComputeCost(1, 4);

        // Expected result for subset [1.5, 2.0, 2.5]
        const double expected = 0.5;
        
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_PartialComputation()
    {
        double[,] data = { { 1.0, 1.5, 2.0, 2.5, 3.0 } };

        var l2CostFunction = new L2CostFunction().Fit(data);
        
        var costOneToThree = l2CostFunction.ComputeCost(1, 3);
        const double expectedCostOneToThree = 0.125;
        Assert.That(costOneToThree, Is.EqualTo(expectedCostOneToThree).Within(1e-6));
        
        var costZeroToFour = l2CostFunction.ComputeCost(0, 4);
        const double expectedCostZeroToFour = 1.25;
        Assert.That(costZeroToFour, Is.EqualTo(expectedCostZeroToFour).Within(1e-6));
    }

    [Test]
    public void ComputeCost_SinglePoint()
    {
        double[,] data = { { 1.0 } };

        var l2CostFunction = new L2CostFunction();
        var cost = l2CostFunction.Fit(data).ComputeCost();

        // For a single point, the cost should be zero as there is no deviation
        Assert.That(cost, Is.EqualTo(0));
    }

    [Test]
    public void ComputeCost_NoPoints()
    {
        double[,] data = { {  } };

        var l2CostFunction = new L2CostFunction();
        var cost = l2CostFunction.Fit(data).ComputeCost();

        Assert.That(cost, Is.EqualTo(0));
    }

    [Test]
    public void ComputeCost_InvalidStart()
    {
        double[,] data = { { 1, 2, 3, 4, 5 } };

        var l2CostFunction = new L2CostFunction();

        Assert.Throws<ArgumentOutOfRangeException>(() => l2CostFunction.Fit(data).ComputeCost(-4, null));
    }

    [Test]
    public void ComputeCost_InvalidEnd()
    {
        double[,] data = { { 1, 2, 3, 4, 5 } };

        var l2CostFunction = new L2CostFunction();

        Assert.Throws<ArgumentOutOfRangeException>(() => l2CostFunction.Fit(data).ComputeCost(0, 18));
    }

    [Test]
    public void ComputeCost_InvalidSegment()
    {
        double[,] data = { { 1, 2, 3, 4, 5 } };

        var l2CostFunction = new L2CostFunction();

        Assert.Throws<SegmentLengthException>(() => l2CostFunction.Fit(data).ComputeCost(0, 0));
    }
}