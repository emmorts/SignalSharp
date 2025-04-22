using SignalSharp.Common.Exceptions;

namespace SignalSharp.CostFunctions.Cost;

/// <summary>
/// Optional interface for PELT cost functions that can provide likelihood-based metrics
/// and parameter counts necessary for information criteria (BIC, AIC, AICc).
/// </summary>
public interface ILikelihoodCostFunction : IPELTCostFunction
{
    /// <summary>
    /// Computes a metric related to the negative log-likelihood for a given segment.
    /// <para>
    /// This metric should be proportional to <c>-2 * logLikelihood(segment | parameters_MLE)</c>, potentially omitting
    /// constant terms that depend only on the data itself (like <c>n*log(2*pi)</c> or <c>Sum(log(k!))</c>)
    /// as these usually cancel out in model comparisons using information criteria.
    /// </para>
    /// </summary>
    /// <param name="start">The start index of the segment (inclusive).</param>
    /// <param name="end">The end index of the segment (exclusive).</param>
    /// <returns>The likelihood-related metric for the segment. Should return <c>double.PositiveInfinity</c> if the metric cannot be computed (e.g., due to invalid segment length, zero variance).</returns>
    /// <exception cref="Exceptions.SegmentLengthException">Thrown if segment length is invalid for the underlying model.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if start or end indices are out of bounds.</exception>
    /// <exception cref="UninitializedDataException">Thrown if <see cref="IPELTCostFunction.Fit(double[,])"/> or <see cref="IPELTCostFunction.Fit(double[])"/> has not been called.</exception>
    /// <exception cref="Exceptions.CostFunctionException">Thrown if an unexpected error occurs during metric calculation.</exception>
    double ComputeLikelihoodMetric(int start, int end);

    /// <summary>
    /// Gets the number of effective parameters estimated for a single segment of the given length
    /// by the underlying statistical model associated with the cost function.
    /// <para>
    /// This count should include parameters like mean, variance, rate, probability, AR coefficients, etc.,
    /// depending on the cost function. It does not include the changepoint locations themselves,
    /// as those are typically accounted for separately in the penalty term of information criteria.
    /// </para>
    /// </summary>
    /// <param name="segmentLength">The length of the segment. This might be relevant for some models (e.g., if parameter count depends on length), but often it's constant.</param>
    /// <returns>The number of parameters estimated within the segment model.</returns>
    /// <exception cref="UninitializedDataException">Thrown if <see cref="IPELTCostFunction.Fit(double[,])"/> or <see cref="IPELTCostFunction.Fit(double[])"/> has not been called.</exception>
    int GetSegmentParameterCount(int segmentLength);

    /// <summary>
    /// Indicates if the cost function provides valid likelihood metrics and parameter counts
    /// suitable for use with information criteria like BIC and AIC.
    /// <para>
    /// If <c>true</c>, the cost function must correctly implement <see cref="ComputeLikelihoodMetric"/>
    /// and <see cref="GetSegmentParameterCount"/>.
    /// </para>
    /// </summary>
    bool SupportsInformationCriteria { get; }
}