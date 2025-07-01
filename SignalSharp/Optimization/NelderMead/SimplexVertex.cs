using System.Numerics;

namespace SignalSharp.Optimization.NelderMead;

/// <summary>
/// Represents a vertex in the Nelder-Mead simplex.
/// </summary>
/// <typeparam name="TMetric">The type of the metric being optimized, which must implement <see cref="IFloatingPointIeee754{TMetric}"/>.</typeparam>
internal record SimplexVertex<TMetric>(double[] ParametersArray, IReadOnlyDictionary<string, double> ParametersDict, TMetric Value)
    : IComparable<SimplexVertex<TMetric>>
    where TMetric : IFloatingPointIeee754<TMetric>
{
    /// <inheritdoc />
    public int CompareTo(SimplexVertex<TMetric>? other)
    {
        if (other is null)
        {
            return 1;
        }

        bool thisIsNaN = TMetric.IsNaN(Value);
        bool otherIsNaN = TMetric.IsNaN(other.Value);

        if (thisIsNaN && otherIsNaN)
        {
            return 0;
        }

        if (thisIsNaN)
        {
            return 1; // this NaN is worse (considered larger)
        }

        if (otherIsNaN)
        {
            return -1; // other NaN is worse, so this is better (considered smaller)
        }

        return Value.CompareTo(other.Value);
    }
}
