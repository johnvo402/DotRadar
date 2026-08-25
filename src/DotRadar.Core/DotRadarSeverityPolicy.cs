using DotRadar.Abstractions;

namespace DotRadar.Core;

public static class DotRadarSeverityPolicy
{
    public static bool MeetsThreshold(
        DotRadarSeverity severity,
        DotRadarSeverity threshold)
    {
        return GetRank(severity) >= GetRank(threshold);
    }

    private static int GetRank(DotRadarSeverity severity)
    {
        return severity switch
        {
            DotRadarSeverity.Info => 0,
            DotRadarSeverity.Warning => 1,
            DotRadarSeverity.Error => 2,

            _ => throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                "Unsupported diagnostic severity.")
        };
    }
}