using SignalSharp.Common.Exceptions;
using SignalSharp.Extrapolation.ExponentialSmoothing;
using SignalSharp.Utilities;

namespace SignalSharp.Tests.Extrapolation;

[TestFixture]
public class HoltMethodExtrapolatorTests
{
    private static readonly double Tolerance = NumericUtils.GetDefaultEpsilon<double>();
    private static readonly double FloatTolerance = NumericUtils.GetDefaultEpsilon<float>();
    private static readonly double StrictDoubleEpsilon = NumericUtils.GetStrictEpsilon<double>();

    private static HoltMethodOptions CreateOptions(
        double? alpha = 0.5,
        double? beta = 0.5,
        HoltMethodTrendType trendType = HoltMethodTrendType.Additive,
        bool dampTrend = false,
        double? phi = null,
        double? initialLevel = null,
        double? initialTrend = null,
        int optimizationGridSteps = 10
    )
    {
        return new HoltMethodOptions
        {
            Alpha = alpha,
            Beta = beta,
            TrendType = trendType,
            DampTrend = dampTrend,
            Phi = phi,
            InitialLevel = initialLevel,
            InitialTrend = initialTrend,
            OptimizationGridSteps = optimizationGridSteps,
        };
    }

    #region Constructor Tests

    [Test]
    public void Constructor_ValidOptions_InitializesCorrectly()
    {
        var options = CreateOptions();
        Assert.DoesNotThrow(() => new HoltMethodExtrapolator<double>(options));
    }

