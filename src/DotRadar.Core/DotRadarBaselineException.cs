namespace DotRadar.Core;

public sealed class DotRadarBaselineException : Exception
{
    public DotRadarBaselineException(string message)
        : base(message)
    {
    }

    public DotRadarBaselineException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}