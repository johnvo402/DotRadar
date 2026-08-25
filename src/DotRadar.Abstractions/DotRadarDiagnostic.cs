namespace DotRadar.Abstractions;

public enum DotRadarSeverity
{
    Info,
    Warning,
    Error
}

public sealed class DotRadarDiagnostic
{
    public DotRadarDiagnostic(
        string ruleId,
        string title,
        string message,
        DotRadarSeverity severity,
        string filePath,
        int line,
        int column)
    {
        RuleId = ruleId;
        Title = title;
        Message = message;
        Severity = severity;
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    public string RuleId { get; }

    public string Title { get; }

    public string Message { get; }

    public DotRadarSeverity Severity { get; }

    public string FilePath { get; }

    public int Line { get; }

    public int Column { get; }
}