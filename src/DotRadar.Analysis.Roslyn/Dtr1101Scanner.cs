using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotRadar.Analysis.Roslyn;

public sealed class Dtr1101Scanner
{
    public async Task<IReadOnlyList<DotRadarDiagnostic>> ScanAsync(
        string target,
        CancellationToken cancellationToken)
    {
        var targetPath = ProjectTargetResolver.Resolve(target);

        using var workspace = MSBuildWorkspace.Create();

        var solution = await LoadSolutionAsync(
            workspace,
            targetPath,
            cancellationToken);

        var diagnostics = new List<DotRadarDiagnostic>();

        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (project.Language != LanguageNames.CSharp)
            {
                continue;
            }

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document.FilePath is null ||
                    IsGeneratedFile(document.FilePath))
                {
                    continue;
                }

                var syntaxRoot = await document.GetSyntaxRootAsync(
                    cancellationToken);

                var semanticModel = await document.GetSemanticModelAsync(
                    cancellationToken);

                if (syntaxRoot is null || semanticModel is null)
                {
                    continue;
                }

                var memberAccesses = syntaxRoot
                    .DescendantNodes()
                    .OfType<MemberAccessExpressionSyntax>();

                foreach (var memberAccess in memberAccesses)
                {
                    if (memberAccess.Name.Identifier.ValueText != "Result")
                    {
                        continue;
                    }

                    var symbol = semanticModel
                        .GetSymbolInfo(memberAccess, cancellationToken)
                        .Symbol;

                    if (symbol is not IPropertySymbol propertySymbol ||
                        !IsTaskLike(propertySymbol.ContainingType))
                    {
                        continue;
                    }

                    diagnostics.Add(CreateDiagnostic(
                        document.FilePath,
                        memberAccess));
                }
            }
        }

        return diagnostics
            .OrderBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Line)
            .ThenBy(x => x.Column)
            .ToArray();
    }

    private static async Task<Solution> LoadSolutionAsync(
        MSBuildWorkspace workspace,
        string targetPath,
        CancellationToken cancellationToken)
    {
        if (targetPath.EndsWith(
                ".csproj",
                StringComparison.OrdinalIgnoreCase))
        {
            var project = await workspace.OpenProjectAsync(
                targetPath,
                cancellationToken: cancellationToken);

            return project.Solution;
        }

        return await workspace.OpenSolutionAsync(
            targetPath,
            cancellationToken: cancellationToken);
    }

    private static bool IsTaskLike(INamedTypeSymbol type)
    {
        var originalType = type.OriginalDefinition;

        if (originalType.ContainingNamespace.ToDisplayString() !=
            "System.Threading.Tasks")
        {
            return false;
        }

        return originalType.MetadataName is "Task`1" or "ValueTask`1";
    }

    private static DotRadarDiagnostic CreateDiagnostic(
        string filePath,
        MemberAccessExpressionSyntax memberAccess)
    {
        var position = memberAccess
            .GetLocation()
            .GetLineSpan()
            .StartLinePosition;

        var relativePath = Path.GetRelativePath(
            Environment.CurrentDirectory,
            filePath);

        return new DotRadarDiagnostic(
            ruleId: "DTR1101",
            title: "Sync-over-async",
            message: "Avoid blocking asynchronous work with .Result. " +
                     "Use await instead.",
            severity: DotRadarSeverity.Error,
            filePath: relativePath,
            line: position.Line + 1,
            column: position.Character + 1);
    }

    private static bool IsGeneratedFile(string filePath)
    {
        return filePath.EndsWith(
                   ".g.cs",
                   StringComparison.OrdinalIgnoreCase) ||
               filePath.EndsWith(
                   ".generated.cs",
                   StringComparison.OrdinalIgnoreCase);
    }
}