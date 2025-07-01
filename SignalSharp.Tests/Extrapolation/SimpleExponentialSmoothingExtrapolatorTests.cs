using SignalSharp.Common.Exceptions;
using SignalSharp.Extrapolation.ExponentialSmoothing;
using SignalSharp.Utilities;

namespace SignalSharp.Tests.Extrapolation;

[TestFixture]
public class SimpleExponentialSmoothingExtrapolatorTests
{
    private static readonly double Tolerance = NumericUtils.GetDefaultEpsilon<double>();
    private static readonly double FloatTolerance = NumericUtils.GetDefaultEpsilon<float>();

    private static SimpleExponentialSmoothingOptions CreateOptions(double alpha, double? initialLevel = null)
    {
        // Alpha validation is expected to be handled by the Options record constructor or its usage
        // for these tests, we assume Alpha is within a valid conceptual range [0,1] for typical SES,
        // but the class itself will use any double value provided.
        return new SimpleExponentialSmoothingOptions { Alpha = alpha, InitialLevel = initialLevel };
    }

    #region Constructor Tests

    [Test]
    public void Constructor_ValidAlpha_InitializesCorrectly()
    {
        var options = CreateOptions(0.5);
        Assert.DoesNotThrow(() => new SimpleExponentialSmoothingExtrapolator<double>(options));
    }

