using System.Numerics;

namespace SignalSharp.Extrapolation;

/// <summary>
/// Defines the contract for signal extrapolation algorithms.
/// </summary>
public interface IExtrapolator<T>
    where T : INumber<T>
{
    /// <summary>
    /// Fits the extrapolation model to the provided historical signal data.
    /// </summary>
    /// <param name="signal">The historical time series data.</param>
    /// <returns>The fitted extrapolator instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the signal is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the signal is invalid for the specific model (e.g., too short).</exception>
    IExtrapolator<T> Fit(ReadOnlySpan<T> signal);

    /// <summary>
    /// Extrapolates the signal into the future based on the fitted model.
    /// </summary>
    /// <param name="horizon">The number of future time steps to predict.</param>
    /// <returns>An array containing the extrapolated signal values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if horizon is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Fit"/> method has not been called successfully.</exception>
    T[] Extrapolate(int horizon);

    /// <summary>
    /// Fits the model to the signal and then extrapolates into the future.
    /// </summary>
    /// <param name="signal">The historical time series data.</param>
    /// <param name="horizon">The number of future time steps to predict.</param>
    /// <returns>An array containing the extrapolated signal values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the signal is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the signal is invalid for the specific model.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if horizon is less than 1.</exception>
    T[] FitAndExtrapolate(ReadOnlySpan<T> signal, int horizon);
}
