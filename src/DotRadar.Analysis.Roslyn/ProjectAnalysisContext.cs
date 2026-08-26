using Microsoft.CodeAnalysis;

namespace DotRadar.Analysis.Roslyn;

public sealed class ProjectAnalysisContext
{
    private ProjectAnalysisContext(
        Project project,
        Compilation compilation)
    {
        Project = project;
        Compilation = compilation;
    }

    public Project Project { get; }

    public Compilation Compilation { get; }

    public string Name => Project.Name;

    public string? FilePath => Project.FilePath;

    public static async Task<ProjectAnalysisContext?> CreateAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);

        var compilation =
            await project.GetCompilationAsync(
                cancellationToken);

        if (compilation is null)
        {
            return null;
        }

        return new ProjectAnalysisContext(
            project,
            compilation);
    }
}