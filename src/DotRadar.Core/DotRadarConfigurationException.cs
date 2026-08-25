namespace DotRadar.Core;

public sealed class DotRadarConfigurationException : Exception
{
    public DotRadarConfigurationException(string message)
        : base(message)
    {
    }

    public DotRadarConfigurationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}