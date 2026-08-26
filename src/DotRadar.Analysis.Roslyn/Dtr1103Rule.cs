using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotRadar.Analysis.Roslyn;

public sealed class Dtr1103Rule : IDotRadarRule
{
    private const string DiagnosticId = "DTR1103";

    private const string Title =
        "Avoid async void methods";

    private static readonly DotRadarRuleDescriptor RuleDescriptor =
        new(
            ruleId: DiagnosticId,
            title: Title,
            description:
                "Detects async void methods and local functions, " +
                "excluding conventional event handlers.",
            category: DotRadarCategory.Reliability,
            confidence: DotRadarConfidence.High,
            defaultSeverity: DotRadarSeverity.Warning,
            documentationPath: "docs/rules/DTR1103.md");

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

        AnalyzeMethods(
            document.FilePath,
            syntaxRoot,
            semanticModel,
            diagnostics,
            cancellationToken);

        AnalyzeLocalFunctions(
            document.FilePath,
            syntaxRoot,
            semanticModel,
            diagnostics,
            cancellationToken);

        return diagnostics;
    }

    private static void AnalyzeMethods(
        string filePath,
        SyntaxNode syntaxRoot,
        SemanticModel semanticModel,
        ICollection<DotRadarDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var method in syntaxRoot
                     .DescendantNodes()
                     .OfType<MethodDeclarationSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                continue;
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(
                method,
                cancellationToken);

            AddDiagnosticIfRequired(
                filePath,
                method.Identifier.GetLocation(),
                methodSymbol,
                diagnostics);
        }
    }

    private static void AnalyzeLocalFunctions(
        string filePath,
        SyntaxNode syntaxRoot,
        SemanticModel semanticModel,
        ICollection<DotRadarDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var localFunction in syntaxRoot
                     .DescendantNodes()
                     .OfType<LocalFunctionStatementSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!localFunction.Modifiers.Any(
                    SyntaxKind.AsyncKeyword))
            {
                continue;
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(
                localFunction,
                cancellationToken);

            AddDiagnosticIfRequired(
                filePath,
                localFunction.Identifier.GetLocation(),
                methodSymbol,
                diagnostics);
        }
    }

    private static void AddDiagnosticIfRequired(
        string filePath,
        Location location,
        IMethodSymbol? methodSymbol,
        ICollection<DotRadarDiagnostic> diagnostics)
    {
        if (methodSymbol is null ||
            !methodSymbol.ReturnsVoid ||
            IsConventionalEventHandler(methodSymbol))
        {
            return;
        }

        diagnostics.Add(CreateDiagnostic(
            filePath,
            location));
    }

    private static bool IsConventionalEventHandler(
        IMethodSymbol method)
    {
        if (method.Parameters.Length != 2)
        {
            return false;
        }

        var senderParameter = method.Parameters[0];
        var eventParameter = method.Parameters[1];

        return senderParameter.Type.SpecialType ==
               SpecialType.System_Object
               && IsEventArgs(eventParameter.Type);
    }

    private static bool IsEventArgs(ITypeSymbol type)
    {
        var currentType = type as INamedTypeSymbol;

        while (currentType is not null)
        {
            if (currentType.MetadataName == "EventArgs" &&
                currentType.ContainingNamespace
                    .ToDisplayString() == "System")
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }

    private static DotRadarDiagnostic CreateDiagnostic(
        string filePath,
        Location location)
    {
        var lineSpan = location.GetLineSpan();
        var start = lineSpan.StartLinePosition;

        return new DotRadarDiagnostic(
            ruleId: DiagnosticId,
            title: Title,
            message:
                "Async void methods cannot be awaited and their " +
                "exceptions cannot be observed reliably. Return " +
                "Task instead.",
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