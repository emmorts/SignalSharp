using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SignalSharp.Optimization;
using SignalSharp.Optimization.NelderMead;

namespace SignalSharp.Tests.Optimization;

[TestFixture]
public class NelderMeadOptimizerTests
{
    private const double MetricTolerance = 1e-2;
    private const double ParamTolerance = 1e-1;

    private GridSearchOptimizerTests.TestInput _testInputData = null!;
    private Random _seededRandom = null!;
    private ILogger<NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>> _logger;

    [SetUp]
    public void SetUp()
    {
        _testInputData = new GridSearchOptimizerTests.TestInput();
        _logger = NullLogger<NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>>.Instance;
        _seededRandom = new Random(314158);
        // _logger = TestOutputLogger.Create<NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>>(TestContext.Progress);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_DefaultOptions_Initializes()
    {
        Assert.DoesNotThrow(() => new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(random: _seededRandom));
    }

    [Test]
    public void Constructor_WithOptions_Initializes()
    {
        var options = new NelderMeadOptimizerOptions();
        Assert.DoesNotThrow(() => new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, random: _seededRandom));
    }

    #endregion

    #region Basic Functionality Tests

    [Test]
    public async Task OptimizeAsync_NoParameters_ReturnsFailure()
    {
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(logger: _logger, random: _seededRandom);
        var parametersToOptimize = Enumerable.Empty<ParameterDefinition>();

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parametersToOptimize,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("No parameters to optimize."));
            Assert.That(double.IsNaN(result.MinimizedMetric), Is.True);
            Assert.That(result.BestParameters, Is.Empty);
            Assert.That(result.FunctionEvaluations, Is.Zero);
        }
    }

    [Test]
    public async Task OptimizeAsync_1D_Quadratic_FindsMinimum()
    {
        var options = new NelderMeadOptimizerOptions { FunctionValueConvergenceTolerance = 1e-8, ParameterConvergenceTolerance = 1e-5 };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", -5.0, 5.0, InitialGuess: 0.0) };
        const double targetX = 2.5;

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: targetX),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task OptimizeAsync_2D_Quadratic_FindsMinimum()
    {
        var options = new NelderMeadOptimizerOptions
        {
            FunctionValueConvergenceTolerance = 1e-8,
            ParameterConvergenceTolerance = 1e-5,
            MaxIterations = 200,
        };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", -5.0, 5.0, InitialGuess: 0.0), new("y", -5.0, 5.0, InitialGuess: 0.0) };
        const double targetX = 2.0;
        const double targetY = -1.0;

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: targetX, targetY: targetY),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(targetY).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task OptimizeAsync_Rosenbrock_FindsMinimum()
    {
        var options = new NelderMeadOptimizerOptions
        {
            FunctionValueConvergenceTolerance = 1e-8,
            ParameterConvergenceTolerance = 1e-4,
            MaxIterations = 500, // Rosenbrock can be tricky
        };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition>
        {
            // Rosenbrock min is at (1,1)
            new("x", -2.0, 2.0, InitialGuess: -1.2),
            new("y", -1.0, 3.0, InitialGuess: 1.0),
        };

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, ObjectiveFunctions.Rosenbrock, CancellationToken.None);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(1.0).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(1.0).Within(ParamTolerance));
        }
    }

    [Test]
    public async Task OptimizeAsync_ObjectiveFunctionThrows_HandlesGracefully()
    {
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(logger: _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 1.0, InitialGuess: 0.5) };

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, ObjectiveFunctions.ThrowingFunction, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // the best point might be the initial guess if all evaluations throw
            // or it might be NaN/Infinity if initialization itself fails badly
            Assert.That(double.IsPositiveInfinity(result.MinimizedMetric), Is.True, "MinimizedMetric should be PositiveInfinity if all evaluations fail.");
            Assert.That(result.FunctionEvaluations, Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task OptimizeAsync_FunctionReturnsNaN_HandlesAndAvoidsNaN()
    {
        var options = new NelderMeadOptimizerOptions { FunctionValueConvergenceTolerance = 1e-7, ParameterConvergenceTolerance = 1e-4 };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        // target is 2.0; if x < 0, function returns NaN
        var parameters = new List<ParameterDefinition> { new("x", -1.0, 3.0, InitialGuess: 0.5) };

        var objectiveFunc = (GridSearchOptimizerTests.TestInput data, IReadOnlyDictionary<string, double> p) =>
            ObjectiveFunctions.Quadratic1D_WithNaN(data, p, targetX: 2D);

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, objectiveFunc, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(2.0).Within(ParamTolerance));
            Assert.That(result.BestParameters["x"], Is.GreaterThanOrEqualTo(0.0));
        }
    }

    #endregion

    #region Options Tests

    [Test]
    public async Task OptimizeAsync_MaxIterations_StopsOptimization()
    {
        var options = new NelderMeadOptimizerOptions { MaxIterations = 10 };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", -100.0, 100.0, InitialGuess: 0.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: 90), // far target, won't converge in 10 iter
            CancellationToken.None
        );

        Assert.That(result.Iterations, Is.LessThanOrEqualTo(options.MaxIterations));
        if (result.Iterations == options.MaxIterations)
        {
            Assert.That(result.Message, Does.Contain("Reached maximum iterations").Or.Contain("Single optimization run completed."));
        }
    }

    [Test]
    public async Task OptimizeAsync_MaxFunctionEvaluations_StopsOptimization()
    {
        var options = new NelderMeadOptimizerOptions { MaxFunctionEvaluations = 15 };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", -5.0, 5.0, InitialGuess: 0.0), new("y", -5.0, 5.0, InitialGuess: 0.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: 4.0, targetY: 4.0),
            CancellationToken.None
        );

        Assert.That(result.FunctionEvaluations, Is.LessThanOrEqualTo(options.MaxFunctionEvaluations));
        if (result.FunctionEvaluations == options.MaxFunctionEvaluations)
        {
            Assert.That(result.Message, Does.Contain("Reached maximum function evaluations"));
        }
    }

    [Test]
    public async Task OptimizeAsync_MultiStart_AttemptsRestartsOnStagnation()
    {
        int objectiveCallCount = 0;
        Func<GridSearchOptimizerTests.TestInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<double>> stagnatingFunction = (data, p) =>
        {
            Interlocked.Increment(ref objectiveCallCount);
            // stagnate for the first few calls, then switch to the real function
            // a single run that stagnates will take ~5 calls, we set the threshold below that
            if (objectiveCallCount < 10)
                return new ObjectiveEvaluation<double>(100.0);
            return ObjectiveFunctions.Quadratic1D(data, p, targetX: 1.0);
        };

        var options = new NelderMeadOptimizerOptions
        {
            EnableMultiStart = true,
            MaxRestarts = 1,
            StagnationThresholdCount = 3,
            FunctionValueConvergenceTolerance = 1e-12,
            ParameterConvergenceTolerance = 1e-12,
            MaxIterations = 200,
        };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", -2.0, 2.0, InitialGuess: 0.0) };

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, stagnatingFunction, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance)); // should find the true minimum after restart
            Assert.That(result.BestParameters["x"], Is.EqualTo(1.0).Within(ParamTolerance));
            Assert.That(objectiveCallCount, Is.GreaterThan(10), "Should have gone past the initial stagnating calls");
        }
    }

    [Test]
    public async Task OptimizeAsync_AdaptiveParameters_RunsSuccessfully()
    {
        var options = new NelderMeadOptimizerOptions
        {
            EnableAdaptiveParameters = true,
            FunctionValueConvergenceTolerance = 1e-8,
            ParameterConvergenceTolerance = 1e-5,
        };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", -5.0, 5.0, InitialGuess: 0.0) };
        const double targetX = 3.0;

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: targetX),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
        }
    }

    #endregion

    #region Boundary and Clamping Tests

    [Test]
    public async Task OptimizeAsync_OptimumAtBoundary_FindsAndWarns()
    {
        var options = new NelderMeadOptimizerOptions { FunctionValueConvergenceTolerance = 1e-8 };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0, InitialGuess: 2.5) };
        const double targetX = 0.0; // optimum is at the lower bound

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: targetX),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.Message, Does.Contain("Warning: The following parameters are at or near their bounds"));
            Assert.That(result.Message, Does.Contain($"{parameters[0].Name} (near lower bound"));
        }
    }

    [Test]
    public async Task OptimizeAsync_OptimumNearBoundary_FindsAndWarns()
    {
        var options = new NelderMeadOptimizerOptions { FunctionValueConvergenceTolerance = 1e-8, ParameterConvergenceTolerance = 1e-5 };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0, InitialGuess: 2.5) };
        // target is 0.001, which is 0.001/5.0 = 0.02% of range from boundary
        // boundary proximity threshold is 1%.
        const double targetX = 0.001;

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: targetX),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.EqualTo(0.0).Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.Message, Does.Contain("Warning: The following parameters are at or near their bounds"));
            Assert.That(result.Message, Does.Contain($"{parameters[0].Name} (near lower bound"));
        }
    }

    [Test]
    public async Task OptimizeAsync_ParameterWithZeroRange_HandlesCorrectly()
    {
        var options = new NelderMeadOptimizerOptions();
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition>
        {
            new("x", -5.0, 5.0, InitialGuess: 0.0), // this one will be optimized
            new("y", 2.0, 2.0, InitialGuess: 2.0), // this one is fixed
        };
        const double targetX = 3.0;
        const double fixedY = 2.0;

        // objective function where y is fixed; metric depends only on x
        // (y-fixedY)^2 term should be 0
        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: targetX, targetY: fixedY),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(MetricTolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(fixedY).Within(ParamTolerance)); // y should remain fixed
            // check for boundary warning if the fixed parameter also meets boundary criteria
            // if MinValue == MaxValue, it is technically at both bounds
            Assert.That(result.Message, Does.Contain("y (at bound of zero-range definition)"));
        }
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public void OptimizeAsync_CancellationRequested_TerminatesGracefully()
    {
        var options = new NelderMeadOptimizerOptions
        {
            MaxIterations = 10000, // large enough to ensure cancellation happens first
        };
        var optimizer = new NelderMeadOptimizer<GridSearchOptimizerTests.TestInput, double>(options, _logger, random: _seededRandom);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0, InitialGuess: 1.0), new("y", 0.0, 5.0, InitialGuess: 1.0) };

        var cts = new CancellationTokenSource();
        int evaluationCount = 0;

        Func<GridSearchOptimizerTests.TestInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<double>> objectiveFuncWithCancel = (data, p) =>
        {
            // short delay to allow cancellation to propagate
            // in a real scenario, objective function might be long-running.
            // for test stability, Thread.Sleep might be too flaky
            // instead, we rely on the optimizer checking CancellationToken between steps.
            if (Volatile.Read(ref evaluationCount) > 20) // arbitrary number of evaluations before cancelling
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            }

            Interlocked.Increment(ref evaluationCount);
            return ObjectiveFunctions.Rosenbrock(data, p);
        };

        OptimizationResult<double> result = null!;

        // measure execution time to ensure it's less than an uncancellable run
        var stopwatch = Stopwatch.StartNew();
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            result = await optimizer.OptimizeAsync(_testInputData, parameters, objectiveFuncWithCancel, cts.Token)
        );
        stopwatch.Stop();

        // even if it throws OperationCanceledException, some evaluations would have occurred
        // the exact number of evaluations before cancellation can vary
        Assert.That(Volatile.Read(ref evaluationCount), Is.GreaterThan(0));
        // assert that it didn't run to full completion (which would take much longer for Rosenbrock)
        // this is an indirect check; MaxIterations is high, so if it completed, it would have many evals
        Assert.That(Volatile.Read(ref evaluationCount), Is.LessThan(parameters.Count * 50)); // heuristic, much less than full run.
    }

    #endregion
}
