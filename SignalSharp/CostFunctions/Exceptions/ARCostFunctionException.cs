namespace SignalSharp.CostFunctions.Exceptions;

public class ARCostFunctionException : Exception
{
    public ARCostFunctionException(string? message) : base(message)
    {
    }

    public ARCostFunctionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}