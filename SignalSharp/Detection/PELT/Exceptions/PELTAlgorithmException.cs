namespace SignalSharp.Detection.PELT.Exceptions;

public class PELTAlgorithmException : Exception
{
    public PELTAlgorithmException(string? message) : base(message)
    {
    }

    public PELTAlgorithmException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
