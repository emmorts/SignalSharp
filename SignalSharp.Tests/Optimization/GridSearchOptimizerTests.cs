using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SignalSharp.Optimization;
using SignalSharp.Optimization.GridSearch;

namespace SignalSharp.Tests.Optimization;

[TestFixture]
public class GridSearchOptimizerTests
{
    private const double Tolerance = 1e-9;
    private const double ParamTolerance = 1e-4;

    private TestInput _testInputData;
    private ILogger<GridSearchOptimizer<TestInput, double>> _logger;

    public class TestInput;

    [SetUp]
    public void SetUp()
    {
        _testInputData = new TestInput();
        _logger = NullLogger<GridSearchOptimizer<TestInput, double>>.Instance;
    }

    #region Constructor Tests

    [Test]
    public void Constructor_DefaultOptions_Initializes()
    {
        Assert.DoesNotThrow(() => new GridSearchOptimizer<TestInput, double>());
    }

    [Test]
    public void Constructor_WithOptions_Initializes()
    {
        var options = new GridSearchOptimizerOptions();
        Assert.DoesNotThrow(() => new GridSearchOptimizer<TestInput, double>(options));
    }

    #endregion

    #region Basic Functionality Tests

    [Test]
    public async Task OptimizeAsync_NoParameters_ReturnsFailure()
    {
        var optimizer = new GridSearchOptimizer<TestInput, double>();
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
            Assert.That(result.FunctionEvaluations, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task OptimizeAsync_1D_Quadratic_FindsMinimum()
    {
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 11 };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: 2.0),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(Tolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(2.0).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.EqualTo(11));
        }
    }

    [Test]
    public async Task OptimizeAsync_2D_Quadratic_FindsMinimum_Sequential()
    {
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 6, EnableParallelProcessing = false };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: 2.0, targetY: 3.0),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(Tolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(2.0).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(3.0).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.EqualTo(36));
        }
    }

    [Test]
    public async Task OptimizeAsync_2D_Quadratic_FindsMinimum_Parallel()
    {
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 6, EnableParallelProcessing = true };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: 2.0, targetY: 3.0),
            CancellationToken.None
        );

        Assert.That(result.Success, Is.True);
        Assert.That(result.MinimizedMetric, Is.EqualTo(0.0).Within(Tolerance));
        Assert.That(result.BestParameters["x"], Is.EqualTo(2.0).Within(ParamTolerance));
        Assert.That(result.BestParameters["y"], Is.EqualTo(3.0).Within(ParamTolerance));
        Assert.That(result.FunctionEvaluations, Is.EqualTo(36));
    }

    [Test]
    public async Task OptimizeAsync_AllEvaluationsFail_ReturnsFailureResult()
    {
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 3 };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 1.0) };

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, ObjectiveFunctions.ThrowingFunction, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(double.IsPositiveInfinity(result.MinimizedMetric), Is.True);
            Assert.That(result.BestParameters, Is.Empty);
            Assert.That(result.FunctionEvaluations, Is.EqualTo(3));
            Assert.That(result.Message, Does.Contain("Grid search optimization failed to find any valid parameters"));
        }
    }

    #endregion

    #region Options Tests

    [Test]
    public async Task OptimizeAsync_PerParameterGridSteps_OverridesDefault()
    {
        var options = new GridSearchOptimizerOptions
        {
            DefaultGridSteps = 10,
            PerParameterGridSteps = new Dictionary<string, int> { { "x", 3 }, { "y", 6 } },
        };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: 2.0, targetY: 3.0),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.EqualTo(0.25).Within(Tolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(2.5).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(3.0).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.EqualTo(18));
        }
    }

    [Test]
    public async Task OptimizeAsync_MaxFunctionEvaluations_LimitsEvaluations()
    {
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 6, MaxFunctionEvaluations = 10 };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: 2.0, targetY: 3.0),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FunctionEvaluations, Is.EqualTo(10));
            Assert.That(result.BestParameters.ContainsKey("x"), Is.True);
            Assert.That(result.BestParameters.ContainsKey("y"), Is.True);
        }
    }

    [Test]
    public async Task OptimizeAsync_EarlyStopping_StopsWhenThresholdReached()
    {
        var options = new GridSearchOptimizerOptions
        {
            DefaultGridSteps = 6,
            EarlyStoppingThreshold = 0.1,
            EnableParallelProcessing = false,
        };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic2D(data, p, targetX: 2.0, targetY: 2.0),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.Zero.Within(Tolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(2.0).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(2.0).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.LessThan(36));
            Assert.That(result.Message, Does.Contain("Grid search completed early due to reaching threshold."));
        }
    }

    #endregion

    #region Scaling Tests

    [Test]
    public async Task OptimizeAsync_LogarithmicScaling_ImprovesPrecisionForSmallValues()
    {
        var parameters = new List<ParameterDefinition> { new("x", 0.001, 1.0) };
        var objectiveFunc = (TestInput data, IReadOnlyDictionary<string, double> p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: 0.01);

        // Linear scaling
        var linearOptions = new GridSearchOptimizerOptions { DefaultGridSteps = 5 };
        var linearOptimizer = new GridSearchOptimizer<TestInput, double>(linearOptions, _logger);
        var linearResult = await linearOptimizer.OptimizeAsync(_testInputData, parameters, objectiveFunc, CancellationToken.None);

        // Logarithmic scaling
        var logOptions = new GridSearchOptimizerOptions
        {
            DefaultGridSteps = 5,
            UseLogarithmicScaleFor = new HashSet<string> { "x" },
        };
        var logOptimizer = new GridSearchOptimizer<TestInput, double>(logOptions, _logger);
        var logResult = await logOptimizer.OptimizeAsync(_testInputData, parameters, objectiveFunc, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Log scaling should get closer to the 0.01 target
            Assert.That(logResult.MinimizedMetric, Is.LessThan(linearResult.MinimizedMetric));
            Assert.That(Math.Abs(logResult.BestParameters["x"] - 0.01), Is.LessThan(Math.Abs(linearResult.BestParameters["x"] - 0.01)));
        }
    }

    [Test]
    public async Task OptimizeAsync_LogScaleWithInvalidBounds_FallsBackToLinear()
    {
        var options = new GridSearchOptimizerOptions
        {
            DefaultGridSteps = 5,
            UseLogarithmicScaleFor = new HashSet<string> { "x" },
        };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 1.0) }; // MinValue of 0 is invalid for log
        var objectiveFunc = (TestInput data, IReadOnlyDictionary<string, double> p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: 0.01);

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, objectiveFunc, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.BestParameters["x"], Is.Zero.Within(ParamTolerance));
            Assert.That(result.MinimizedMetric, Is.EqualTo(0.0001).Within(Tolerance));
        }
    }

    #endregion

    #region Refinement Tests

    [Test]
    public async Task OptimizeAsync_AdaptiveRefinement_ImprovesSolution()
    {
        const double targetX = 2.25;
        const double targetY = 3.25;
        var objectiveFunc = (TestInput data, IReadOnlyDictionary<string, double> p) =>
            ObjectiveFunctions.Quadratic2D(data, p, targetX: targetX, targetY: targetY);

        var options = new GridSearchOptimizerOptions
        {
            DefaultGridSteps = 6,
            EnableAdaptiveRefinement = true,
            RefinementRangeFactor = 0.2,
            RefinementGridSteps = 5,
            MaxFunctionEvaluations = 100,
        };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, objectiveFunc, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(targetY).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.EqualTo(36 + 25));
            Assert.That(result.Message, Does.Contain("Grid search with adaptive refinement completed successfully."));
        }
    }

    [Test]
    public async Task OptimizeAsync_AdaptiveRefinement_DoesNotImproveIfAlreadyOptimal()
    {
        const double targetX = 2.25;
        const double targetY = 3.25;
        var objectiveFunc = (TestInput data, IReadOnlyDictionary<string, double> p) =>
            ObjectiveFunctions.Quadratic2D(data, p, targetX: targetX, targetY: targetY);

        var options = new GridSearchOptimizerOptions
        {
            DefaultGridSteps = 21, // Initial grid includes the exact optimum points
            EnableAdaptiveRefinement = true,
            RefinementRangeFactor = 0.2,
            RefinementGridSteps = 5,
            MaxFunctionEvaluations = 21 * 21 + 5 * 5 + 10,
        };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, objectiveFunc, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(targetY).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.EqualTo(21 * 21 + 5 * 5));
            Assert.That(result.Message, Does.Contain("Grid search completed successfully."));
            Assert.That(result.Message, Does.Not.Contain("adaptive refinement completed successfully"));
        }
    }

    [Test]
    public async Task OptimizeAsync_AdaptiveRefinement_InsufficientBudget_SkipsRefinement()
    {
        const double targetX = 2.25;
        const double targetY = 3.25;
        var objectiveFunc = (TestInput data, IReadOnlyDictionary<string, double> p) =>
            ObjectiveFunctions.Quadratic2D(data, p, targetX: targetX, targetY: targetY);

        var options = new GridSearchOptimizerOptions
        {
            DefaultGridSteps = 6,
            EnableAdaptiveRefinement = true,
            RefinementRangeFactor = 0.2,
            RefinementGridSteps = 5,
            MaxFunctionEvaluations = 41, // Not enough for refinement after initial search
        };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(_testInputData, parameters, objectiveFunc, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.EqualTo(0.125).Within(Tolerance));
            Assert.That(result.BestParameters["x"], Is.EqualTo(2.0).Within(ParamTolerance));
            Assert.That(result.BestParameters["y"], Is.EqualTo(3.0).Within(ParamTolerance));
            Assert.That(result.FunctionEvaluations, Is.EqualTo(36));
            Assert.That(result.Message, Does.Not.Contain("adaptive refinement"));
        }
    }

    #endregion

    #region Boundary Tests

    [Test]
    public async Task OptimizeAsync_OptimumAtBoundary_IncludesWarningInResult()
    {
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 6 };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: 0.0),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BestParameters["x"], Is.Zero.Within(ParamTolerance));
            Assert.That(result.Message, Does.Contain("Warning: The following parameters are at or near their bounds"));
            Assert.That(result.Message, Does.Contain("x (at lower bound, distance: 0.00"));
        }
    }

    [Test]
    public async Task OptimizeAsync_OptimumNearBoundary_IncludesWarningInResult()
    {
        const double targetX = 0.005;
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 201 };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 1.0) };

        var result = await optimizer.OptimizeAsync(
            _testInputData,
            parameters,
            (data, p) => ObjectiveFunctions.Quadratic1D(data, p, targetX: targetX),
            CancellationToken.None
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BestParameters["x"], Is.EqualTo(targetX).Within(ParamTolerance));
            Assert.That(result.Message, Does.Contain("Warning: The following parameters are at or near their bounds"));
            Assert.That(result.Message, Does.Contain("x (at lower bound"));
        }
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public void OptimizeAsync_CancellationRequested_TerminatesGracefully()
    {
        var options = new GridSearchOptimizerOptions { DefaultGridSteps = 100, EnableParallelProcessing = true };
        var optimizer = new GridSearchOptimizer<TestInput, double>(options, _logger);
        var parameters = new List<ParameterDefinition> { new("x", 0.0, 5.0), new("y", 0.0, 5.0) };

        var cts = new CancellationTokenSource();
        int evaluationCount = 0;

        Func<TestInput, IReadOnlyDictionary<string, double>, ObjectiveEvaluation<double>> objectiveFuncWithCancel = (data, p) =>
        {
            Thread.Sleep(1);

            Interlocked.Increment(ref evaluationCount);
            if (evaluationCount > 20)
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            }
            return ObjectiveFunctions.Quadratic2D(data, p, targetX: 2.0, targetY: 2.0);
        };

        OptimizationResult<double> result = default;
        Assert.DoesNotThrowAsync(async () => result = await optimizer.OptimizeAsync(_testInputData, parameters, objectiveFuncWithCancel, cts.Token));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.MinimizedMetric, Is.Not.EqualTo(double.PositiveInfinity));
            Assert.That(result.FunctionEvaluations, Is.GreaterThan(0));
            Assert.That(result.FunctionEvaluations, Is.LessThan(options.DefaultGridSteps * options.DefaultGridSteps));
        }
    }

    #endregion
}
