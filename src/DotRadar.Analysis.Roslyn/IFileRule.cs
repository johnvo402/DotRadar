using DotRadar.Abstractions;

namespace DotRadar.Analysis.Roslyn;

public interface IFileRule : IDotRadarRule
{
    bool CanAnalyze(string filePath);

    ValueTask<IReadOnlyList<DotRadarDiagnostic>> AnalyzeAsync(
        FileAnalysisContext context,
        CancellationToken cancellationToken);
}