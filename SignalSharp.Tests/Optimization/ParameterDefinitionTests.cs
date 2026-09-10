using SignalSharp.Optimization;
using SignalSharp.Optimization.GridSearch;

namespace SignalSharp.Tests.Optimization;

[TestFixture]
public class ParameterDefinitionTests
{
    [Test]
    public void Constructor_ValidDefinition_Succeeds()
    {
        var parameter = new ParameterDefinition("x", -5.0, 5.0, InitialGuess: 0.0);

        Assert.That(parameter.Name, Is.EqualTo("x"));
        Assert.That(parameter.MinValue, Is.EqualTo(-5.0));
        Assert.That(parameter.MaxValue, Is.EqualTo(5.0));
        Assert.That(parameter.InitialGuess, Is.EqualTo(0.0));
    }

    [Test]
    public void Constructor_MinGreaterThanMax_Throws()
    {
        Assert.That(() => new ParameterDefinition("x", 5.0, 1.0), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_EqualBounds_IsAllowed()
    {
        // Fixed parameters are expressed as a zero-range definition.
        Assert.That(() => new ParameterDefinition("x", 2.0, 2.0, InitialGuess: 2.0), Throws.Nothing);
    }

    [Test]
    public void Constructor_EmptyName_Throws()
    {
        Assert.That(() => new ParameterDefinition(" ", 0.0, 1.0), Throws.ArgumentException);
    }

    [Test]
    public void Constructor_NonFiniteBounds_Throws()
    {
        Assert.That(() => new ParameterDefinition("x", double.NaN, 1.0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => new ParameterDefinition("x", 0.0, double.PositiveInfinity), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_NonFiniteInitialGuess_Throws()
    {
        Assert.That(() => new ParameterDefinition("x", 0.0, 1.0, InitialGuess: double.NaN), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void WithExpression_ProducingInvalidBounds_IsRejectedWhenUsed()
    {
        var parameter = new ParameterDefinition("x", 0.0, 1.0) with { MinValue = 5.0 };
        var optimizer = new GridSearchOptimizer<object, double>();

        Assert.That(
            () => optimizer.Optimize(new object(), [parameter], (_, _) => new ObjectiveEvaluation<double>(0.0)),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
        );
    }
}
