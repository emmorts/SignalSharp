using SignalSharp.Detection.PELT;
using SignalSharp.Detection.PELT.Cost;
using SignalSharp.Detection.PELT.Exceptions;
using SignalSharp.Detection.PELT.Models;

namespace SignalSharp.Tests.Detection.PELT;

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

        // Private fields are not directly accessible for testing, so we rely on the Predict method
        // to indirectly verify that the signal and nSamples are updated correctly.
        var breakpoints = algo.Predict(10);
        Assert.That(breakpoints, Is.Empty); // No breakpoints expected for this simple signal
    }

    [Test]
    public void Predict_WithoutFit_ShouldThrowInvalidOperationException()
    {
        var options = new PELTOptions { CostFunction = new L2CostFunction() };
        var algo = new PELTAlgorithm(options);

        Assert.Throws<UninitializedDataException>(() => algo.Predict(10));
    }

    [Test]
    public void Predict_SimpleSignal_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions { CostFunction = new L2CostFunction(), MinSize = 1, Jump = 1 };
        var algo = new PELTAlgorithm(options);
        double[,] signal = { { 1, 1, 1, 5, 5, 5, 1, 1, 1 } };

        algo.Fit(signal);
        var breakpoints = algo.Predict(2);

        int[] expectedBreakpoints = [3, 6];
        Assert.That(expectedBreakpoints, Is.EqualTo(breakpoints));
    }

    [Test]
    public void FitPredict_SimpleSignal_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions { CostFunction = new L2CostFunction(), MinSize = 1, Jump = 1 };
        var algo = new PELTAlgorithm(options);
        double[] signal = [1, 1, 1, 5, 5, 5, 1, 1, 1];

        var breakpoints = algo.FitPredict(signal, 2);

        int[] expectedBreakpoints = [3, 6];
        Assert.That(expectedBreakpoints, Is.EqualTo(breakpoints));
    }
    
    [Test]
    public void FitPredict_LargeSignal_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions { CostFunction = new RBFCostFunction() };
        var algo = new PELTAlgorithm(options);
        var signal = Enumerable
            .Repeat(new double[] { 1, 1, 1, 5, 5, 5, 1, 1, 1, 2, 2, 3, 4, 2, 1 }, 100)
            .SelectMany(x => x)
            .ToArray();
        var signalMatrix = new double[1, signal.Length];
        for (var i = 0; i < signal.Length; i++)
        {
            signalMatrix[0, i] = signal[i];
        }

        var breakpoints = algo.FitPredict(signalMatrix, 10);

        Assert.That(new List<double>(), Is.EqualTo(breakpoints));
    }

    [Test]
    public void Predict_RBFCostFunction_ShouldReturnExpectedBreakpoints()
    {
        var options = new PELTOptions { CostFunction = new RBFCostFunction(), MinSize = 1, Jump = 1 };
        var algo = new PELTAlgorithm(options);
        double[,] signal = { { 1, 1, 1, 5, 5, 5, 1, 1, 1 } };

        algo.Fit(signal);
        var breakpoints = algo.Predict(0.1);

        int[] expectedBreakpoints = [3, 6];
        Assert.That(breakpoints, Is.EqualTo(expectedBreakpoints));
    }
}