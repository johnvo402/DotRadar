using DotRadar.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotRadar.Analysis.Roslyn;

public sealed class DotRadarScanner
{
    private readonly IReadOnlyList<IDotRadarRule> _rules;

    public DotRadarScanner(
        IEnumerable<IDotRadarRule>? rules = null)
    {
        _rules = (rules ?? RuleRegistry.CreateDefault())
            .ToArray();

        ValidateRules(_rules);
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
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context =
                    await DocumentAnalysisContext.CreateAsync(
                        document,
                        cancellationToken);

                if (context is null)
                {
                    continue;
                }

                foreach (var rule in _rules)
                {
                    var ruleDiagnostics =
                        await rule.AnalyzeAsync(
                            context,
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

    private static void ValidateRules(
        IReadOnlyList<IDotRadarRule> rules)
    {
        var duplicateRuleId = rules
            .GroupBy(
                rule => rule.RuleId,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateRuleId is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate rule ID: {duplicateRuleId}");
        }
    }
}