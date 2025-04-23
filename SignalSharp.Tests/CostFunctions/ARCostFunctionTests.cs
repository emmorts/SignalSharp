using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.CostFunctions.Exceptions;

namespace SignalSharp.Tests.CostFunctions;

[TestFixture]
public class ARCostFunctionTests
{
    private const double Tolerance = 1e-6;

    [Test]
    public void Constructor_InvalidOrder_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ARCostFunction(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ARCostFunction(-1));
    }

    [Test]
    public void Fit_NullSignal_ThrowsArgumentNullException()
    {
        var costFunc = new ARCostFunction(1);
        Assert.Throws<ArgumentNullException>(() => costFunc.Fit(null!));
        Assert.Throws<ArgumentNullException>(() => costFunc.Fit((double[,])null!));
    }

    [Test]
    public void Fit_SignalTooShort_ThrowsArgumentException()
    {
        var costFunc = new ARCostFunction(2); // Requires length >= 3
        double[] shortSignal1D = [1.0, 2.0];
        double[,] shortSignal2D =
        {
            { 1.0, 2.0 },
        };

        Assert.Throws<ArgumentException>(() => costFunc.Fit(shortSignal1D));
        Assert.Throws<ArgumentException>(() => costFunc.Fit(shortSignal2D));
    }

    [Test]
    public void Fit_MultidimensionalSignal_ThrowsNotSupportedException()
    {
        var costFunc = new ARCostFunction(1);
        double[,] multiSignal =
        {
            { 1.0, 2.0, 3.0 },
            { 4.0, 5.0, 6.0 },
        };
        Assert.Throws<NotSupportedException>(() => costFunc.Fit(multiSignal));
    }

    [Test]
    public void ComputeCost_BeforeFit_ThrowsUninitializedDataException()
    {
        var costFunc = new ARCostFunction(1);
        Assert.Throws<UninitializedDataException>(() => costFunc.ComputeCost());
    }

    [Test]
    public void ComputeCost_KnownAR1_NoError_ReturnsLowRSS()
    {
        // Data roughly following y[t] = 0.8 * y[t-1] (noise-free for simplicity)
        double[] signal = [1.0, 0.8, 0.64, 0.512, 0.4096];
        var costFunc = new ARCostFunction(order: 1, includeIntercept: false);

        costFunc.Fit(signal);
        var cost = costFunc.ComputeCost();

        // Expect very low RSS as it's a perfect fit (ideally 0, but floating point)
        Assert.That(cost, Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_KnownAR1_WithIntercept_ReturnsInfinityForConstantData()
    {
        // Data roughly following y[t] = 1 + 0.5 * y[t-1] -> stationary mean = 2
        // Using constant data leads to singularity with intercept
        double[] signal = [2.0, 2.0, 2.0, 2.0, 2.0]; // Stationary at mean = 1 / (1-0.5) = 2
        var costFunc = new ARCostFunction(order: 1);

        costFunc.Fit(signal);
        var cost = costFunc.ComputeCost();

        // Expect infinite cost due to singular matrix when fitting AR(p)+intercept to constant data
        Assert.That(double.IsPositiveInfinity(cost), Is.True, "Constant signal with intercept should cause singularity, leading to infinite cost.");
    }

    [Test]
    public void ComputeCost_SegmentTooShort_ThrowsSegmentLengthException()
    {
        double[] signal = [1.0, 2.0, 3.0, 4.0, 5.0];
        var costFunc = new ARCostFunction(order: 2);

        costFunc.Fit(signal);

        // Check segments shorter than minRequiredLength (which is 5 for AR(2)+intercept)
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(0, 1), "Segment 0..1 (len 1) too short");
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(0, 2), "Segment 0..2 (len 2) too short");
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(0, 3), "Segment 0..3 (len 3) too short");
        Assert.Throws<SegmentLengthException>(() => costFunc.ComputeCost(0, 4), "Segment 0..4 (len 4) too short");

        // This should work as length is 5 (>= minRequiredLength 5)
        Assert.DoesNotThrow(() => costFunc.ComputeCost(0, 5), "Segment 0..5 (len 5) should be long enough");
    }

    [Test]
    public void ComputeCost_InvalidIndices_ThrowsArgumentOutOfRangeException()
    {
        double[] signal = [1.0, 2.0, 3.0, 4.0, 5.0];
        var costFunc = new ARCostFunction(order: 1);
        costFunc.Fit(signal);

        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(-1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(0, 6));
        // While end=start gives length 0, which throws SegmentLengthException first,
        // end < start should also be caught if length check passed.
        // Assert.Throws<ArgumentOutOfRangeException>(() => costFunc.ComputeCost(3, 2)); // This combo throws SegmentLengthException
    }

    [Test]
    public void ComputeCost_SimpleSegment_ReturnsCorrectRSS()
    {
        // Simple AR(1) data: y[t] = 0.5 * y[t-1]
        double[] signal = [1.0, 0.5, 0.25, 0.125, 0.0625];
        var costFunc = new ARCostFunction(order: 1, includeIntercept: false);
        costFunc.Fit(signal);

        // Cost for segment [1, 4) => { 0.5, 0.25, 0.125 }
        // Equations:
        // 0.25 = a1 * 0.5
        // 0.125 = a1 * 0.25
        // OLS fit should yield a1 = 0.5.
        // Residuals: (0.25 - 0.5*0.5) = 0, (0.125 - 0.5*0.25) = 0
        // RSS = 0^2 + 0^2 = 0
        var cost = costFunc.ComputeCost(1, 4);
        Assert.That(cost, Is.EqualTo(0.0).Within(Tolerance));

        // Cost for segment [0, 3) => { 1.0, 0.5, 0.25 }
        // Equations:
        // 0.5 = a1 * 1.0
        // 0.25 = a1 * 0.5
        // OLS fit => a1 = 0.5
        // Residuals: (0.5 - 0.5*1.0) = 0, (0.25 - 0.5*0.5) = 0
        // RSS = 0
        var cost2 = costFunc.ComputeCost(0, 3);
        Assert.That(cost2, Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void ComputeCost_ChangeInData_CostShouldDiffer()
    {
        // Segment 1: AR(1) with phi=0.8
        // Segment 2: AR(1) with phi=0.2
        double[] signal =
        [
            // Segment 1 (approx y[t] = 0.8 * y[t-1])
            1.0,
            0.8,
            0.64,
            0.512,
            0.4096,
            // Segment 2 (approx y[t] = 0.2 * y[t-1], starting from last value)
            0.4096 * 0.2, // 0.08192
            0.08192 * 0.2, // 0.016384
            0.016384 * 0.2, // 0.0032768
            0.0032768 * 0.2, // 0.00065536
            0.00065536 * 0.2, // 0.000131072
        ];
        var costFunc = new ARCostFunction(order: 1, includeIntercept: false);
        costFunc.Fit(signal);

        var costSegment1 = costFunc.ComputeCost(0, 5); // { 1.0, 0.8, 0.64, 0.512, 0.4096 }
        var costSegment2 = costFunc.ComputeCost(5, 10); // { 0.08192, ..., 0.000131072 }
        var costAcrossChange = costFunc.ComputeCost(3, 7); // { 0.512, 0.4096, 0.08192, 0.016384 }

        // Expect near-zero cost for pure segments
        Assert.That(costSegment1, Is.EqualTo(0.0).Within(Tolerance), "Cost for segment 1");
        Assert.That(costSegment2, Is.EqualTo(0.0).Within(Tolerance), "Cost for segment 2");

        // Expect significantly higher cost for the segment spanning the change
        Assert.That(costAcrossChange, Is.GreaterThan(costSegment1 + costSegment2 + Tolerance), "Cost across change point");
        // Calculate expected costAcrossChange more accurately if needed, but > 0 is key
        // For {0.512, 0.4096, 0.08192, 0.016384}, equations:
        // 0.4096 = a1*0.512
        // 0.08192 = a1*0.4096
        // 0.016384 = a1*0.08192
        // This doesn't fit a single AR(1) well. OLS will find a compromise a1.
        // Let's check it's non-zero.
        Assert.That(costAcrossChange, Is.GreaterThan(1e-4)); // Check it's substantially non-zero
    }

    [Test]
    public void ComputeCost_ConstantSegment_NoIntercept_ReturnsZeroCost()
    {
        // A constant segment can be modelled by AR(p) without an intercept if coeffs sum to 1.
        // OLS should find this solution with RSS = 0.
        double[] signal = [5.0, 5.0, 5.0, 5.0, 5.0];
        var costFunc = new ARCostFunction(order: 1, includeIntercept: false);
        costFunc.Fit(signal);

        var cost = costFunc.ComputeCost();
        // Expect zero cost as y[t] = 1 * y[t-1] fits perfectly.
        Assert.That(cost, Is.EqualTo(0.0).Within(Tolerance), "Cost should be zero for constant signal without intercept, as AR(1) model y[t]=1*y[t-1] fits.");
    }

    [Test]
    public void ComputeCost_ConstantSegment_WithIntercept_ReturnsInfinity()
    {
        // A constant segment *cannot* be uniquely modelled with an intercept + AR terms -> singular matrix
        double[] signal = [5.0, 5.0, 5.0, 5.0, 5.0];
        var costFunc = new ARCostFunction(order: 1, includeIntercept: true);
        costFunc.Fit(signal);

        var cost = costFunc.ComputeCost();
        // Expect infinite cost due to singularity
        Assert.That(double.IsPositiveInfinity(cost), Is.True, "Cost should be infinite for constant signal with intercept due to singularity.");
    }
}
