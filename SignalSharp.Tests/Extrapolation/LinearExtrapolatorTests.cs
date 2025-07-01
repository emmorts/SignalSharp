using SignalSharp.Common.Exceptions;
using SignalSharp.Extrapolation.Linear;
using SignalSharp.Utilities;

namespace SignalSharp.Tests.Extrapolation
{
    [TestFixture]
    public class LinearExtrapolatorTests
    {
        private static readonly double Tolerance = NumericUtils.GetDefaultEpsilon<double>();
        private static readonly double FloatTolerance = NumericUtils.GetDefaultEpsilon<float>();

        #region Constructor Tests

        [Test]
        public void DefaultConstructor_InitializesWithDefaultOptions()
        {
            var extrapolator = new LinearExtrapolator<double>();

            Assert.DoesNotThrow(() => extrapolator.FitAndExtrapolate([1, 2], 1));
        }

        [Test]
        public void ConstructorWithOptions_InitializesCorrectly()
        {
            var options = new LinearExtrapolationOptions { WindowSize = 3 };
            var extrapolator = new LinearExtrapolator<double>(options);

            Assert.DoesNotThrow(() => extrapolator.FitAndExtrapolate([1, 2, 3], 1));
        }

        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_InvalidWindowSize_ThrowsArgumentOutOfRangeException(int invalidWindowSize)
        {
            var options = new LinearExtrapolationOptions { WindowSize = invalidWindowSize };
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearExtrapolator<double>(options));
        }

