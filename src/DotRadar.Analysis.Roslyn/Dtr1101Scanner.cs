using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotRadar.Analysis.Roslyn;

public sealed class Dtr1101Scanner
{
    private readonly Dtr1101Rule _rule = new();

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

        foreach (var project in solution.Projects
                     .Where(project =>
                         project.Language == LanguageNames.CSharp))
        {
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentDiagnostics =
                    await _rule.AnalyzeAsync(
                        document,
                        cancellationToken);

                diagnostics.AddRange(documentDiagnostics);
            }
        }

        return diagnostics
            .OrderBy(diagnostic => diagnostic.FilePath)
            .ThenBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Column)
            .ToArray();
    }

    private static async Task<Solution> LoadSolutionAsync(
        MSBuildWorkspace workspace,
        string targetPath,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(targetPath);

        if (extension.Equals(
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
}