    [Test]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SimpleExponentialSmoothingExtrapolator<double>(null!));
    }

    [TestCase(-0.1)]
    [TestCase(1.1)]
    [TestCase(2.0)]
    [TestCase(-1.0)]
    public void Constructor_AlphaOutsideTypicalZeroOneRangeInOptions_DoesNotThrowExtrapolatorError(double alpha)
    {
        var options = new SimpleExponentialSmoothingOptions { Alpha = alpha };

        Assert.DoesNotThrow(() => new SimpleExponentialSmoothingExtrapolator<double>(options));
    }

    #endregion

    #region Fit Method Tests

    [Test]
    public void Fit_EmptySignal_ThrowsArgumentException()
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.Fit(ReadOnlySpan<double>.Empty));
    }

    [Test]
    public void Fit_SinglePointSignal_NoInitialLevel_SetsLevelToSignalValue()
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);

        // L_initial = signal[0] = 10.0
        // L_t = alpha * y_t + (1-alpha) * L_{t-1}
        // L_1 = 0.5 * 10.0 + (1-0.5) * 10.0 = 10.0
        Assert.That(forecast[0], Is.EqualTo(10.0).Within(Tolerance));
    }

    [Test]
    public void Fit_SinglePointSignal_WithInitialLevel_UsesInitialLevelCorrectly()
    {
        var options = CreateOptions(0.5, 5.0);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);

        // L_initial = options.InitialLevel = 5.0
        // L_1 = 0.5 * 10.0 + (1-0.5) * 5.0 = 5.0 + 2.5 = 7.5
        Assert.That(forecast[0], Is.EqualTo(7.5).Within(Tolerance));
    }

    [Test]
    public void Fit_MultiplePoints_NoInitialLevel_CalculatesLevelCorrectly()
    {
        var options = CreateOptions(0.2);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);

        // L_initial = signal[0] = 10.0
        // iteration 1 (value=10.0): L = 0.2*10.0 + 0.8*10.0 = 10.0
        // iteration 2 (value=12.0): L = 0.2*12.0 + 0.8*10.0 = 2.4 + 8.0 = 10.4
        // iteration 3 (value=15.0): L = 0.2*15.0 + 0.8*10.4 = 3.0 + 8.32 = 11.32
        Assert.That(forecast[0], Is.EqualTo(11.32).Within(Tolerance));
    }

    [Test]
    public void Fit_MultiplePoints_WithInitialLevel_CalculatesLevelCorrectly()
    {
        var options = CreateOptions(0.2, 8.0);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);

        // L_initial = initialLevel = 8.0
        // iteration 1 (value=10.0): L = 0.2*10.0 + 0.8*8.0 = 2.0 + 6.4 = 8.4
        // iteration 2 (value=12.0): L = 0.2*12.0 + 0.8*8.4 = 2.4 + 6.72 = 9.12
        // iteration 3 (value=15.0): L = 0.2*15.0 + 0.8*9.12 = 3.0 + 7.296 = 10.296
        Assert.That(forecast[0], Is.EqualTo(10.296).Within(Tolerance));
    }

    [Test]
    public void Fit_AlphaIsZero_NoInitialLevel_LevelRemainsFirstSignalValue()
    {
        var options = CreateOptions(0.0);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);
        // L_initial = 10.0. For alpha=0, L_t = L_{t-1}. So level remains 10.0
        Assert.That(forecast[0], Is.EqualTo(10.0).Within(Tolerance));
    }

    [Test]
    public void Fit_AlphaIsZero_WithInitialLevel_LevelRemainsInitialLevel()
    {
        var options = CreateOptions(0.0, 8.0);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);
        // L_initial = 8.0. For alpha=0, L_t = L_{t-1}. So level remains 8.0
        Assert.That(forecast[0], Is.EqualTo(8.0).Within(Tolerance));
    }

    [Test]
    public void Fit_AlphaIsOne_NoInitialLevel_LevelBecomesLastSignalValue()
    {
        var options = CreateOptions(1.0);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);
        // for alpha=1, L_t = y_t. So level becomes signal.Last() = 15.0
        Assert.That(forecast[0], Is.EqualTo(15.0).Within(Tolerance));
    }

    [Test]
    public void Fit_AlphaIsOne_WithInitialLevel_LevelBecomesLastSignalValue()
    {
        var options = CreateOptions(1.0, 8.0);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);
        // for alpha=1, L_t = y_t. So level becomes signal.Last() = 15.0
        Assert.That(forecast[0], Is.EqualTo(15.0).Within(Tolerance));
    }

    [Test]
    public void Fit_SetsIsFittedFlag()
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [1.0, 2.0];

        Assert.Throws<UninitializedDataException>(() => extrapolator.Extrapolate(1), "Extrapolate before Fit should throw.");

        extrapolator.Fit(signal);

        Assert.DoesNotThrow(() => extrapolator.Extrapolate(1), "Extrapolate after Fit should succeed.");
    }

    [Test]
    public void Fit_FloatType_CalculatesCorrectly()
    {
        var options = new SimpleExponentialSmoothingOptions { Alpha = 0.3f, InitialLevel = 5.0f };
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<float>(options);
        float[] signal = [8.0f, 10.0f];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(1);

        // L_initial = initialLevel = 5.0f
        // iteration 1 (value=8.0f):  L = 0.3f*8.0f + 0.7f*5.0f = 2.4f + 3.5f = 5.9f
        // iteration 2 (value=10.0f): L = 0.3f*10.0f + 0.7f*5.9f = 3.0f + 4.13f = 7.13f
        Assert.That(forecast[0], Is.EqualTo(7.13f).Within(FloatTolerance));
    }

    #endregion

    #region Extrapolate Method Tests

    [Test]
    public void Extrapolate_BeforeFit_ThrowsUninitializedDataException()
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        Assert.Throws<UninitializedDataException>(() => extrapolator.Extrapolate(1));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Extrapolate_HorizonLessThanOne_ThrowsArgumentOutOfRangeException(int horizon)
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        extrapolator.Fit([1.0, 2.0]); // minimal fit
        Assert.Throws<ArgumentOutOfRangeException>(() => extrapolator.Extrapolate(horizon));
    }

    [Test]
    public void Extrapolate_ValidHorizon_ReturnsArrayFilledWithLevel()
    {
        var options = CreateOptions(0.2, 8.0);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0];
        extrapolator.Fit(signal); // expected final level is 10.296 (from previous test)

        var forecast = extrapolator.Extrapolate(3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(forecast, Has.Length.EqualTo(3));
            Assert.That(forecast[0], Is.EqualTo(10.296).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(10.296).Within(Tolerance));
            Assert.That(forecast[2], Is.EqualTo(10.296).Within(Tolerance));
        }
    }

    [Test]
    public void Extrapolate_FloatType_ReturnsCorrectValues()
    {
        var options = new SimpleExponentialSmoothingOptions { Alpha = 0.3f, InitialLevel = 5.0f };
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<float>(options);
        float[] signal = [8.0f, 10.0f];
        extrapolator.Fit(signal); // expected final level is 7.13f (from previous test)

        var forecast = extrapolator.Extrapolate(2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(forecast, Has.Length.EqualTo(2));
            Assert.That(forecast[0], Is.EqualTo(7.13f).Within(FloatTolerance));
            Assert.That(forecast[1], Is.EqualTo(7.13f).Within(FloatTolerance));
        }
    }

    #endregion

    #region FitAndExtrapolate Method Tests

    [Test]
    public void FitAndExtrapolate_SimpleCase_CorrectValues()
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        double[] signal = [2.0, 4.0, 6.0];

        var forecast = extrapolator.FitAndExtrapolate(signal, 2);

        // L_initial = 2.0
        // iteration 1 (value=2.0): L = 0.5*2.0 + 0.5*2.0 = 2.0
        // iteration 2 (value=4.0): L = 0.5*4.0 + 0.5*2.0 = 2.0 + 1.0 = 3.0
        // iteration 3 (value=6.0): L = 0.5*6.0 + 0.5*3.0 = 3.0 + 1.5 = 4.5

        using (Assert.EnterMultipleScope())
        {
            Assert.That(forecast, Has.Length.EqualTo(2));
            Assert.That(forecast[0], Is.EqualTo(4.5).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(4.5).Within(Tolerance));
        }
    }

    [Test]
    public void FitAndExtrapolate_FloatType_CorrectValues()
    {
        var options = new SimpleExponentialSmoothingOptions { Alpha = 0.3f }; // no initial level
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<float>(options);
        float[] signal = [10.0f, 12.0f];

        var forecast = extrapolator.FitAndExtrapolate(signal, 2);

        // L_initial = 10.0f
        // iteration 1 (value=10.0f): L = 0.3f*10.0f + 0.7f*10.0f = 3.0f + 7.0f = 10.0f
        // iteration 2 (value=12.0f): L = 0.3f*12.0f + 0.7f*10.0f = 3.6f + 7.0f = 10.6f
        using (Assert.EnterMultipleScope())
        {
            Assert.That(forecast, Has.Length.EqualTo(2));
            Assert.That(forecast[0], Is.EqualTo(10.6f).Within(FloatTolerance));
            Assert.That(forecast[1], Is.EqualTo(10.6f).Within(FloatTolerance));
        }
    }

    [Test]
    public void FitAndExtrapolate_EmptySignal_ThrowsArgumentException()
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.FitAndExtrapolate(ReadOnlySpan<double>.Empty, 1));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void FitAndExtrapolate_InvalidHorizon_ThrowsArgumentOutOfRangeException(int horizon)
    {
        var options = CreateOptions(0.5);
        var extrapolator = new SimpleExponentialSmoothingExtrapolator<double>(options);
        Assert.Throws<ArgumentOutOfRangeException>(() => extrapolator.FitAndExtrapolate([1.0, 2.0], horizon));
    }

    #endregion
}
