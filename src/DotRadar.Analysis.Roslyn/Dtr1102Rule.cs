using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotRadar.Analysis.Roslyn;

public sealed class Dtr1102Rule : IDocumentRule
{
    private const string DiagnosticId = "DTR1102";

    private static readonly DotRadarRuleDescriptor RuleDescriptor =
        new(
            ruleId: DiagnosticId,
            title: "CancellationToken parameter is not used",
            description:
                "Detects methods that declare a CancellationToken " +
                "parameter but never use or propagate it.",
            category: DotRadarCategory.Reliability,
            confidence: DotRadarConfidence.High,
            defaultSeverity: DotRadarSeverity.Warning,
            documentationPath: "docs/rules/DTR1102.md");

    public string RuleId => DiagnosticId;

    public DotRadarRuleDescriptor Descriptor => RuleDescriptor;

    public ValueTask<
    IReadOnlyList<DotRadarDiagnostic>> AnalyzeAsync(
    DocumentAnalysisContext context,
    CancellationToken cancellationToken)
    {
        if (IsGeneratedFile(context.FilePath))
        {
            return ValueTask.FromResult<
                IReadOnlyList<DotRadarDiagnostic>>([]);
        }

        var syntaxRoot = context.SyntaxRoot;
        var semanticModel = context.SemanticModel;

        var diagnostics = new List<DotRadarDiagnostic>();

        foreach (var method in syntaxRoot
                     .DescendantNodes()
                     .OfType<MethodDeclarationSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            SyntaxNode? executableBody =
                method.Body ??
                (SyntaxNode?)method.ExpressionBody?.Expression;

            // Interface, abstract hoặc partial declaration.
            if (executableBody is null)
            {
                continue;
            }

            if (semanticModel.GetDeclaredSymbol(
                method,
                cancellationToken) is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            foreach (var parameterSymbol in methodSymbol.Parameters
                         .Where(IsCancellationToken))
            {
                if (IsParameterUsed(
                        executableBody,
                        parameterSymbol,
                        semanticModel,
                        cancellationToken))
                {
                    continue;
                }

                var parameterSyntax = method.ParameterList.Parameters
                    .FirstOrDefault(parameter =>
                        SymbolEqualityComparer.Default.Equals(
                            semanticModel.GetDeclaredSymbol(
                                parameter,
                                cancellationToken),
                            parameterSymbol));

                if (parameterSyntax is null)
                {
                    continue;
                }

                diagnostics.Add(CreateDiagnostic(
                    context.FilePath,
                    parameterSyntax.Identifier,
                    parameterSymbol.Name));
            }
        }

        return ValueTask.FromResult<
    IReadOnlyList<DotRadarDiagnostic>>(diagnostics);
    }

    private static bool IsCancellationToken(
        IParameterSymbol parameter)
    {
        var type = parameter.Type;

        return type.Name == nameof(CancellationToken)
               && type.ContainingNamespace.ToDisplayString()
                   == "System.Threading";
    }

    private static bool IsParameterUsed(
        SyntaxNode executableBody,
        IParameterSymbol parameterSymbol,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        return executableBody
            .DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier =>
                SymbolEqualityComparer.Default.Equals(
                    semanticModel.GetSymbolInfo(
                        identifier,
                        cancellationToken).Symbol,
                    parameterSymbol));
    }

    private static DotRadarDiagnostic CreateDiagnostic(
        string filePath,
        SyntaxToken parameterIdentifier,
        string parameterName)
    {
        var lineSpan = parameterIdentifier
            .GetLocation()
            .GetLineSpan();

        var start = lineSpan.StartLinePosition;

        return new DotRadarDiagnostic(
            ruleId: DiagnosticId,
            title: "CancellationToken parameter is not used",
            message:
                $"The CancellationToken parameter '{parameterName}' " +
                "is not used or propagated. Pass it to asynchronous " +
                "operations or remove it.",
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