namespace DotRadar.Core;

public sealed class DotRadarBaseline
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public List<DotRadarBaselineEntry> Diagnostics { get; set; } = [];
}

public sealed class DotRadarBaselineEntry
{
    public string Fingerprint { get; set; } = string.Empty;

    public string RuleId { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public int Line { get; set; }

    public int Column { get; set; }
}