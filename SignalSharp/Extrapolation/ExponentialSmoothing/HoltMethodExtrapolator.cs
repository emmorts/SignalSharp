using System.Diagnostics;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SignalSharp.Common.Exceptions;
using SignalSharp.Utilities;

namespace SignalSharp.Extrapolation.ExponentialSmoothing;

/// <summary>
/// Implements Holt's Linear Trend method (Double Exponential Smoothing) for extrapolation,
/// supporting generic numeric types.
/// </summary>
/// <typeparam name="T">The numeric type of the signal data, implementing <see cref="IFloatingPointIeee754{T}"/>.</typeparam>
/// <remarks>
/// <para>
/// Holt's method models time series data with a level and a trend component, suitable for data
/// exhibiting a trend but no seasonality. It supports both Additive and Multiplicative trend models,
/// optionally with a damped trend modification.
/// </para>
/// <para>
/// **Parameter Optimization:** If the smoothing parameters Alpha, Beta, or Phi (when damping is enabled)
/// are not provided (set to null in <see cref="HoltMethodOptions"/>), the <see cref="Fit"/> method
/// performs an optimization search (grid search by default) to find parameters that minimize the
/// Sum of Squared Errors (SSE) of one-step-ahead forecasts on the training data. This automated
/// tuning adds computational cost but can improve forecast accuracy without manual intervention.
/// </para>
/// </remarks>
public class HoltMethodExtrapolator<T> : HoltBaseExtrapolator, IExtrapolator<T>
    where T : IFloatingPointIeee754<T>
{
    private readonly HoltMethodOptions _options;
    private readonly ILogger _logger;
    private readonly T _epsilon;

    private T _effectiveAlpha;
    private T _effectiveBeta;
    private T _effectivePhi;

    private T _lastLevel = default!;
    private T _lastTrend = default!;
    private bool _isFitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="HoltMethodExtrapolator{T}"/> class with specified options and logger.
    /// </summary>
    /// <param name="options">Configuration options for Holt's method. Must not be null.</param>
    /// <param name="logger">Optional logger instance for diagnostics.</param>
    public HoltMethodExtrapolator(HoltMethodOptions options, ILogger<HoltMethodExtrapolator<T>>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ValidateOptions(options);

        _options = options;
        _logger = (ILogger?)logger ?? NullLogger.Instance;
        _epsilon = NumericUtils.GetStrictEpsilon<T>();

        _effectiveAlpha = T.NaN;
        _effectiveBeta = T.NaN;
        _effectivePhi = T.One;

        _logger.LogDebug("HoltMethodExtrapolator<{TypeName}> initialized with options: {Options}", typeof(T).Name, _options);
    }

    private static void ValidateOptions(HoltMethodOptions options)
    {
        if (options.Alpha.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(options.Alpha.Value, nameof(options.Alpha));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Alpha.Value, 1.0, nameof(options.Alpha));
        }

        if (options.Beta.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(options.Beta.Value, nameof(options.Beta));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Beta.Value, 1.0, nameof(options.Beta));
        }

        if (options is { DampTrend: true, Phi: not null })
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.Phi.Value, 0.0, nameof(options.Phi));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(options.Phi.Value, 1.0, nameof(options.Phi));
        }
        else if (options is { DampTrend: false, Phi: not null } && !NumericUtils.AreApproximatelyEqual(options.Phi.Value, 1.0, DoubleEpsilonForGridSearch))
        {
            // Logging for this case handled in Fit()/ConfigureEffectiveParameters
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.OptimizationGridSteps, nameof(options.OptimizationGridSteps));
    }

    /// <inheritdoc />
    public IExtrapolator<T> Fit(ReadOnlySpan<T> signal)
    {
        var signalLength = signal.Length;
        if (signalLength < 2)
        {
            throw new ArgumentException($"Holt's method requires at least 2 data points. Provided: {signalLength}.", nameof(signal));
        }

        ValidateSignalForTrendType(signal);

        ConfigureEffectiveParameters(signal);

        _logger.LogInformation(
            "Fitting Holt model (Type: {TrendType}, Damped: {IsDamped}) using parameters: Alpha={Alpha:F4}, Beta={Beta:F4}, Phi={Phi:F4}",
            _options.TrendType,
            _options.DampTrend,
            Convert.ToDouble(_effectiveAlpha),
            Convert.ToDouble(_effectiveBeta),
            Convert.ToDouble(_effectivePhi)
        );

        InitializeLevelAndTrend(signal, out var currentLevel, out var currentTrend);

        _logger.LogTrace("Initial state: Level={InitialLevel:G4}, Trend={InitialTrend:G4}", Convert.ToDouble(currentLevel), Convert.ToDouble(currentTrend));

        for (var t = 0; t < signalLength; t++)
        {
            var previousLevel = currentLevel;
            var previousTrend = currentTrend;
            var currentValue = signal[t];

            try
            {
                UpdateModelState(currentValue, ref currentLevel, ref currentTrend, previousLevel, previousTrend, t);

                _logger.LogTrace(
                    "Fit step {Time}: Value={Value:G4}, PrevL={PrevLevel:G4}, PrevT={PrevTrend:G4} -> NewL={NewLevel:G4}, NewT={NewTrend:G4}",
                    t,
                    Convert.ToDouble(currentValue),
                    Convert.ToDouble(previousLevel),
                    Convert.ToDouble(previousTrend),
                    Convert.ToDouble(currentLevel),
                    Convert.ToDouble(currentTrend)
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Fit failed at step {Time} due to invalid state calculation.", t);
                throw new InvalidOperationException($"Fit failed at step {t}. See inner exception.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during fit step {Time}.", t);
                throw new InvalidOperationException($"Unexpected error during fit at step {t}. See inner exception.", ex);
            }
        }

        _lastLevel = currentLevel;
        _lastTrend = currentTrend;
        _isFitted = true;

        _logger.LogDebug("Fit complete. Final State: Level={Level:G4}, Trend={Trend:G4}", Convert.ToDouble(_lastLevel), Convert.ToDouble(_lastTrend));

        return this;
    }

    private void ConfigureEffectiveParameters(ReadOnlySpan<T> signal)
    {
        bool optimizeAlpha = !_options.Alpha.HasValue;
        bool optimizeBeta = !_options.Beta.HasValue;
        bool optimizePhi = _options is { DampTrend: true, Phi: null };

        if (optimizeAlpha || optimizeBeta || optimizePhi)
        {
            _logger.LogInformation(
                "Optimizing Holt parameters (Alpha: {OptA}, Beta: {OptB}, Phi: {OptP}) using grid search (Steps: {Steps})...",
                optimizeAlpha,
                optimizeBeta,
                optimizePhi,
                _options.OptimizationGridSteps
            );

            var (bestAlphaDouble, bestBetaDouble, bestPhiDouble) = FindOptimalParametersViaGridSearch(signal);

            _effectiveAlpha = T.CreateChecked(bestAlphaDouble);
            _effectiveBeta = T.CreateChecked(bestBetaDouble);
            _effectivePhi = T.CreateChecked(bestPhiDouble);

            _logger.LogInformation(
                "Optimization complete. Effective parameters set: Alpha={Alpha:F4}, Beta={Beta:F4}, Phi={Phi:F4}",
                bestAlphaDouble,
                bestBetaDouble,
                bestPhiDouble
            );
        }
        else
        {
            _effectiveAlpha = T.CreateChecked(_options.Alpha!.Value);
            _effectiveBeta = T.CreateChecked(_options.Beta!.Value);
            _effectivePhi = _options.DampTrend ? T.CreateChecked(_options.Phi!.Value) : T.One;

            if (_options is { DampTrend: false, Phi: not null } && !NumericUtils.AreApproximatelyEqual(_options.Phi.Value, 1.0, DoubleEpsilonForGridSearch))
            {
                _logger.LogWarning("Phi parameter provided ({PhiValue:F4}) but DampTrend is false. Phi will be ignored (effectively 1.0).", _options.Phi.Value);
            }
        }
    }

    private (double BestAlpha, double BestBeta, double BestPhi) FindOptimalParametersViaGridSearch(ReadOnlySpan<T> signal)
    {
        double minSse = double.PositiveInfinity;
        double bestAlpha = double.NaN;
        double bestBeta = double.NaN;
        double bestPhi = double.NaN;

        int steps = Math.Max(2, _options.OptimizationGridSteps);
        double stepSize = 1.0 / (steps - 1);

        bool optimizeAlpha = !_options.Alpha.HasValue;
        bool optimizeBeta = !_options.Beta.HasValue;
        bool optimizePhi = _options is { DampTrend: true, Phi: null };

        var alphaValues = optimizeAlpha ? Enumerable.Range(0, steps).Select(i => Math.Clamp(i * stepSize, 0.0, 1.0)).ToArray() : [_options.Alpha!.Value];

        var betaValues = optimizeBeta ? Enumerable.Range(0, steps).Select(j => Math.Clamp(j * stepSize, 0.0, 1.0)).ToArray() : [_options.Beta!.Value];

        double[] phiValues;
        if (optimizePhi)
        {
            double phiStepSize = (1.0 - 2 * DoubleEpsilonForGridSearch) / Math.Max(1, steps - 1);
            phiValues = Enumerable
                .Range(0, steps)
                .Select(k => Math.Clamp(DoubleEpsilonForGridSearch + k * phiStepSize, DoubleEpsilonForGridSearch, 1.0 - DoubleEpsilonForGridSearch))
                .ToArray();
        }
        else
        {
            phiValues = [_options.DampTrend ? _options.Phi!.Value : 1.0];
        }

        long totalCombinations = alphaValues.Length * betaValues.Length * phiValues.Length;
        long currentCombination = 0;
        long logThreshold = Math.Max(1, totalCombinations / 20);

        _logger.LogDebug(
            "Starting grid search with {TotalCombinations} combinations (AlphaSteps={AS}, BetaSteps={BS}, PhiSteps={PS}).",
            totalCombinations,
            alphaValues.Length,
            betaValues.Length,
            phiValues.Length
        );

        foreach (double currentAlphaD in alphaValues)
        {
            foreach (double currentBetaD in betaValues)
            {
                foreach (double currentPhiD in phiValues)
                {
                    currentCombination++;
                    if (currentCombination % logThreshold == 0 || currentCombination == totalCombinations)
                    {
                        _logger.LogTrace(
                            "Grid search progress: {ProgressPercent:F1}% ({Current}/{Total})",
                            (100.0 * currentCombination / totalCombinations),
                            currentCombination,
                            totalCombinations
                        );
                    }

                    T sseT = CalculateSseForParameters(signal, currentAlphaD, currentBetaD, currentPhiD);
                    double sse = Convert.ToDouble(sseT);

                    if (!double.IsNaN(sse) && !double.IsPositiveInfinity(sse) && sse < minSse)
                    {
                        minSse = sse;
                        bestAlpha = currentAlphaD;
                        bestBeta = currentBetaD;
                        bestPhi = currentPhiD;

                        _logger.LogTrace(
                            "New best SSE={SSE:G6} found for Alpha={Alpha:F4}, Beta={Beta:F4}, Phi={Phi:F4}",
                            minSse,
                            bestAlpha,
                            bestBeta,
                            bestPhi
                        );
                    }
                }
            }
        }

        if (double.IsPositiveInfinity(minSse) || double.IsNaN(bestAlpha))
        {
            const string errorMsg =
                "Parameter optimization failed to find valid parameters. SSE remained undefined or infinite. "
                + "Check signal data quality (e.g., positivity for multiplicative), model type suitability, or optimization range.";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        if (!_options.DampTrend)
        {
            bestPhi = 1.0;
        }

        return (bestAlpha, bestBeta, bestPhi);
    }

    private T CalculateSseForParameters(ReadOnlySpan<T> signal, double alphaDouble, double betaDouble, double phiDouble)
    {
        T alpha = T.CreateChecked(alphaDouble);
        T beta = T.CreateChecked(betaDouble);
        T phi = T.CreateChecked(phiDouble);

        T sse = T.Zero;
        int signalLength = signal.Length;

        try
        {
            InitializeLevelAndTrend(signal, out var currentLevel, out var currentTrend);

            for (var t = 0; t < signalLength; t++)
            {
                var previousLevel = currentLevel;
                var previousTrend = currentTrend;
                var currentValue = signal[t];

                T forecast = CalculateOneStepForecast(previousLevel, previousTrend, phi);

                T error = currentValue - forecast;
                sse += error * error;

                if (T.IsNaN(sse) || T.IsInfinity(sse))
                {
                    _logger.LogWarning(
                        "SSE calculation became invalid (NaN/Infinity) at step {Time} for params A={Alpha:F4}, B={Beta:F4}, P={Phi:F4}",
                        t,
                        alphaDouble,
                        betaDouble,
                        phiDouble
                    );
                    return T.PositiveInfinity;
                }

                UpdateModelStateForSse(currentValue, ref currentLevel, ref currentTrend, previousLevel, previousTrend, t, alpha, beta, phi);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogTrace(ex, "SSE calculation failed during initialization for A={Alpha:F4}, B={Beta:F4}, P={Phi:F4}.", alphaDouble, betaDouble, phiDouble);
            return T.PositiveInfinity;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogTrace(ex, "SSE calculation failed during update steps for A={Alpha:F4}, B={Beta:F4}, P={Phi:F4}.", alphaDouble, betaDouble, phiDouble);
            return T.PositiveInfinity;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during SSE calculation for A={Alpha:F4}, B={Beta:F4}, P={Phi:F4}.", alphaDouble, betaDouble, phiDouble);
            return T.PositiveInfinity;
        }

        return sse;
    }

    private T CalculateOneStepForecast(T previousLevel, T previousTrend, T phi)
    {
        if (_options.TrendType == HoltMethodTrendType.Additive)
        {
            return previousLevel + phi * previousTrend;
        }

        if (previousLevel <= _epsilon)
            throw new InvalidOperationException($"Cannot forecast: Multiplicative previous level ({Convert.ToDouble(previousLevel)}) is non-positive.");

        if (previousTrend <= _epsilon && !NumericUtils.AreApproximatelyEqual(phi, T.Zero, _epsilon))
            throw new InvalidOperationException(
                $"Cannot forecast: Multiplicative previous trend ({Convert.ToDouble(previousTrend)}) is non-positive and phi ({Convert.ToDouble(phi)}) is non-zero."
            );

        T trendFactor;
        if (previousTrend > _epsilon || NumericUtils.AreApproximatelyEqual(phi, T.Zero, _epsilon))
        {
            trendFactor = T.Pow(previousTrend, phi);
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot forecast: Multiplicative previous trend ({Convert.ToDouble(previousTrend)}) is non-positive and phi ({Convert.ToDouble(phi)}) is non-zero."
            );
        }

        return previousLevel * trendFactor;
    }

    private void UpdateModelState(T currentValue, ref T currentLevel, ref T currentTrend, T previousLevel, T previousTrend, int timeStep)
    {
        UpdateModelStateInternal(
            currentValue,
            ref currentLevel,
            ref currentTrend,
            previousLevel,
            previousTrend,
            timeStep,
            _effectiveAlpha,
            _effectiveBeta,
            _effectivePhi
        );
    }

    private void UpdateModelStateForSse(
        T currentValue,
        ref T currentLevel,
        ref T currentTrend,
        T previousLevel,
        T previousTrend,
        int timeStep,
        T alpha,
        T beta,
        T phi
    )
    {
        UpdateModelStateInternal(currentValue, ref currentLevel, ref currentTrend, previousLevel, previousTrend, timeStep, alpha, beta, phi);
    }

    private void UpdateModelStateInternal(
        T currentValue,
        ref T currentLevel,
        ref T currentTrend,
        T previousLevel,
        T previousTrend,
        int timeStep,
        T alpha,
        T beta,
        T phi
    )
    {
        if (_options.TrendType == HoltMethodTrendType.Additive)
        {
            T levelTrendComponent = previousLevel + phi * previousTrend;
            currentLevel = alpha * currentValue + (T.One - alpha) * levelTrendComponent;
            currentTrend = beta * (currentLevel - previousLevel) + (T.One - beta) * phi * previousTrend;
        }
        else
        {
            if (previousLevel <= _epsilon)
                throw new InvalidOperationException(
                    $"Multiplicative update failed at step {timeStep}: Previous level ({Convert.ToDouble(previousLevel)}) is non-positive."
                );

            if (previousTrend <= _epsilon && !NumericUtils.AreApproximatelyEqual(phi, T.Zero, _epsilon))
                throw new InvalidOperationException(
                    $"Multiplicative update failed at step {timeStep}: Previous trend ({Convert.ToDouble(previousTrend)}) is non-positive and phi ({Convert.ToDouble(phi)}) is non-zero."
                );

            T trendPowerPhi;
            try
            {
                if (previousTrend > _epsilon || NumericUtils.AreApproximatelyEqual(phi, T.Zero, _epsilon))
                {
                    trendPowerPhi = T.Pow(previousTrend, phi);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot compute Trend^Phi: Trend ({Convert.ToDouble(previousTrend)}) is non-positive and Phi ({Convert.ToDouble(phi)}) is non-zero."
                    );
                }
            }
            catch (Exception ex) when (ex is OverflowException or NotFiniteNumberException)
            {
                throw new InvalidOperationException(
                    $"Error computing Trend^Phi ({Convert.ToDouble(previousTrend)}^{Convert.ToDouble(phi)}) at step {timeStep}.",
                    ex
                );
            }

            T levelTrendComponent = previousLevel * trendPowerPhi;
            currentLevel = alpha * currentValue + (T.One - alpha) * levelTrendComponent;

            if (currentLevel <= _epsilon)
                throw new InvalidOperationException(
                    $"Multiplicative update failed at step {timeStep}: Resulting level ({Convert.ToDouble(currentLevel)}) is non-positive."
                );

            if (T.Abs(previousLevel) < _epsilon)
                throw new InvalidOperationException(
                    $"Multiplicative update failed at step {timeStep}: Previous level for ratio ({Convert.ToDouble(previousLevel)}) is too close to zero."
                );

            T levelRatio = currentLevel / previousLevel;
            currentTrend = beta * levelRatio + (T.One - beta) * trendPowerPhi;

            if (currentTrend <= _epsilon)
                throw new InvalidOperationException(
                    $"Multiplicative update failed at step {timeStep}: Resulting trend ({Convert.ToDouble(currentTrend)}) is non-positive."
                );
        }
    }

    /// <inheritdoc />
    public T[] Extrapolate(int horizon)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(horizon, 1, nameof(horizon));
        UninitializedDataException.ThrowIfFalse(_isFitted, "Fit() must be called before Extrapolate().");

        var forecast = new T[horizon];
        _logger.LogDebug(
            "Extrapolating {Horizon} steps from Level={Level:G4}, Trend={Trend:G4} (Phi={Phi:F4})",
            horizon,
            Convert.ToDouble(_lastLevel),
            Convert.ToDouble(_lastTrend),
            Convert.ToDouble(_effectivePhi)
        );

        try
        {
            if (_options.TrendType == HoltMethodTrendType.Additive)
            {
                ExtrapolateAdditive(forecast, horizon);
            }
            else
            {
                ExtrapolateMultiplicative(forecast, horizon);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Extrapolation failed due to invalid fitted state or horizon.");
            throw;
        }
        catch (Exception ex) when (ex is OverflowException || ex is NotFiniteNumberException)
        {
            _logger.LogError(ex, "Numerical error occurred during extrapolation for horizon {Horizon}.", horizon);
            throw new InvalidOperationException("Extrapolation resulted in numerical error. Check fitted parameters and horizon.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during extrapolation calculation for horizon {Horizon}.", horizon);
            throw new InvalidOperationException("Unexpected error during extrapolation. Check state and inputs.", ex);
        }

        _logger.LogTrace("Generated forecast: [{ForecastValues}]", string.Join(", ", forecast.Select(v => Convert.ToDouble(v).ToString("G4"))));
        return forecast;
    }

    private void ExtrapolateAdditive(T[] forecast, int horizon)
    {
        T phiSumPowers = T.Zero;

        for (var h = 1; h <= horizon; h++)
        {
            T hAsT = T.CreateChecked(h);
            if (NumericUtils.AreApproximatelyEqual(_effectivePhi, T.One, _epsilon))
            {
                phiSumPowers = hAsT;
            }
            else
            {
                phiSumPowers += T.Pow(_effectivePhi, hAsT);
            }

            forecast[h - 1] = _lastLevel + phiSumPowers * _lastTrend;
        }
    }

    private void ExtrapolateMultiplicative(T[] forecast, int horizon)
    {
        if (_lastLevel <= _epsilon)
            throw new InvalidOperationException($"Cannot extrapolate: Multiplicative fitted level ({Convert.ToDouble(_lastLevel)}) is non-positive.");

        if (_lastTrend <= _epsilon && !NumericUtils.AreApproximatelyEqual(_effectivePhi, T.Zero, _epsilon))
            throw new InvalidOperationException(
                $"Cannot extrapolate: Multiplicative fitted trend ({Convert.ToDouble(_lastTrend)}) is non-positive and effective phi ({Convert.ToDouble(_effectivePhi)}) is non-zero."
            );

        T phiSumPowers = T.Zero;

        for (var h = 1; h <= horizon; h++)
        {
            T hAsT = T.CreateChecked(h);
            if (NumericUtils.AreApproximatelyEqual(_effectivePhi, T.One, _epsilon))
            {
                phiSumPowers = hAsT;
            }
            else
            {
                phiSumPowers += T.Pow(_effectivePhi, hAsT);
            }

            if (_lastTrend <= _epsilon && !NumericUtils.AreApproximatelyEqual(phiSumPowers, T.Zero, _epsilon))
                throw new InvalidOperationException(
                    $"Cannot extrapolate at step {h}: Multiplicative fitted trend ({Convert.ToDouble(_lastTrend)}) is non-positive and forecast exponent ({Convert.ToDouble(phiSumPowers)}) is non-zero."
                );

            T trendFactor;
            try
            {
                if (_lastTrend > _epsilon || NumericUtils.AreApproximatelyEqual(phiSumPowers, T.Zero, _epsilon))
                {
                    trendFactor = T.Pow(_lastTrend, phiSumPowers);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot compute Trend^Exponent: Trend ({Convert.ToDouble(_lastTrend)}) is non-positive and exponent ({Convert.ToDouble(phiSumPowers)}) is non-zero."
                    );
                }
            }
            catch (Exception ex) when (ex is OverflowException || ex is NotFiniteNumberException)
            {
                throw new InvalidOperationException(
                    $"Error computing Trend^Exponent ({Convert.ToDouble(_lastTrend)}^{Convert.ToDouble(phiSumPowers)}) at forecast step {h}.",
                    ex
                );
            }

            forecast[h - 1] = _lastLevel * trendFactor;
        }
    }

    /// <inheritdoc />
    public T[] FitAndExtrapolate(ReadOnlySpan<T> signal, int horizon)
    {
        Fit(signal);
        return Extrapolate(horizon);
    }

    private void ValidateSignalForTrendType(ReadOnlySpan<T> signal)
    {
        if (_options.TrendType != HoltMethodTrendType.Multiplicative)
            return;

        for (int i = 0; i < signal.Length; i++)
        {
            if (signal[i] <= _epsilon)
            {
                var errorMessage =
                    $"Multiplicative trend requires strictly positive signal values (tolerance={Convert.ToDouble(_epsilon)}). Found {Convert.ToDouble(signal[i])} at index {i}.";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage, nameof(signal));
            }
        }
    }

    private void InitializeLevelAndTrend(ReadOnlySpan<T> signal, out T initialLevel, out T initialTrend)
    {
        initialLevel = _options.InitialLevel.HasValue ? T.CreateChecked(_options.InitialLevel.Value) : signal[0];
        _logger.LogTrace(
            "Initial level set to {InitialLevel:G4} (Source: {Source})",
            Convert.ToDouble(initialLevel),
            _options.InitialLevel.HasValue ? "Options" : "Signal[0]"
        );

        if (_options.TrendType == HoltMethodTrendType.Multiplicative && initialLevel <= _epsilon)
        {
            var source = _options.InitialLevel.HasValue ? "options" : "Signal[0]";
            var errorMsg =
                $"Initial level ({Convert.ToDouble(initialLevel)}) from {source} must be strictly positive for Multiplicative trend (tolerance={Convert.ToDouble(_epsilon)}).";
            _logger.LogError(errorMsg);
            throw new ArgumentException(errorMsg, _options.InitialLevel.HasValue ? nameof(_options.InitialLevel) : nameof(signal));
        }

        initialTrend =
            _options.TrendType == HoltMethodTrendType.Additive ? InitializeAdditiveTrend(signal) : InitializeMultiplicativeTrend(signal, initialLevel);
    }

    private T InitializeAdditiveTrend(ReadOnlySpan<T> signal)
    {
        var initialTrend = _options.InitialTrend.HasValue ? T.CreateChecked(_options.InitialTrend.Value) : (signal[1] - signal[0]);
        _logger.LogTrace(
            "Initial additive trend set to {InitialTrend:G4} (Source: {Source})",
            Convert.ToDouble(initialTrend),
            _options.InitialTrend.HasValue ? "Options" : "Signal[1]-Signal[0]"
        );

        return initialTrend;
    }

    private T InitializeMultiplicativeTrend(ReadOnlySpan<T> signal, T initialLevel)
    {
        T initialTrend;

        if (_options.InitialTrend.HasValue)
        {
            initialTrend = T.CreateChecked(_options.InitialTrend.Value);
            _logger.LogTrace("Using provided initial multiplicative trend: {InitialTrend:G4}", Convert.ToDouble(initialTrend));

            if (initialTrend <= _epsilon)
            {
                var errorMsg =
                    $"Provided initial trend ({Convert.ToDouble(initialTrend)}) must be strictly positive for Multiplicative trend (tolerance={Convert.ToDouble(_epsilon)}).";
                _logger.LogError(errorMsg);
                throw new ArgumentException(errorMsg, nameof(_options.InitialTrend));
            }
        }
        else
        {
            Debug.Assert(initialLevel > _epsilon, "Initial level should be positive here.");
            Debug.Assert(signal[1] > _epsilon, "Signal[1] should be positive here.");

            if (T.Abs(initialLevel) < _epsilon)
                throw new ArgumentException(
                    $"Initial level ({Convert.ToDouble(initialLevel)}) for multiplicative trend calculation is too close to zero.",
                    nameof(signal)
                );

            initialTrend = signal[1] / initialLevel;
            _logger.LogTrace("Estimated initial multiplicative trend: {InitialTrend:G4} (From Signal[1] / InitialLevel)", Convert.ToDouble(initialTrend));

            if (initialTrend <= _epsilon)
            {
                var errorMsg =
                    $"Estimated initial multiplicative trend ({Convert.ToDouble(initialTrend)}) from Signal[1]={Convert.ToDouble(signal[1])} / InitialLevel={Convert.ToDouble(initialLevel)} is non-positive (tolerance={Convert.ToDouble(_epsilon)}). Consider providing InitialTrend or check data.";
                _logger.LogError(errorMsg);
                throw new ArgumentException(errorMsg, nameof(signal));
            }
        }

        return initialTrend;
    }
}
