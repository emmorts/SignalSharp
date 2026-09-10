using SignalSharp.Common;
using SignalSharp.Common.Models;

namespace SignalSharp.Tests.Utilities;

[TestFixture]
public class SignalPaddingTests
{
    [Test]
    public void ApplyPadding_Periodic_ContinuesThePeriodForwardOnBothSides()
    {
        double[] signal = [1.0, 2.0, 3.0];

        var padded = SignalPadding.ApplyPadding(signal, windowSize: 5, Padding.Periodic, paddedValue: 0);

        // Left pad wraps backwards (...,2,3 | 1,2,3) and the right pad continues forwards (1,2,3 | 1,2,...).
        Assert.That(padded, Is.EqualTo(new[] { 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0 }));
    }

    [Test]
    public void ApplyPadding_Periodic_WindowLargerThanSignal_RepeatsTheSignal()
    {
        double[] signal = [1.0, 2.0, 3.0];

        var padded = SignalPadding.ApplyPadding(signal, windowSize: 9, Padding.Periodic, paddedValue: 0);

        Assert.That(padded, Is.EqualTo(new[] { 3.0, 1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0 }));
    }

    [Test]
    public void ApplyPadding_Mirror_WindowLargerThanSignal_ReflectsWithoutThrowing()
    {
        double[] signal = [1.0, 2.0, 3.0];

        var padded = SignalPadding.ApplyPadding(signal, windowSize: 9, Padding.Mirror, paddedValue: 0);

        // Edge-inclusive reflection: the boundary values repeat first, then the signal is reflected.
        Assert.That(padded, Is.EqualTo(new[] { 3.0, 3.0, 2.0, 1.0, 1.0, 2.0, 3.0, 3.0, 2.0, 1.0, 1.0 }));
    }

    [Test]
    public void ApplyPadding_NearestAndConstant_WindowLargerThanSignal_DoNotThrow()
    {
        double[] signal = [1.0, 2.0, 3.0];

        Assert.That(
            SignalPadding.ApplyPadding(signal, windowSize: 9, Padding.Nearest, paddedValue: 0),
            Is.EqualTo(new[] { 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 3.0, 3.0, 3.0, 3.0, 3.0 })
        );
        Assert.That(
            SignalPadding.ApplyPadding(signal, windowSize: 9, Padding.Constant, paddedValue: -1),
            Is.EqualTo(new[] { -1.0, -1.0, -1.0, -1.0, 1.0, 2.0, 3.0, -1.0, -1.0, -1.0, -1.0 })
        );
    }

    [Test]
    public void ApplyPadding_None_ReturnsSignalUnchanged()
    {
        double[] signal = [1.0, 2.0, 3.0];

        var padded = SignalPadding.ApplyPadding(signal, windowSize: 3, Padding.None, paddedValue: 0);

        Assert.That(padded, Is.SameAs(signal));
    }

    [Test]
    public void ApplyPadding_InvalidArguments_Throw()
    {
        Assert.That(() => SignalPadding.ApplyPadding(null!, 3, Padding.Mirror, 0), Throws.ArgumentNullException);
        Assert.That(() => SignalPadding.ApplyPadding([], 3, Padding.Mirror, 0), Throws.ArgumentException);
        Assert.That(() => SignalPadding.ApplyPadding([1.0], -1, Padding.Mirror, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => SignalPadding.ApplyPadding([1.0], 3, (Padding)99, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void TrimPadding_UndersizedInput_Throws()
    {
        Assert.That(() => SignalPadding.TrimPadding([1.0, 2.0], originalLength: 3, windowSize: 5), Throws.ArgumentException);
        Assert.That(() => SignalPadding.TrimPadding([1.0, 2.0, 3.0], originalLength: 3, windowSize: 1), Throws.Nothing);
    }
}
