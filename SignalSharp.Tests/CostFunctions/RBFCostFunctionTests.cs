using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

[TestFixture]
public class RBFCostFunctionTests
{
    [Test]
    public void ComputeCost_SimpleCase()
    {
        double[,] data =
        {
            { 1.0, 2.0, 3.0 },
        };
        const double gamma = 1.0;

        var rbfCostFunction = new RBFCostFunction(gamma);
        var cost = rbfCostFunction.Fit(data).ComputeCost();

        const double expected = 1.49728365;

        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_LargerCase()
    {
        double[,] data =
        {
            { 1.0, 1.5, 2.0, 2.5, 3.0 },
        };
        const double gamma = 0.5;

        var rbfCostFunction = new RBFCostFunction(gamma);
        var cost = rbfCostFunction.Fit(data).ComputeCost();

        const double expected = 1.5463120770;

        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_Subset()
    {
        double[,] data =
        {
            { 1.0, 1.5, 2.0, 2.5, 3.0 },
        };
        const double gamma = 0.5;

        var rbfCostFunction = new RBFCostFunction(gamma);
        var cost = rbfCostFunction.Fit(data).ComputeCost(1, 4);

        // Expected result for subset [1.5, 2.0, 2.5]
        const double expected = 0.41898369007;
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_SinglePoint()
    {
        double[,] data =
        {
            { 1.0 },
        };
        const double gamma = 0.5;

        var rbfCostFunction = new RBFCostFunction(gamma);
        var cost = rbfCostFunction.Fit(data).ComputeCost();

        // For a single point, there are no pairs to calculate the RBF, so the cost should be zero
        Assert.That(cost, Is.EqualTo(0));
    }

    [Test]
    public void ComputeCost_NoPoints()
    {
        double[,] data =
        {
            { },
        };
        const double gamma = 0.5;

        var rbfCostFunction = new RBFCostFunction(gamma);

        Assert.Throws<SegmentLengthException>(() => rbfCostFunction.Fit(data).ComputeCost(), "Segment length must be at least 1.");
    }

    [Test]
    public void ComputeCost_DefaultGamma()
    {
        double[,] data =
        {
            { 1.0, 2.0, 3.0 },
        };

        var rbfCostFunction = new RBFCostFunction();
        var cost = rbfCostFunction.Fit(data).ComputeCost();

        // Expected result based on default gamma calculation (using median heuristic)
        // Median of pairwise distances [1.0, 4.0, 1.0] is 1.0
        // Gamma = 1 / median = 1 / 1 = 1
        const double expected = 1.497283652;

        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_IterativeComputationUsingPartialSums()
    {
        double[,] data =
        {
            { 1.0, 1.5, 2.0, 2.5, 3.0 },
        };
        const double gamma = 0.5;

        var rbfCostFunction = new RBFCostFunction(gamma).Fit(data);

        var costOneToThree = rbfCostFunction.ComputeCost(1, 3);
        const double expectedCostOneToThree = 0.11750309741540454;
        Assert.That(costOneToThree, Is.EqualTo(expectedCostOneToThree).Within(1e-6));

        var costOfZeroToFour = rbfCostFunction.ComputeCost(0, 4);
        const double expectedCostZeroToFour = 0.90739775273129819;
        Assert.That(costOfZeroToFour, Is.EqualTo(expectedCostZeroToFour).Within(1e-6));
    }

    [Test]
    public void ComputeCost_MultidimensionalData()
    {
        double[,] data =
        {
            { 1.0, 2.0, 3.0 },
            { 4.0, 5.0, 6.0 },
        };
        const double gamma = 1.0;

        var rbfCostFunction = new RBFCostFunction(gamma);
        var cost = rbfCostFunction.Fit(data).ComputeCost();

        const double expected = 2.99456730;
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_MultidimensionalSubset()
    {
        double[,] data =
        {
            { 1.0, 2.0, 3.0, 4.0, 5.0 },
            { 6.0, 7.0, 8.0, 9.0, 10.0 },
        };
        const double gamma = 0.5;

        var rbfCostFunction = new RBFCostFunction(gamma);
        var cost = rbfCostFunction.Fit(data).ComputeCost(1, 4);

        // Expected result for subset considering multidimensional data
        const double expected = 2.20213786;
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }

    [Test]
    public void ComputeCost_VaryingGammaValues()
    {
        double[,] data =
        {
            { 1.0, 2.0, 3.0 },
        };

        double[] gammaValues = [0.1, 1.0, 10.0];
        double[] expectedCosts = [0.34667007, 1.49728365, 1.99993946];

        for (var i = 0; i < gammaValues.Length; i++)
        {
            var rbfCostFunction = new RBFCostFunction(gammaValues[i]);
            var cost = rbfCostFunction.Fit(data).ComputeCost();

            Assert.That(cost, Is.EqualTo(expectedCosts[i]).Within(1e-6));
        }
    }

    [Test]
    public void ComputeCost_DefaultGamma_Multidimensional()
    {
        double[,] data =
        {
            { 1.0, 2.0, 3.0 },
            { 4.0, 5.0, 6.0 },
        };

        var rbfCostFunction = new RBFCostFunction();
        var cost = rbfCostFunction.Fit(data).ComputeCost();

        const double expected = 2.99456730;
        Assert.That(cost, Is.EqualTo(expected).Within(1e-6));
    }
}
