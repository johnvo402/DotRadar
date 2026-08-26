using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotRadar.Analysis.Roslyn;

public sealed class Dtr1001Rule : IDotRadarRule
{
    private const string DiagnosticId = "DTR1001";

    private const string Title =
        "Avoid creating HttpClient per operation";

    private static readonly DotRadarRuleDescriptor RuleDescriptor =
        new(
            ruleId: DiagnosticId,
            title: Title,
            description:
                "Detects HttpClient instances created inside methods, " +
                "local functions, accessors, or anonymous functions.",
            category: DotRadarCategory.Reliability,
            confidence: DotRadarConfidence.High,
            defaultSeverity: DotRadarSeverity.Warning,
            documentationPath: "docs/rules/DTR1001.md");

    public string RuleId => DiagnosticId;

    public DotRadarRuleDescriptor Descriptor => RuleDescriptor;

    public async Task<IReadOnlyList<DotRadarDiagnostic>> AnalyzeAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        if (document.FilePath is null ||
            IsGeneratedFile(document.FilePath))
        {
            return [];
        }

        var syntaxRoot =
            await document.GetSyntaxRootAsync(cancellationToken);

        var semanticModel =
            await document.GetSemanticModelAsync(cancellationToken);

        if (syntaxRoot is null || semanticModel is null)
        {
            return [];
        }

        var diagnostics = new List<DotRadarDiagnostic>();

        foreach (var creation in syntaxRoot
                     .DescendantNodes()
                     .OfType<BaseObjectCreationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsCreatedPerOperation(creation))
            {
                continue;
            }

            var type = semanticModel
                .GetTypeInfo(creation, cancellationToken)
                .Type as INamedTypeSymbol;

            if (type is null || !IsHttpClient(type))
            {
                continue;
            }

            diagnostics.Add(CreateDiagnostic(
                document.FilePath,
                creation));
        }

        return diagnostics;
    }

    private static bool IsHttpClient(INamedTypeSymbol type)
    {
        var originalType = type.OriginalDefinition;

        return originalType.MetadataName == "HttpClient"
               && originalType.ContainingNamespace
                   .ToDisplayString() == "System.Net.Http";
    }

    private static bool IsCreatedPerOperation(
        BaseObjectCreationExpressionSyntax creation)
    {
        foreach (var ancestor in creation.Ancestors())
        {
            if (ancestor is BaseMethodDeclarationSyntax or
                LocalFunctionStatementSyntax or
                AnonymousFunctionExpressionSyntax or
                AccessorDeclarationSyntax)
            {
                return true;
            }

            if (ancestor is PropertyDeclarationSyntax property &&
                property.ExpressionBody is not null)
            {
                return true;
            }

            if (ancestor is IndexerDeclarationSyntax indexer &&
                indexer.ExpressionBody is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static DotRadarDiagnostic CreateDiagnostic(
        string filePath,
        SyntaxNode node)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        var start = lineSpan.StartLinePosition;

        return new DotRadarDiagnostic(
            ruleId: DiagnosticId,
            title: Title,
            message:
                "Creating HttpClient per operation can exhaust " +
                "available sockets. Reuse a long-lived instance " +
                "or use IHttpClientFactory.",
            severity: DotRadarSeverity.Warning,
            filePath: filePath,
            line: start.Line + 1,
            column: start.Character + 1);
    }

    private static bool IsGeneratedFile(string filePath)
    {
        return filePath.EndsWith(
                   ".g.cs",
                   StringComparison.OrdinalIgnoreCase)
               || filePath.EndsWith(
                   ".generated.cs",
                   StringComparison.OrdinalIgnoreCase);
    }
}