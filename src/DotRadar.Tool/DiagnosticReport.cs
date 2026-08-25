using DotRadar.Abstractions;
using DotRadar.Core;

namespace DotRadar.Tool;

internal sealed class DiagnosticReport
{
    public DiagnosticReport(
        IReadOnlyList<DotRadarDiagnostic> diagnostics,
        IReadOnlyList<DotRadarRuleDescriptor> rules,
        string baseDirectory,
        int suppressedCount,
        DotRadarSeverity failureThreshold)
    {
        Diagnostics = diagnostics;
        Rules = rules;
        BaseDirectory = baseDirectory;
        SuppressedCount = suppressedCount;
        FailureThreshold = failureThreshold;
    }

    public IReadOnlyList<DotRadarDiagnostic> Diagnostics { get; }

    public IReadOnlyList<DotRadarRuleDescriptor> Rules { get; }

    public string BaseDirectory { get; }

    public int SuppressedCount { get; }

    public DotRadarSeverity FailureThreshold { get; }

    public int FailureCount => Diagnostics.Count(
        diagnostic =>
            DotRadarSeverityPolicy.MeetsThreshold(
                diagnostic.Severity,
                FailureThreshold));
}