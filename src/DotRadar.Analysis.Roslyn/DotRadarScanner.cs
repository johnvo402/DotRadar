using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotRadar.Analysis.Roslyn;

public sealed class DotRadarScanner
{
    private readonly RuleSet _rules;

    public DotRadarScanner(RuleSet? rules = null)
    {
        _rules = rules ?? RuleRegistry.CreateDefault();
    }

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
            cancellationToken.ThrowIfCancellationRequested();

            var projectContext =
                await ProjectAnalysisContext.CreateAsync(
                    project,
                    cancellationToken);

            if (projectContext is null)
            {
                continue;
            }

            foreach (var rule in _rules.ProjectRules)
            {
                var ruleDiagnostics =
                    await rule.AnalyzeAsync(
                        projectContext,
                        cancellationToken);

                diagnostics.AddRange(ruleDiagnostics);
            }

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentContext =
                    await DocumentAnalysisContext.CreateAsync(
                        projectContext,
                        document,
                        cancellationToken);

                if (documentContext is null)
                {
                    continue;
                }

                foreach (var rule in _rules.DocumentRules)
                {
                    var ruleDiagnostics =
                        await rule.AnalyzeAsync(
                            documentContext,
                            cancellationToken);

                    diagnostics.AddRange(ruleDiagnostics);
                }
            }
        }

        return diagnostics
            .OrderBy(diagnostic => diagnostic.FilePath)
            .ThenBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Column)
            .ThenBy(diagnostic => diagnostic.RuleId)
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