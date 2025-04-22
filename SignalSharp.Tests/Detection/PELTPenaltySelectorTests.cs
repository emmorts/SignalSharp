using Moq;
using SignalSharp.CostFunctions.Cost;
using SignalSharp.Detection.PELT;
using SignalSharp.Detection.PELT.Exceptions;
using SignalSharp.CostFunctions.Exceptions;
using SignalSharp.Utilities;

namespace SignalSharp.Tests.Detection;

[TestFixture]
public class PELTPenaltySelectorTests
{
    private Mock<IPELTAlgorithm> _mockPeltAlgorithm = null!;
    private Mock<ILikelihoodCostFunction> _mockLikelihoodCostFn = null!;
    private Mock<IPELTCostFunction> _mockNonLikelihoodCostFn = null!;
    private PELTOptions _testOptions = null!;
    private const int SignalLength = 100;
    private static readonly double[] TestSignal = Enumerable.Range(0, SignalLength).Select(i => (double)i).ToArray();

    [SetUp]
    public void SetUp()
    {
        _mockPeltAlgorithm = new Mock<IPELTAlgorithm>();
        _mockLikelihoodCostFn = new Mock<ILikelihoodCostFunction>();
        _mockNonLikelihoodCostFn = new Mock<IPELTCostFunction>();

        _mockLikelihoodCostFn.Setup(c => c.SupportsInformationCriteria).Returns(true);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(It.IsAny<int>())).Returns(2);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int start, int end) => (end - start) * 1.0);

        _testOptions = new PELTOptions
        {
            CostFunction = _mockLikelihoodCostFn.Object,
            MinSize = 2,
            Jump = 1
        };

        _mockPeltAlgorithm.Setup(a => a.Options).Returns(_testOptions);
        _mockPeltAlgorithm.Setup(a => a.Fit(It.IsAny<double[]>())).Returns(_mockPeltAlgorithm.Object);
        _mockPeltAlgorithm.Setup(a => a.Fit(It.IsAny<double[,]>())).Returns(_mockPeltAlgorithm.Object);
    }

    private PELTPenaltySelector CreateSelector(IPELTCostFunction costFunction)
    {
        var options = new PELTOptions { CostFunction = costFunction, MinSize = 2, Jump = 1 };
        _mockPeltAlgorithm.Setup(a => a.Options).Returns(options);
        return new PELTPenaltySelector(_mockPeltAlgorithm.Object);
    }

    [Test]
    public void Constructor_NullAlgorithm_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PELTPenaltySelector(null!));
    }

    [Test]
    public void Constructor_AlgorithmWithNullOptions_ThrowsArgumentNullException()
    {
        _mockPeltAlgorithm.Setup(a => a.Options).Returns((PELTOptions)null!);
        Assert.Throws<ArgumentNullException>(() => new PELTPenaltySelector(_mockPeltAlgorithm.Object));
    }

    [Test]
    public void Constructor_AlgorithmWithNullCostFunction_ThrowsArgumentNullException()
    {
        var options = new PELTOptions { CostFunction = null!, MinSize = 1, Jump = 1 };
        _mockPeltAlgorithm.Setup(a => a.Options).Returns(options);
        Assert.Throws<ArgumentNullException>(() => new PELTPenaltySelector(_mockPeltAlgorithm.Object));
    }

    [Test]
    public void Constructor_AlgorithmWithInvalidMinSize_ThrowsArgumentOutOfRangeException()
    {
        var options = new PELTOptions { CostFunction = _mockLikelihoodCostFn.Object, MinSize = 0, Jump = 1 };
        _mockPeltAlgorithm.Setup(a => a.Options).Returns(options);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PELTPenaltySelector(_mockPeltAlgorithm.Object));
    }

    [Test]
    public void Constructor_AlgorithmWithInvalidJump_ThrowsArgumentOutOfRangeException()
    {
        var options = new PELTOptions { CostFunction = _mockLikelihoodCostFn.Object, MinSize = 1, Jump = 0 };
        _mockPeltAlgorithm.Setup(a => a.Options).Returns(options);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PELTPenaltySelector(_mockPeltAlgorithm.Object));
    }

    [Test]
    public void FitAndSelect_NullSignal_ThrowsArgumentNullException()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC);
        Assert.Throws<ArgumentNullException>(() => selector.FitAndSelect((double[])null!, selectionOptions));
    }

    [Test]
    public void FitAndSelect_NullSignalMatrix_ThrowsArgumentNullException()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC);
        Assert.Throws<ArgumentNullException>(() => selector.FitAndSelect((double[,])null!, selectionOptions));
    }

    [Test]
    public void FitAndSelect_NullSelectionOptions_ThrowsArgumentNullException()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        Assert.Throws<ArgumentNullException>(() => selector.FitAndSelect(TestSignal, null!));
    }

    [Test]
    public void FitAndSelect_LikelihoodMethod_NonLikelihoodCost_ThrowsInvalidOperationException()
    {
        var selector = CreateSelector(_mockNonLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC);

        Assert.Throws<InvalidOperationException>(() => selector.FitAndSelect(TestSignal, selectionOptions));
    }

    [Test]
    public void FitAndSelect_LikelihoodMethod_LikelihoodCostUnsupported_ThrowsInvalidOperationException()
    {
        _mockLikelihoodCostFn.Setup(c => c.SupportsInformationCriteria).Returns(false);
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.AIC);

        Assert.Throws<InvalidOperationException>(() => selector.FitAndSelect(TestSignal, selectionOptions));
    }

    [Test]
    public void FitAndSelect_BIC_SelectsCorrectPenalty()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 10, MaxPenalty = 20, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 10.0))))
            .Returns([50]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 20.0))))
            .Returns([]);

        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 50)).Returns(50 * 1.0);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(50, 100)).Returns(50 * 1.0);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 100)).Returns(100 * 1.0);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(50)).Returns(2);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(100)).Returns(2);

        var expectedBIC10 = 100 + 5 * Math.Log(100);
        var expectedBIC20 = 100 + 2 * Math.Log(100);

        var result = selector.FitAndSelect(TestSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(20.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.EqualTo(Array.Empty<int>()));
        Assert.That(result.SelectionMethod, Is.EqualTo(PELTPenaltySelectionMethod.BIC));
        Assert.That(result.Diagnostics, Is.Not.Null);
        Assert.That(result.Diagnostics!, Has.Count.EqualTo(2));
        Assert.That(result.Diagnostics[0].Penalty, Is.EqualTo(10.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.Diagnostics[0].Score,
            Is.EqualTo(expectedBIC10).Within(1e-4)); // Score comparison tolerance can be different
        Assert.That(result.Diagnostics[0].ChangePoints, Is.EqualTo(1));
        Assert.That(result.Diagnostics[1].Penalty, Is.EqualTo(20.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.Diagnostics[1].Score, Is.EqualTo(expectedBIC20).Within(1e-4));
        Assert.That(result.Diagnostics[1].ChangePoints, Is.EqualTo(0));
    }

    [Test]
    public void FitAndSelect_AIC_SelectsCorrectPenalty()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.AIC)
            { MinPenalty = 1, MaxPenalty = 5, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 1.0))))
            .Returns([50]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 5.0))))
            .Returns([]);

        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int start, int end) => (end - start) * 0.5);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(It.IsAny<int>())).Returns(2);

        var result = selector.FitAndSelect(TestSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(5.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.EqualTo(Array.Empty<int>()));
        Assert.That(result.SelectionMethod, Is.EqualTo(PELTPenaltySelectionMethod.AIC));
    }

    [Test]
    public void FitAndSelect_AICc_SelectsCorrectPenalty_AndAppliesCorrection()
    {
        const int shortSignalLength = 15;
        var shortSignal = Enumerable.Range(0, shortSignalLength).Select(i => (double)i).ToArray();
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.AICc)
            { MinPenalty = 1, MaxPenalty = 5, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 1.0))))
            .Returns([8]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 5.0))))
            .Returns([]);

        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int start, int end) => (end - start) * 0.5);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(It.IsAny<int>())).Returns(2);

        const double expectedAICc1 = (4.0 + 3.5) + 2.0 * (2 + 2 + 1) + (2.0 * 5 * 6) / (15.0 - 5 - 1); // ~24.1667
        const double expectedAICc5 = 7.5 + 2.0 * (2 + 0) + (2.0 * 2 * 3) / (15.0 - 2 - 1); // 12.5

        var result = selector.FitAndSelect(shortSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(5.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.EqualTo(Array.Empty<int>()));
        Assert.That(result.SelectionMethod, Is.EqualTo(PELTPenaltySelectionMethod.AICc));
        Assert.That(result.Diagnostics, Is.Not.Null);
        Assert.That(result.Diagnostics!, Has.Count.EqualTo(2));
        Assert.That(result.Diagnostics[0].Score, Is.EqualTo(expectedAICc1).Within(1e-4));
        Assert.That(result.Diagnostics[1].Score, Is.EqualTo(expectedAICc5).Within(1e-4));
    }

    [Test]
    public void FitAndSelect_AICc_ReturnsInfinityWhenCorrectionUndefined_ShouldThrow()
    {
        _mockPeltAlgorithm.Reset();
        _mockLikelihoodCostFn.Reset();

        var options = new PELTOptions { CostFunction = _mockLikelihoodCostFn.Object, MinSize = 2, Jump = 1 };
        _mockPeltAlgorithm.Setup(a => a.Options).Returns(options);
        _mockPeltAlgorithm.Setup(a => a.Fit(It.IsAny<double[]>())).Returns(_mockPeltAlgorithm.Object);

        _mockLikelihoodCostFn.Setup(c => c.SupportsInformationCriteria).Returns(true);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(It.IsAny<int>(), It.IsAny<int>())).Returns(1.0);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(It.IsAny<int>())).Returns(2);

        var selector = new PELTPenaltySelector(_mockPeltAlgorithm.Object);

        const int veryShortSignalLength = 5; // N=5
        var veryShortSignal = Enumerable.Range(0, veryShortSignalLength).Select(i => (double)i).ToArray();

        // This will generate penalties [1.0, 2.0] after internal range adjustment
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.AICc)
            { MinPenalty = 1, MaxPenalty = 1, NumPenaltySteps = 1 };

        // Penalty 1.0: Detect -> [3]. Params = P(0,3)+P(3,5)+K = 2+2+1 = 5. N=5. N <= Params+1. AICc -> Inf.
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 1.0))))
            .Returns([3]);
        // Penalty 2.0: Detect -> [2,4]. Params = P(0,2)+P(2,4)+P(4,5)+K = 2+2+2+2 = 8. N=5. N <= Params+1. AICc -> Inf.
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 2.0))))
            .Returns([2, 4]);

        var ex = Assert.Throws<PELTAlgorithmException>(() => selector.FitAndSelect(veryShortSignal, selectionOptions));

        Assert.That(ex?.Message, Does.Contain("Could not find a suitable penalty"));
        Assert.That(ex?.Message, Does.Contain("infinite/NaN scores"));

        _mockPeltAlgorithm.Verify(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 1.0))),
            Times.Once);
        _mockPeltAlgorithm.Verify(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 2.0))),
            Times.Once);
    }

    [Test]
    public void FitAndSelect_DetectThrowsException_HandlesGracefullyAndSelectsOther()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 10, MaxPenalty = 30, NumPenaltySteps = 3 }; // Test ~10, ~17.32, ~30

        var penalty1 = 10.0;
        var penalty2 = Math.Sqrt(10.0 * 30.0); // ~17.32
        var penalty3 = 30.0;

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, penalty1))))
            .Returns([]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, penalty2))))
            .Throws(new CostFunctionException("Cost failed"));
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, penalty3))))
            .Returns([]);

        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 100)).Returns(100);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(100)).Returns(2);

        var result = selector.FitAndSelect(TestSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(penalty1).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.Empty);
        Assert.That(result.Diagnostics, Is.Not.Null);
        Assert.That(result.Diagnostics!, Has.Count.EqualTo(3));
        Assert.That(result.Diagnostics[1].Penalty,
            Is.EqualTo(penalty2).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.Diagnostics[1].Score, Is.NaN);
        Assert.That(result.Diagnostics[1].ChangePoints, Is.EqualTo(-1));
    }

    [Test]
    public void FitAndSelect_LikelihoodCalcThrowsException_HandlesGracefully()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 10, MaxPenalty = 20, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 10.0))))
            .Returns([50]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 20.0))))
            .Returns([]);

        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(It.IsAny<int>())).Returns(2);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 50))
            .Throws(new CostFunctionException("Likelihood failed"));
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(50, 100)).Returns(50);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 100)).Returns(100);

        var result = selector.FitAndSelect(TestSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(20.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.Empty);
        Assert.That(result.Diagnostics, Is.Not.Null);
        Assert.That(result.Diagnostics!.Count, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Score, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void FitAndSelect_LikelihoodCalcReturnsNaN_HandlesGracefully()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 10, MaxPenalty = 20, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 10.0))))
            .Returns([50]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 20.0))))
            .Returns([]);

        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(It.IsAny<int>())).Returns(2);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 50)).Returns(double.NaN);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(50, 100)).Returns(50);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 100)).Returns(100);

        var result = selector.FitAndSelect(TestSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(20.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.Empty);
        Assert.That(result.Diagnostics, Is.Not.Null);
        Assert.That(result.Diagnostics!, Has.Count.EqualTo(2));
        Assert.That(result.Diagnostics[0].Score, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void FitAndSelect_AllPenaltiesFail_ThrowsPELTAlgorithmException()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 10, MaxPenalty = 20, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.IsAny<double>()))
            .Throws(new CostFunctionException("Cost failed"));

        Assert.Throws<PELTAlgorithmException>(() => selector.FitAndSelect(TestSignal, selectionOptions));
    }

    [Test]
    public void FitAndSelect_InvalidSegmentLengthFromDetect_AssignsInfiniteScore()
    {
        _testOptions = new PELTOptions { CostFunction = _mockLikelihoodCostFn.Object, MinSize = 10, Jump = 1 };
        _mockPeltAlgorithm.Setup(a => a.Options).Returns(_testOptions);
        var selector = new PELTPenaltySelector(_mockPeltAlgorithm.Object);

        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 10, MaxPenalty = 20, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 10.0))))
            .Returns([5]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 20.0))))
            .Returns([]);

        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 100)).Returns(100);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(100)).Returns(2);

        var result = selector.FitAndSelect(TestSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(20.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.Empty);
        Assert.That(result.Diagnostics, Is.Not.Null);
        Assert.That(result.Diagnostics!.Count, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Score, Is.EqualTo(double.PositiveInfinity));
        Assert.That(result.Diagnostics[1].Score, Is.Not.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void FitAndSelect_UsesPenaltyRangeFromOptions()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 5.0, MaxPenalty = 15.0, NumPenaltySteps = 3 }; // Test 5, ~8.66, 15

        var penalty1 = 5.0;
        var penalty2 = Math.Sqrt(5.0 * 15.0); // ~8.66
        var penalty3 = 15.0;

        _mockPeltAlgorithm.Setup(a => a.Detect(It.IsAny<double>())).Returns([]);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 100)).Returns(100);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(100)).Returns(2);

        selector.FitAndSelect(TestSignal, selectionOptions);

        _mockPeltAlgorithm.Verify(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, penalty1))),
            Times.Once);
        _mockPeltAlgorithm.Verify(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, penalty2))),
            Times.Once);
        _mockPeltAlgorithm.Verify(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, penalty3))),
            Times.Once);
        // Verify Detect was *not* called with penalties outside the expected generated range
        _mockPeltAlgorithm.Verify(a => a.Detect(It.Is<double>(p => !NumericUtils.AreApproximatelyEqual(p, penalty1) &&
                                                                   !NumericUtils.AreApproximatelyEqual(p, penalty2) &&
                                                                   !NumericUtils.AreApproximatelyEqual(p, penalty3))),
            Times.Never);
    }

    [Test]
    public void FitAndSelect_TieBreak_PrefersFewerChangePoints()
    {
        var selector = CreateSelector(_mockLikelihoodCostFn.Object);
        var selectionOptions = new PELTPenaltySelectionOptions(PELTPenaltySelectionMethod.BIC)
            { MinPenalty = 10, MaxPenalty = 20, NumPenaltySteps = 2 };

        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 10.0))))
            .Returns([50]);
        _mockPeltAlgorithm.Setup(a => a.Detect(It.Is<double>(p => NumericUtils.AreApproximatelyEqual(p, 20.0))))
            .Returns([]);

        var likelihood1Cp = 100.0;
        var likelihood0Cp = likelihood1Cp + 3 * Math.Log(100); // Make BICs equal

        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 50)).Returns(likelihood1Cp / 2.0);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(50, 100)).Returns(likelihood1Cp / 2.0);
        _mockLikelihoodCostFn.Setup(c => c.ComputeLikelihoodMetric(0, 100)).Returns(likelihood0Cp);
        _mockLikelihoodCostFn.Setup(c => c.GetSegmentParameterCount(It.IsAny<int>())).Returns(2);

        var result = selector.FitAndSelect(TestSignal, selectionOptions);

        Assert.That(result.SelectedPenalty, Is.EqualTo(20.0).Within(NumericUtils.GetDefaultEpsilon<double>()));
        Assert.That(result.OptimalBreakpoints, Is.Empty);
    }
}