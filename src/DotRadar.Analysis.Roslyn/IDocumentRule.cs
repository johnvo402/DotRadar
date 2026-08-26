using DotRadar.Abstractions;

namespace DotRadar.Analysis.Roslyn;

public interface IDocumentRule : IDotRadarRule
{
    ValueTask<IReadOnlyList<DotRadarDiagnostic>> AnalyzeAsync(
        DocumentAnalysisContext context,
        CancellationToken cancellationToken);
}