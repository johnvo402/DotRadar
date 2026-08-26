using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotRadar.Analysis.Roslyn;

public sealed class Dtr1101Rule : IDotRadarRule
{
    private const string DiagnosticId = "DTR1101";

    private const string Title =
        "Avoid blocking on asynchronous operations";

    private static readonly DotRadarRuleDescriptor RuleDescriptor =
        new(
            ruleId: DiagnosticId,
            title: Title,
            description:
                "Detects synchronous blocking through Task.Result, " +
                "Task.Wait() and GetAwaiter().GetResult().",
            category: DotRadarCategory.Reliability,
            confidence: DotRadarConfidence.High,
            defaultSeverity: DotRadarSeverity.Warning,
            documentationPath: "docs/rules/DTR1101.md");

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

        AnalyzeResultProperties(
            context.FilePath,
            syntaxRoot,
            semanticModel,
            diagnostics,
            cancellationToken);

        AnalyzeBlockingInvocations(
            context.FilePath,
            syntaxRoot,
            semanticModel,
            diagnostics,
            cancellationToken);

        return ValueTask.FromResult<
                IReadOnlyList<DotRadarDiagnostic>>(diagnostics);
    }

    private static void AnalyzeResultProperties(
        string filePath,
        SyntaxNode syntaxRoot,
        SemanticModel semanticModel,
        ICollection<DotRadarDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var memberAccess in syntaxRoot
                     .DescendantNodes()
                     .OfType<MemberAccessExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (memberAccess.Name.Identifier.ValueText != "Result")
            {
                continue;
            }

            var symbol = semanticModel
                .GetSymbolInfo(memberAccess, cancellationToken)
                .Symbol;

            if (symbol is not IPropertySymbol propertySymbol ||
                !IsTaskResultProperty(propertySymbol))
            {
                continue;
            }

            diagnostics.Add(CreateDiagnostic(
                filePath,
                memberAccess,
                "Accessing Task.Result can block the current thread. " +
                "Use await instead."));
        }
    }

    private static void AnalyzeBlockingInvocations(
        string filePath,
        SyntaxNode syntaxRoot,
        SemanticModel semanticModel,
        ICollection<DotRadarDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var invocation in syntaxRoot
                     .DescendantNodes()
                     .OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel
                .GetSymbolInfo(invocation, cancellationToken)
                .Symbol as IMethodSymbol;

            if (symbol is null)
            {
                continue;
            }

            string? message = null;

            if (IsTaskWait(symbol))
            {
                message =
                    "Calling Task.Wait() can block the current thread. " +
                    "Use await instead.";
            }
            else if (IsAwaiterGetResult(symbol))
            {
                message =
                    "Calling GetAwaiter().GetResult() can block the " +
                    "current thread. Use await instead.";
            }

            if (message is not null)
            {
                diagnostics.Add(CreateDiagnostic(
                    filePath,
                    invocation,
                    message));
            }
        }
    }

    private static bool IsTaskResultProperty(
        IPropertySymbol property)
    {
        var type = property.ContainingType.OriginalDefinition;

        return type.ContainingNamespace.ToDisplayString()
                   == "System.Threading.Tasks"
               && type.MetadataName
                   is "Task`1" or "ValueTask`1";
    }

    private static bool IsTaskWait(IMethodSymbol method)
    {
        if (method.Name != "Wait" || method.IsStatic)
        {
            return false;
        }

        var type = method.ContainingType.OriginalDefinition;

        return type.ContainingNamespace.ToDisplayString()
                   == "System.Threading.Tasks"
               && type.MetadataName == "Task";
    }

    private static bool IsAwaiterGetResult(IMethodSymbol method)
    {
        if (method.Name != "GetResult" ||
            method.Parameters.Length != 0)
        {
            return false;
        }

        var type = method.ContainingType;

        if (type.ContainingNamespace.ToDisplayString()
            != "System.Runtime.CompilerServices")
        {
            return false;
        }

        return type.MetadataName is
            "TaskAwaiter" or
            "TaskAwaiter`1" or
            "ValueTaskAwaiter" or
            "ValueTaskAwaiter`1" or
            "ConfiguredTaskAwaiter" or
            "ConfiguredValueTaskAwaiter";
    }

    private static DotRadarDiagnostic CreateDiagnostic(
        string filePath,
        SyntaxNode node,
        string message)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        var start = lineSpan.StartLinePosition;

        return new DotRadarDiagnostic(
            ruleId: DiagnosticId,
            title: Title,
            message: message,
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