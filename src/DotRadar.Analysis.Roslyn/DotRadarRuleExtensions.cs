using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;

namespace DotRadar.Analysis.Roslyn;

public static class DotRadarRuleExtensions
{
    public static async Task<
        IReadOnlyList<DotRadarDiagnostic>> AnalyzeAsync(
        this IDocumentRule rule,
        Document document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(document);

        var context =
            await DocumentAnalysisContext.CreateAsync(
                document,
                cancellationToken);

        if (context is null)
        {
            return [];
        }

        return await rule.AnalyzeAsync(
            context,
            cancellationToken);
    }
}