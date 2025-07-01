using System.Numerics;

namespace SignalSharp.Optimization.GridSearch;

internal class GridSearchState<T>
    where T : IFloatingPointIeee754<T>
{
    public Dictionary<string, double> BestParams { get; set; } = new();
    public T MinMetric { get; set; } = T.PositiveInfinity;
    public int FunctionEvaluations { get; set; } = 0;
    public bool EarlyStop { get; set; } = false;
}
