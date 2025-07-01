using System.Numerics;
using SignalSharp.Common.Exceptions;
using SignalSharp.Utilities;

namespace SignalSharp.Extrapolation.Linear;

/// <summary>
/// Implements signal extrapolation using a linear trend fitted to recent historical data,
/// supporting generic numeric types.
/// </summary>
/// <typeparam name="T">The numeric type of the signal data, implementing <see cref="IFloatingPoint{T}"/>.</typeparam>
public class LinearExtrapolator<T> : IExtrapolator<T>
    where T : IFloatingPoint<T>
{
    private readonly LinearExtrapolationOptions _options;
    private T _slope = default!;
    private T _intercept = default!;
    private T _lastValue = default!;
    private int _signalLength;
    private bool _isFitted;
    private readonly T _epsilon;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearExtrapolator{T}"/> class with specified options.
    /// </summary>
    /// <param name="options">Configuration options for the linear extrapolation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <see cref="LinearExtrapolationOptions.WindowSize"/> is provided and is less than 2.</exception>
    public LinearExtrapolator(LinearExtrapolationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.WindowSize.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(options.WindowSize.Value, 2, nameof(options.WindowSize));
        }

        _options = options;
        _epsilon = NumericUtils.GetStrictEpsilon<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearExtrapolator{T}"/> class with default options.
    /// </summary>
    public LinearExtrapolator()
        : this(new LinearExtrapolationOptions()) { }

    /// <summary>
    /// Fits the linear extrapolation model to the provided historical signal data.
    /// </summary>
    /// <param name="signal">The historical time series data. Must contain at least 2 data points if no window size is specified, or if the window size covers the whole signal.</param>
    /// <returns>The fitted extrapolator instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the effective number of data points for fitting is less than 2.</exception>
    public IExtrapolator<T> Fit(ReadOnlySpan<T> signal)
    {
        _signalLength = signal.Length;

        int effectiveWindowSize = _options.WindowSize ?? _signalLength;
        effectiveWindowSize = Math.Min(effectiveWindowSize, _signalLength);

        if (effectiveWindowSize < 2)
        {
            throw new ArgumentException($"Cannot fit linear trend with less than 2 data points. Effective window size: {effectiveWindowSize}", nameof(signal));
        }

        int startIndex = _signalLength - effectiveWindowSize;
        var signalWindow = signal[startIndex..(startIndex + effectiveWindowSize)];

        T sumX = T.Zero;
        T sumY = T.Zero;
        T sumXy = T.Zero;
        T sumXSquared = T.Zero;
        T n = T.CreateChecked(effectiveWindowSize);

        for (int i = 0; i < effectiveWindowSize; i++)
        {
            T x = T.CreateChecked(i);
            T y = signalWindow[i];
            sumX += x;
            sumY += y;
            sumXy += x * y;
            sumXSquared += x * x;
        }

        T denominator = n * sumXSquared - sumX * sumX;

        if (T.Abs(denominator) < _epsilon)
        {
            _slope = T.Zero;
            _intercept = sumY / n;
        }
        else
        {
            _slope = (n * sumXy - sumX * sumY) / denominator;
            _intercept = (sumY * sumXSquared - sumX * sumXy) / denominator;
        }

        _lastValue = signal[^1];
        _isFitted = true;

        return this;
    }

    /// <summary>
    /// Extrapolates the signal into the future based on the fitted linear model.
    /// </summary>
    /// <param name="horizon">The number of future time steps to predict. Must be >= 1.</param>
    /// <returns>An array of type <typeparamref name="T"/> containing the extrapolated signal values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="horizon"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Fit"/> method has not been called successfully.</exception>
    public T[] Extrapolate(int horizon)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(horizon, 1, nameof(horizon));
        UninitializedDataException.ThrowIfFalse(_isFitted, "Fit() must be called before Extrapolate().");

        var extrapolatedValues = new T[horizon];

        for (int i = 0; i < horizon; i++)
        {
            T stepsAhead = T.CreateChecked(i + 1);
            extrapolatedValues[i] = _lastValue + _slope * stepsAhead;
        }

        return extrapolatedValues;
    }

    /// <summary>
    /// Fits the linear model to the signal and then extrapolates into the future.
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
