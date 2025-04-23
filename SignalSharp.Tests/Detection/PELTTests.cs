using SignalSharp.Common.Exceptions;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.Detection.PELT;

namespace SignalSharp.Tests.Detection;

[TestFixture]
public class PELTAlgorithmTests
{
    [Test]
    public void Fit_ValidSignal_ShouldUpdateSignalAndNSamples()
    {
        var options = new PELTOptions { CostFunction = new L2CostFunction() };
        var algo = new PELTAlgorithm(options);
        double[] signal = [1, 2, 3, 4, 5];

        algo.Fit(signal);

        // Private fields are not directly accessible for testing, so we rely on the Detect method
        // to indirectly verify that the signal and nSamples are updated correctly.
        var breakpoints = algo.Detect(10);
        Assert.That(breakpoints, Is.Empty); // No breakpoints expected for this simple signal
    }

    [Test]
    public void Detect_WithoutFit_ShouldThrowInvalidOperationException()
    {
        var options = new PELTOptions { CostFunction = new L2CostFunction() };
        var algo = new PELTAlgorithm(options);

        Assert.Throws<UninitializedDataException>(() => algo.Detect(10));
    }

    [Test]
    public void Detect_SimpleSignal_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions
        {
            CostFunction = new L2CostFunction(),
            MinSize = 1,
            Jump = 1,
        };
        var algo = new PELTAlgorithm(options);
        double[,] signal =
        {
            { 1, 1, 1, 5, 5, 5, 1, 1, 1 },
        };

        algo.Fit(signal);
        var breakpoints = algo.Detect(2);

        int[] expectedBreakpoints = [3, 6];
        Assert.That(expectedBreakpoints, Is.EqualTo(breakpoints));
    }

    [Test]
    public void FitDetect_SimpleSignal_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions
        {
            CostFunction = new L2CostFunction(),
            MinSize = 1,
            Jump = 1,
        };
        var algo = new PELTAlgorithm(options);
        double[] signal = [1, 1, 1, 5, 5, 5, 1, 1, 1];

        var breakpoints = algo.FitAndDetect(signal, 2);

        int[] expectedBreakpoints = [3, 6];
        Assert.That(expectedBreakpoints, Is.EqualTo(breakpoints));
    }

    [Test]
    public void FitDetect_LargeSignal_RBFCost_Exact_Jump1_ShouldReturnNoBreakpoints()
    {
        var options = new PELTOptions { CostFunction = new RBFCostFunction(), Jump = 1 };
        var algo = new PELTAlgorithm(options);
        var pattern = new double[] { 1, 1, 1, 5, 5, 5, 1, 1, 1, 2, 2, 3, 4, 2, 1 };
        var signal = Enumerable.Repeat(pattern, 100).SelectMany(x => x).ToArray();

        var signalMatrix = new double[1, signal.Length];
        for (var i = 0; i < signal.Length; i++)
        {
            signalMatrix[0, i] = signal[i];
        }

        var breakpoints = algo.FitAndDetect(signalMatrix, 10.0);

        Assert.That(breakpoints, Is.Empty);
    }

    [Test]
    public void FitDetect_LargeSignal_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions
        {
            CostFunction = new RBFCostFunction(),
            Jump = 5,
            MinSize = 2,
        };
        var algo = new PELTAlgorithm(options);
        var signal = Enumerable.Repeat(new double[] { 1, 1, 1, 5, 5, 5, 1, 1, 1, 2, 2, 3, 4, 2, 1 }, 100).SelectMany(x => x).ToArray();
        var signalMatrix = new double[1, signal.Length];
        for (var i = 0; i < signal.Length; i++)
        {
            signalMatrix[0, i] = signal[i];
        }

        var breakpoints = algo.FitAndDetect(signalMatrix, 10);

        int[] expectedApproximateBreakpoints = [1496, 1498];

        Assert.That(expectedApproximateBreakpoints, Is.EqualTo(breakpoints));
    }

    [Test]
    public void Detect_RBFCostFunction_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions
        {
            CostFunction = new RBFCostFunction(),
            MinSize = 1,
            Jump = 1,
        };
        var algo = new PELTAlgorithm(options);
        double[,] signal =
        {
            { 1, 1, 1, 5, 5, 5, 1, 1, 1 },
        };

        algo.Fit(signal);
        var breakpoints = algo.Detect(0.1);

        int[] expectedBreakpoints = [3, 6];
        Assert.That(breakpoints, Is.EqualTo(expectedBreakpoints));
    }
}