        [Test]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LinearExtrapolator<double>(null!));
        }

        #endregion

        #region Fit Method Tests

        [Test]
        public void Fit_SignalTooShort_NoWindowSize_ThrowsArgumentException()
        {
            var extrapolator = new LinearExtrapolator<double>();
            Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0]));
        }

        [Test]
        public void Fit_SignalTooShort_ForExplicitWindowSize_ThrowsArgumentException()
        {
            var options = new LinearExtrapolationOptions { WindowSize = 3 };
            var extrapolator = new LinearExtrapolator<double>(options);
            Assert.Throws<ArgumentException>(() => extrapolator.Fit([1.0]));
        }

        [Test]
        public void Fit_EmptySignal_ThrowsArgumentException()
        {
            var extrapolator = new LinearExtrapolator<double>();
            Assert.Throws<ArgumentException>(() => extrapolator.Fit(ReadOnlySpan<double>.Empty));
        }

        [Test]
        public void Fit_ValidSignal_TwoPoints_CalculatesCorrectly()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [1.0, 3.0]; // y = 2x + 1 (for x = 0, 1 relative to window start)
            extrapolator.Fit(signal);

            var forecast = extrapolator.Extrapolate(2);

            using (Assert.EnterMultipleScope())
            {
                // last value = 3.0.; slope expected = 2.0
                // Extrapolate(1) = lastValue + slope * 1 = 3.0 + 2.0 * 1 = 5.0
                // Extrapolate(2) = lastValue + slope * 2 = 3.0 + 2.0 * 2 = 7.0
                Assert.That(forecast[0], Is.EqualTo(5.0).Within(Tolerance));
                Assert.That(forecast[1], Is.EqualTo(7.0).Within(Tolerance));
            }
        }

        [Test]
        public void Fit_PerfectlyLinearData_DefaultWindow_CalculatesCorrectly()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [1.0, 3.0, 5.0, 7.0]; // y = 2x + 1 (for x = 0,1,2,3 relative to window)
            extrapolator.Fit(signal);

            var forecast = extrapolator.Extrapolate(2);

            using (Assert.EnterMultipleScope())
            {
                // last value = 7.0; slope expected = 2.0
                Assert.That(forecast[0], Is.EqualTo(9.0).Within(Tolerance)); // 7.0 + 2.0 * 1
                Assert.That(forecast[1], Is.EqualTo(11.0).Within(Tolerance)); // 7.0 + 2.0 * 2
            }
        }

        [Test]
        public void Fit_PerfectlyLinearData_SpecificWindow_CalculatesCorrectly()
        {
            var options = new LinearExtrapolationOptions { WindowSize = 3 };
            var extrapolator = new LinearExtrapolator<double>(options);
            // signal: { 0, 1.0, 3.0, 5.0, 7.0 }, window uses {3.0, 5.0, 7.0}
            // relative x for window: 0, 1, 2; y-values: 5.0, 7.0; slope = 2.
            double[] signal = [0.0, 1.0, 3.0, 5.0, 7.0];
            extrapolator.Fit(signal);

            var forecast = extrapolator.Extrapolate(2);

            using (Assert.EnterMultipleScope())
            {
                // last value = 7.0; slope expected = 2.0 (from window {3,5,7})
                Assert.That(forecast[0], Is.EqualTo(9.0).Within(Tolerance)); // 7.0 + 2.0 * 1
                Assert.That(forecast[1], Is.EqualTo(11.0).Within(Tolerance)); // 7.0 + 2.0 * 2
            }
        }

        [Test]
        public void Fit_ConstantData_DefaultWindow_CalculatesCorrectSlope()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [5.0, 5.0, 5.0, 5.0];
            extrapolator.Fit(signal);

            var forecast = extrapolator.Extrapolate(2);

            using (Assert.EnterMultipleScope())
            {
                // slope should be 0; last value = 5.0
                Assert.That(forecast[0], Is.EqualTo(5.0).Within(Tolerance)); // 5.0 + 0 * 1
                Assert.That(forecast[1], Is.EqualTo(5.0).Within(Tolerance)); // 5.0 + 0 * 2
            }
        }

        [Test]
        public void Fit_WindowSizeLargerThanSignal_UsesSignalLength()
        {
            var options = new LinearExtrapolationOptions { WindowSize = 5 };
            var extrapolator = new LinearExtrapolator<double>(options);
            double[] signal = [1.0, 3.0, 5.0]; // effective window size is 3; slope = 2
            extrapolator.Fit(signal);

            var forecast = extrapolator.Extrapolate(1);
            // last value = 5.0
            Assert.That(forecast[0], Is.EqualTo(7.0).Within(Tolerance)); // 5.0 + 2.0 * 1
        }

        [Test]
        public void Fit_DenominatorNearZero_HandlesGracefully_ResultsInZeroSlope()
        {
            // this case is typically triggered by constant data within the window
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [5.0, 5.0, 5.0, 5.0];
            extrapolator.Fit(signal);

            var forecast = extrapolator.Extrapolate(1);
            // slope should be 0, intercept for window should be mean (5.0). Last value 5.0
            Assert.That(forecast[0], Is.EqualTo(5.0).Within(Tolerance));
        }

        [Test]
        public void Fit_FloatType_CalculatesCorrectly()
        {
            var extrapolator = new LinearExtrapolator<float>();
            float[] signal = [1.0f, 1.5f, 2.0f, 2.5f]; // slope 0.5f
            extrapolator.Fit(signal);

            var forecast = extrapolator.Extrapolate(2);

            using (Assert.EnterMultipleScope())
            {
                // last value = 2.5f; slope = 0.5f
                Assert.That(forecast[0], Is.EqualTo(3.0f).Within(FloatTolerance)); // 2.5f + 0.5f * 1
                Assert.That(forecast[1], Is.EqualTo(3.5f).Within(FloatTolerance)); // 2.5f + 0.5f * 2
            }
        }

        #endregion

        #region Extrapolate Method Tests

        [Test]
        public void Extrapolate_BeforeFit_ThrowsUninitializedDataException()
        {
            var extrapolator = new LinearExtrapolator<double>();
            Assert.Throws<UninitializedDataException>(() => extrapolator.Extrapolate(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Extrapolate_HorizonLessThanOne_ThrowsArgumentOutOfRangeException(int horizon)
        {
            var extrapolator = new LinearExtrapolator<double>();
            extrapolator.Fit([1, 2]); // minimal fit
            Assert.Throws<ArgumentOutOfRangeException>(() => extrapolator.Extrapolate(horizon));
        }

        [Test]
        public void Extrapolate_PositiveSlope_CorrectValues()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [1.0, 3.0, 5.0]; // slope 2; last val 5
            extrapolator.Fit(signal);
            var forecast = extrapolator.Extrapolate(3);
            Assert.That(forecast[0], Is.EqualTo(7.0).Within(Tolerance)); // 5 + 2*1
            Assert.That(forecast[1], Is.EqualTo(9.0).Within(Tolerance)); // 5 + 2*2
            Assert.That(forecast[2], Is.EqualTo(11.0).Within(Tolerance)); // 5 + 2*3
        }

        [Test]
        public void Extrapolate_NegativeSlope_CorrectValues()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [5.0, 3.0, 1.0]; // slope -2, last val 1
            extrapolator.Fit(signal);
            var forecast = extrapolator.Extrapolate(3);
            Assert.That(forecast[0], Is.EqualTo(-1.0).Within(Tolerance)); // 1 + (-2)*1
            Assert.That(forecast[1], Is.EqualTo(-3.0).Within(Tolerance)); // 1 + (-2)*2
            Assert.That(forecast[2], Is.EqualTo(-5.0).Within(Tolerance)); // 1 + (-2)*3
        }

        [Test]
        public void Extrapolate_ZeroSlope_CorrectValues()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [3.0, 3.0, 3.0]; // slope 0, last val 3
            extrapolator.Fit(signal);
            var forecast = extrapolator.Extrapolate(3);
            Assert.That(forecast[0], Is.EqualTo(3.0).Within(Tolerance)); // 3 + 0*1
            Assert.That(forecast[1], Is.EqualTo(3.0).Within(Tolerance)); // 3 + 0*2
            Assert.That(forecast[2], Is.EqualTo(3.0).Within(Tolerance)); // 3 + 0*3
        }

        [Test]
        public void Extrapolate_OneStep_CorrectValue()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [1.0, 4.0]; // slope 3, last val 4
            extrapolator.Fit(signal);
            var forecast = extrapolator.Extrapolate(1);
            Assert.That(forecast[0], Is.EqualTo(7.0).Within(Tolerance)); // 4 + 3*1
        }

        #endregion

        #region FitAndExtrapolate Method Tests

        [Test]
        public void FitAndExtrapolate_SimpleCase_CorrectValues()
        {
            var extrapolator = new LinearExtrapolator<double>();
            double[] signal = [2.0, 4.0, 6.0]; // slope 2, last val 6
            var forecast = extrapolator.FitAndExtrapolate(signal, 2);
            Assert.That(forecast[0], Is.EqualTo(8.0).Within(Tolerance)); // 6 + 2*1
            Assert.That(forecast[1], Is.EqualTo(10.0).Within(Tolerance)); // 6 + 2*2
        }

        [Test]
        public void FitAndExtrapolate_FloatType_CorrectValues()
        {
            var extrapolator = new LinearExtrapolator<float>();
            float[] signal = [1.0f, 1.5f, 2.0f]; // slope 0.5f, last val 2.0f
            var forecast = extrapolator.FitAndExtrapolate(signal, 2);
            Assert.That(forecast[0], Is.EqualTo(2.5f).Within(FloatTolerance)); // 2.0f + 0.5f*1
            Assert.That(forecast[1], Is.EqualTo(3.0f).Within(FloatTolerance)); // 2.0f + 0.5f*2
        }

        [Test]
        public void FitAndExtrapolate_SignalTooShort_ThrowsArgumentException()
        {
            var extrapolator = new LinearExtrapolator<double>();
            Assert.Throws<ArgumentException>(() => extrapolator.FitAndExtrapolate([1.0], 1));
        }

        [Test]
        public void FitAndExtrapolate_InvalidHorizon_ThrowsArgumentOutOfRangeException()
        {
            var extrapolator = new LinearExtrapolator<double>();
            Assert.Throws<ArgumentOutOfRangeException>(() => extrapolator.FitAndExtrapolate([1.0, 2.0], 0));
        }

        #endregion
    }
}