    [Test]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new HoltMethodExtrapolator<double>(null!));
    }

    [TestCase(-0.1)]
    [TestCase(1.1)]
    public void Constructor_InvalidAlpha_ThrowsArgumentOutOfRangeException(double alpha)
    {
        var options = CreateOptions(alpha: alpha);
        Assert.Throws<ArgumentOutOfRangeException>(() => new HoltMethodExtrapolator<double>(options));
    }

    [TestCase(-0.1)]
    [TestCase(1.1)]
    public void Constructor_InvalidBeta_ThrowsArgumentOutOfRangeException(double beta)
    {
        var options = CreateOptions(beta: beta);
        Assert.Throws<ArgumentOutOfRangeException>(() => new HoltMethodExtrapolator<double>(options));
    }

    [TestCase(0.0)] // Phi must be > 0
    [TestCase(1.0)] // Phi must be < 1
    [TestCase(-0.1)]
    [TestCase(1.1)]
    public void Constructor_InvalidPhi_WithDampTrend_ThrowsArgumentOutOfRangeException(double phi)
    {
        var options = CreateOptions(dampTrend: true, phi: phi);
        Assert.Throws<ArgumentOutOfRangeException>(() => new HoltMethodExtrapolator<double>(options));
    }

    [Test]
    public void Constructor_PhiProvided_DampTrendFalse_PhiNotOne_DoesNotThrowAtConstruction()
    {
        // warning for this is logged during Fit, not an error at construction for ValidateOptions
        var options = CreateOptions(dampTrend: false, phi: 0.8);
        Assert.DoesNotThrow(() => new HoltMethodExtrapolator<double>(options));
    }

    [Test]
    public void Constructor_PhiProvided_DampTrendFalse_PhiIsOne_DoesNotThrowAtConstruction()
    {
        var options = CreateOptions(dampTrend: false, phi: 1.0);
        Assert.DoesNotThrow(() => new HoltMethodExtrapolator<double>(options));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_InvalidOptimizationGridSteps_ThrowsArgumentOutOfRangeException(int steps)
    {
        var options = CreateOptions(optimizationGridSteps: steps);
        Assert.Throws<ArgumentOutOfRangeException>(() => new HoltMethodExtrapolator<double>(options));
    }

    #endregion

    #region Fit Method Tests

    [Test]
    public void Fit_SignalTooShort_ThrowsArgumentException()
    {
        var options = CreateOptions();
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0]));
    }

    [Test]
    public void Fit_MultiplicativeTrend_SignalWithZero_ThrowsArgumentException()
    {
        var options = CreateOptions(trendType: HoltMethodTrendType.Multiplicative);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0, 0.0, 3.0]));
    }

    [Test]
    public void Fit_MultiplicativeTrend_SignalWithNegative_ThrowsArgumentException()
    {
        var options = CreateOptions(trendType: HoltMethodTrendType.Multiplicative);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0, -2.0, 3.0]));
    }

    [Test]
    public void Fit_MultiplicativeTrend_InitialLevelFromOptions_NonPositive_ThrowsArgumentException()
    {
        var options = CreateOptions(trendType: HoltMethodTrendType.Multiplicative, initialLevel: 0.0);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0, 2.0, 3.0]));
    }

    [Test]
    public void Fit_MultiplicativeTrend_InitialLevelFromSignal_NonPositive_ThrowsArgumentException()
    {
        var options = CreateOptions(trendType: HoltMethodTrendType.Multiplicative);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        // StrictDoubleEpsilon is used for checks, so signal[0] = StrictDoubleEpsilon / 2 will be considered non-positive
        Assert.Throws<ArgumentException>(() => extrapolator.Fit([StrictDoubleEpsilon / 2.0, 2.0, 3.0]));
    }

    [Test]
    public void Fit_MultiplicativeTrend_InitialTrendFromOptions_NonPositive_ThrowsArgumentException()
    {
        var options = CreateOptions(trendType: HoltMethodTrendType.Multiplicative, initialTrend: 0.0);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0, 2.0, 3.0]));
    }

    [Test]
    public void Fit_MultiplicativeTrend_EstimatedInitialTrend_NonPositive_ThrowsArgumentException()
    {
        // this case occurs if signal[1]/initialLevel results in a non-positive trend
        // e.g. initialLevel=1, signal[1]=epsilon/2
        var options = CreateOptions(trendType: HoltMethodTrendType.Multiplicative, initialLevel: 1.0);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0, StrictDoubleEpsilon / 2.0, 3.0]));
    }

    [Test]
    public void Fit_Additive_NoOptimization_NoDamping_CalculatesCorrectly()
    {
        var options = CreateOptions(alpha: 0.5, beta: 0.5, dampTrend: false); // Phi will be 1.0
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        double[] signal = [1.0, 2.0, 3.0, 4.0];

        extrapolator.Fit(signal); // L0=1, T0=1.
        // with t=0 loop: _lastLevel = 3.8203125, _lastTrend = 0.82421875
        var forecast = extrapolator.Extrapolate(2);

        using (Assert.EnterMultipleScope())
        {
            // forecast: L + 1*T, L + 2*T
            // [3.8203125 + 0.82421875, 3.8203125 + 2*0.82421875] = [4.64453125, 5.46875]
            Assert.That(forecast[0], Is.EqualTo(4.64453125).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(5.46875).Within(Tolerance));
        }
    }

    [Test]
    public void Fit_Additive_NoOptimization_WithDamping_CalculatesCorrectly()
    {
        var options = CreateOptions(alpha: 0.2, beta: 0.3, dampTrend: true, phi: 0.9, initialLevel: 10, initialTrend: 1);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        double[] signal = [11.5, 12.0, 12.8, 13.5];

        extrapolator.Fit(signal);
        // L0=10, T0=1
        // y1=11.5, alpha=0.2, beta=0.3, phi=0.9
        // L1 = 0.2*11.5 + 0.8*(10 + 0.9*1) = 2.3 + 0.8*10.9 = 2.3 + 8.72 = 11.02
        // T1 = 0.3*(11.02-10) + 0.7*(0.9*1) = 0.3*1.02 + 0.7*0.9 = 0.306 + 0.63 = 0.936
        // y2=12.0
        // L2 = 0.2*12.0 + 0.8*(11.02 + 0.9*0.936) = 2.4 + 0.8*(11.02 + 0.8424) = 2.4 + 0.8*11.8624 = 2.4 + 9.48992 = 11.88992
        // T2 = 0.3*(11.88992-11.02) + 0.7*(0.9*0.936) = 0.3*0.86992 + 0.7*0.8424 = 0.260976 + 0.58968 = 0.850656
        // y3=12.8
        // L3 = 0.2*12.8 + 0.8*(11.88992 + 0.9*0.850656) = 2.56 + 0.8*(11.88992 + 0.7655904) = 2.56 + 0.8*12.6555104 = 2.56 + 10.12440832 = 12.68440832
        // T3 = 0.3*(12.68440832-11.88992) + 0.7*(0.9*0.850656) = 0.3*0.79448832 + 0.7*0.7655904 = 0.238346496 + 0.53591328 = 0.774259776
        // y4=13.5
        // L4 = 0.2*13.5 + 0.8*(12.68440832 + 0.9*0.774259776) = 2.7 + 0.8*(12.68440832 + 0.6968337984) = 2.7 + 0.8*13.3812421184 = 2.7 + 10.70499369472 = 13.40499369472
        // T4 = 0.3*(13.40499369472-12.68440832) + 0.7*(0.9*0.774259776) = 0.3*0.72058537472 + 0.7*0.6968337984 = 0.216175612416 + 0.48778365888 = 0.703959271296
        // last Level: 13.40499369472, Last Trend: 0.703959271296

        var forecast = extrapolator.Extrapolate(1);
        // forecast h=1: L_last + phi*T_last = 13.40499369472 + 0.9 * 0.703959271296 = 13.40499369472 + 0.6335633441664 = 14.0385570388864
        Assert.That(forecast[0], Is.EqualTo(14.0385570389).Within(Tolerance));
    }

    [Test]
    public void Fit_Multiplicative_NoOptimization_NoDamping_CalculatesCorrectly()
    {
        var options = CreateOptions(alpha: 0.4, beta: 0.6, trendType: HoltMethodTrendType.Multiplicative, dampTrend: false);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        double[] signal = [10.0, 12.0, 15.0, 18.0];

        extrapolator.Fit(signal); // L0=10, T0=1.2
        // with t=0 loop: _lastLevel = 17.2026638974, _lastTrend = 1.16983721243
        var forecast = extrapolator.Extrapolate(1);
        // forecast h=1: L_last * T_last^1 = 17.2026638974 * 1.16983721243 = 20.1240675210
        Assert.That(forecast[0], Is.EqualTo(20.1240675210).Within(Tolerance));
    }

    [Test]
    public void Fit_Optimization_AlphaBetaPhi_DampTrendTrue_FindsParameters()
    {
        // data: linear trend with some noise, so alpha/beta shouldn't be too extreme
        // expect phi to be close to 1 if trend is fairly consistent
        double[] signal = [1.0, 1.9, 3.1, 4.0, 5.2, 5.8, 7.1, 8.0];
        var options = CreateOptions(alpha: null, beta: null, dampTrend: true, phi: null, optimizationGridSteps: 5);
        var extrapolator = new HoltMethodExtrapolator<double>(options);

        Assert.DoesNotThrow(() => extrapolator.Fit(signal));

        var forecast = extrapolator.Extrapolate(1);
        Assert.That(forecast[0], Is.Not.NaN);
    }

    [Test]
    public void Fit_Optimization_MultiplicativeWithZeroInSignal_ThrowsArgumentExceptionBeforeOptimization()
    {
        // Multiplicative trend with zero in signal should cause SSE to be Inf/NaN
        var options = CreateOptions(alpha: null, beta: null, trendType: HoltMethodTrendType.Multiplicative, dampTrend: false, optimizationGridSteps: 3);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        double[] signal = [1.0, 0.0, 3.0, 4.0]; // Zero will cause issues for multiplicative

        // ValidateSignalForTrendType is called before optimization and should throw.
        var ex = Assert.Throws<ArgumentException>(() => extrapolator.Fit(signal));
        Assert.That(ex.Message, Does.Contain("Multiplicative trend requires strictly positive signal values"));
    }

    [Test]
    public void Fit_SetsIsFittedFlag()
    {
        var options = CreateOptions();
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        extrapolator.Fit([1.0, 2.0, 3.0]);
        Assert.DoesNotThrow(() => extrapolator.Extrapolate(1), "Extrapolate after Fit should succeed.");
    }

    [Test]
    public void Fit_FloatType_CalculatesCorrectly_NoOptimization_Additive()
    {
        var options = CreateOptions(alpha: 0.5f, beta: 0.5f, trendType: HoltMethodTrendType.Additive, dampTrend: false);
        var extrapolator = new HoltMethodExtrapolator<float>(options!);
        float[] signal = [1.0f, 2.0f, 3.0f, 4.0f];

        extrapolator.Fit(signal);
        var forecast = extrapolator.Extrapolate(2);

        using (Assert.EnterMultipleScope())
        {
            // with t=0 loop: _lastLevel = 3.8203125f, _lastTrend = 0.82421875f
            Assert.That(forecast[0], Is.EqualTo(4.64453125f).Within(FloatTolerance));
            Assert.That(forecast[1], Is.EqualTo(5.46875f).Within(FloatTolerance));
        }
    }

    #endregion

    #region Extrapolate Method Tests

    [Test]
    public void Extrapolate_BeforeFit_ThrowsUninitializedDataException()
    {
        var options = CreateOptions();
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<UninitializedDataException>(() => extrapolator.Extrapolate(1));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Extrapolate_HorizonLessThanOne_ThrowsArgumentOutOfRangeException(int horizon)
    {
        var options = CreateOptions();
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        extrapolator.Fit([1.0, 2.0, 3.0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => extrapolator.Extrapolate(horizon));
    }

    [Test]
    public void Extrapolate_Additive_NoDamping_CorrectForecast()
    {
        var options = CreateOptions(alpha: 0.5, beta: 0.5, dampTrend: false);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        extrapolator.Fit([1.0, 2.0, 3.0, 4.0]); // L0=1, T0=1
        // with t=0 loop: _lastLevel=3.8203125, _lastTrend=0.82421875
        var forecast = extrapolator.Extrapolate(3);

        using (Assert.EnterMultipleScope())
        {
            // h=1: L + 1*T = 3.8203125 + 1*0.82421875 = 4.64453125
            // h=2: L + 2*T = 3.8203125 + 2*0.82421875 = 5.46875
            // h=3: L + 3*T = 3.8203125 + 3*0.82421875 = 6.29296875
            Assert.That(forecast[0], Is.EqualTo(4.64453125).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(5.46875).Within(Tolerance));
            Assert.That(forecast[2], Is.EqualTo(6.29296875).Within(Tolerance));
        }
    }

    [Test]
    public void Extrapolate_Additive_WithDamping_CorrectForecast()
    {
        var options = CreateOptions(alpha: 0.2, beta: 0.3, dampTrend: true, phi: 0.9, initialLevel: 10, initialTrend: 1);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        extrapolator.Fit([11.5, 12.0, 12.8, 13.5]);
        // from earlier calculation: L_last=13.40499369472, T_last=0.703959271296, Phi=0.9
        var forecast = extrapolator.Extrapolate(2);

        using (Assert.EnterMultipleScope())
        {
            // h=1: phi_sum = 0.9. Forecast = L + phi_sum*T = 13.40499369472 + 0.9*0.703959271296 = 14.0385570389
            // h=2: phi_sum = 0.9 + 0.9^2 = 0.9 + 0.81 = 1.71. Forecast = L + phi_sum*T = 13.40499369472 + 1.71*0.703959271296 = 13.40499369472 + 1.20377035391616 = 14.6087640486
            Assert.That(forecast[0], Is.EqualTo(14.0385570389).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(14.6087640486).Within(Tolerance));
        }
    }

    [Test]
    public void Extrapolate_Multiplicative_NoDamping_CorrectForecast()
    {
        var options = CreateOptions(alpha: 0.4, beta: 0.6, trendType: HoltMethodTrendType.Multiplicative, dampTrend: false);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        extrapolator.Fit([10.0, 12.0, 15.0, 18.0]); // L0=10, T0=1.2
        // with t=0 loop: _lastLevel=17.2026638974, _lastTrend=1.16983721243, Phi=1
        var forecast = extrapolator.Extrapolate(2);

        using (Assert.EnterMultipleScope())
        {
            // h=1: phi_sum=1 (since phi=1). Forecast = L * T^1 = 17.2026638974 * 1.16983721243^1 = 20.1240675210
            // h=2: phi_sum=2 (since phi=1). Forecast = L * T^2 = 17.2026638974 * 1.16983721243^2 = 23.5419248994
            Assert.That(forecast[0], Is.EqualTo(20.1240675210).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(23.5419248994).Within(Tolerance));
        }
    }

    #endregion

    #region FitAndExtrapolate Method Tests

    [Test]
    public void FitAndExtrapolate_Additive_SimpleCase_CorrectValues()
    {
        var options = CreateOptions(alpha: 0.5, beta: 0.5);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        double[] signal = [2.0, 4.0, 6.0]; // L0=2, T0=2
        // with t=0 loop: _lastLevel=5.8125, _lastTrend=1.46875
        var forecast = extrapolator.FitAndExtrapolate(signal, 2);

        using (Assert.EnterMultipleScope())
        {
            // h=1: 5.8125 + 1*1.46875 = 7.28125
            // h=2: 5.8125 + 2*1.46875 = 8.75
            Assert.That(forecast[0], Is.EqualTo(7.28125).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(8.75).Within(Tolerance));
        }
    }

    [Test]
    public void FitAndExtrapolate_SignalTooShort_ThrowsArgumentException()
    {
        var options = CreateOptions();
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentException>(() => extrapolator.FitAndExtrapolate([1.0], 1));
    }

    [Test]
    public void FitAndExtrapolate_InvalidHorizon_ThrowsArgumentOutOfRangeException()
    {
        var options = CreateOptions();
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        Assert.Throws<ArgumentOutOfRangeException>(() => extrapolator.FitAndExtrapolate([1.0, 2.0], 0));
    }

    [Test]
    public void Fit_ConstantSignal_Additive_NoOptimization_NoDamping_CalculatesCorrectly()
    {
        var options = CreateOptions(alpha: 0.5, beta: 0.5, dampTrend: false, initialLevel: 5.0, initialTrend: 0.0);
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        double[] signal = [5.0, 5.0, 5.0, 5.0];

        extrapolator.Fit(signal);
        // L0=5, T0=0. alpha=0.5, beta=0.5, phi=1
        // y1=5: L1 = 0.5*5 + 0.5*(5+0) = 2.5+2.5=5. T1 = 0.5*(5-5) + 0.5*0 = 0
        // ...all L=5, T=0
        // _lastLevel = 5, _lastTrend = 0.
        var forecast = extrapolator.Extrapolate(2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(forecast[0], Is.EqualTo(5.0).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(5.0).Within(Tolerance));
        }
    }

    [Test]
    public void Fit_ConstantSignal_Multiplicative_NoOptimization_NoDamping_CalculatesCorrectly()
    {
        var options = CreateOptions(
            alpha: 0.5,
            beta: 0.5,
            trendType: HoltMethodTrendType.Multiplicative,
            dampTrend: false,
            initialLevel: 5.0,
            initialTrend: 1.0
        );
        var extrapolator = new HoltMethodExtrapolator<double>(options);
        double[] signal = [5.0, 5.0, 5.0, 5.0];

        extrapolator.Fit(signal);
        // L0=5, T0=1. alpha=0.5, beta=0.5, phi=1
        // y1=5: L1 = 0.5*5 + 0.5*(5*1) = 5. T1 = 0.5*(5/5) + 0.5*1 = 0.5*1+0.5 = 1
        // ...all L=5, T=1
        // _lastLevel = 5, _lastTrend = 1.
        var forecast = extrapolator.Extrapolate(2);

        using (Assert.EnterMultipleScope())
        {
            // h=1: 5 * 1^1 = 5
            // h=2: 5 * 1^2 = 5
            Assert.That(forecast[0], Is.EqualTo(5.0).Within(Tolerance));
            Assert.That(forecast[1], Is.EqualTo(5.0).Within(Tolerance));
        }
    }

    #endregion
}
