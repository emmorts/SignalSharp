using System.Numerics;
using SignalSharp.Common.Exceptions;

namespace SignalSharp.Extrapolation.ExponentialSmoothing;

/// <summary>
/// Implements Simple Exponential Smoothing (SES) extrapolation, supporting generic numeric types.
/// </summary>
/// <typeparam name="T">The numeric type of the signal data, implementing <see cref="IFloatingPoint{T}"/>.</typeparam>
public class SimpleExponentialSmoothingExtrapolator<T> : IExtrapolator<T>
    where T : IFloatingPoint<T>
{
    private readonly SimpleExponentialSmoothingOptions _options;
    private readonly T _alpha;
    private T _level = default!;
    private bool _isFitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleExponentialSmoothingExtrapolator{T}"/> class with specified options.
    /// </summary>
    /// <param name="options">Configuration options for Simple Exponential Smoothing.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public SimpleExponentialSmoothingExtrapolator(SimpleExponentialSmoothingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _alpha = T.CreateChecked(_options.Alpha);
    }

    /// <summary>
    /// Fits the Simple Exponential Smoothing model to the provided historical signal data.
    /// </summary>
    /// <param name="signal">The historical time series data. Must not be empty.</param>
    /// <returns>The fitted extrapolator instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="signal"/> is empty.</exception>
    public IExtrapolator<T> Fit(ReadOnlySpan<T> signal)
    {
        if (signal.IsEmpty)
        {
            throw new ArgumentException("Signal cannot be empty.", nameof(signal));
        }

        _level = _options.InitialLevel.HasValue ? T.CreateChecked(_options.InitialLevel.Value) : signal[0];

        foreach (var value in signal)
        {
            _level = _alpha * value + (T.One - _alpha) * _level;
        }

        _isFitted = true;

        return this;
    }

    /// <summary>
    /// Extrapolates the signal into the future based on the fitted SES model.
    /// For SES, the forecast is simply the last smoothed level repeated.
    /// </summary>
    /// <param name="horizon">The number of future time steps to predict. Must be >= 1.</param>
    /// <returns>An array of type <typeparamref name="T"/> containing the extrapolated signal values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="horizon"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Fit"/> method has not been called successfully.</exception>
    public T[] Extrapolate(int horizon)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(horizon, 1, nameof(horizon));
        UninitializedDataException.ThrowIfFalse(_isFitted, "Fit() must be called before Extrapolate().");

        var forecast = new T[horizon];
        Array.Fill(forecast, _level);
        return forecast;
    }

    /// <summary>
    /// Fits the SES model to the signal and then extrapolates into the future.
    /// </summary>
    /// <param name="signal">The historical time series data.</param>
    /// <param name="horizon">The number of future time steps to predict.</param>
    /// <returns>An array of type <typeparamref name="T"/> containing the extrapolated signal values.</returns>
    public T[] FitAndExtrapolate(ReadOnlySpan<T> signal, int horizon)
    {
        Fit(signal);
        return Extrapolate(horizon);
    }
}
