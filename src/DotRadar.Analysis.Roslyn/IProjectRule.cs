using DotRadar.Abstractions;

namespace DotRadar.Analysis.Roslyn;

public interface IProjectRule : IDotRadarRule
{
    ValueTask<IReadOnlyList<DotRadarDiagnostic>> AnalyzeAsync(
        ProjectAnalysisContext context,
        CancellationToken cancellationToken);
}