using SignalSharp.Utilities;

namespace SignalSharp.Extrapolation.ExponentialSmoothing;

public abstract class HoltBaseExtrapolator
{
    protected static readonly double DoubleEpsilonForGridSearch = NumericUtils.GetStrictEpsilon<double>();
}
