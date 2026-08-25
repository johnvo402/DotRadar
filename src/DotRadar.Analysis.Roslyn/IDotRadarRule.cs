using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;

namespace DotRadar.Analysis.Roslyn;

public interface IDotRadarRule
{
    string RuleId { get; }

    DotRadarRuleDescriptor Descriptor { get; }

    Task<IReadOnlyList<DotRadarDiagnostic>> AnalyzeAsync(
        Document document,
        CancellationToken cancellationToken);
}