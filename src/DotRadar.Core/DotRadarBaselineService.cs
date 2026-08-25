using DotRadar.Abstractions;

namespace DotRadar.Core;

public static class DotRadarBaselineService
{
    public static DotRadarBaseline Create(
        IReadOnlyList<DotRadarDiagnostic> diagnostics,
        string baseDirectory)
    {
        var generator =
            new DiagnosticFingerprintGenerator(baseDirectory);

        var entries = diagnostics
            .Select(diagnostic => new DotRadarBaselineEntry
            {
                Fingerprint = generator.Create(diagnostic),
                RuleId = diagnostic.RuleId,

                FilePath = generator.GetRelativeFilePath(
                    diagnostic.FilePath),

                Line = diagnostic.Line,
                Column = diagnostic.Column
            })
            .GroupBy(entry => entry.Fingerprint)
            .Select(group => group.First())
            .OrderBy(entry => entry.RuleId)
            .ThenBy(entry => entry.FilePath)
            .ThenBy(entry => entry.Line)
            .ToList();

        return new DotRadarBaseline
        {
            Version = DotRadarBaseline.CurrentVersion,
            Diagnostics = entries
        };
    }

    public static IReadOnlyList<DotRadarDiagnostic> Filter(
        IReadOnlyList<DotRadarDiagnostic> diagnostics,
        DotRadarBaseline baseline,
        string baseDirectory)
    {
        var knownFingerprints = baseline.Diagnostics
            .Select(entry => entry.Fingerprint)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var generator =
            new DiagnosticFingerprintGenerator(baseDirectory);

        return diagnostics
            .Where(diagnostic =>
                !knownFingerprints.Contains(
                    generator.Create(diagnostic)))
            .ToArray();
    }
}