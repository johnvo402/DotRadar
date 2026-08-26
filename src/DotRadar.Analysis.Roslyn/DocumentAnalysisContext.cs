using Microsoft.CodeAnalysis;

namespace DotRadar.Analysis.Roslyn;

public sealed class DocumentAnalysisContext
{
    private DocumentAnalysisContext(
        ProjectAnalysisContext projectContext,
        Document document,
        string filePath,
        SyntaxNode syntaxRoot,
        SemanticModel semanticModel)
    {
        ProjectContext = projectContext;
        Document = document;
        FilePath = filePath;
        SyntaxRoot = syntaxRoot;
        SemanticModel = semanticModel;
    }

    public ProjectAnalysisContext ProjectContext { get; }

    public Document Document { get; }

    public string FilePath { get; }

    public SyntaxNode SyntaxRoot { get; }

    public SemanticModel SemanticModel { get; }

    public Compilation Compilation =>
        ProjectContext.Compilation;

    public static async Task<DocumentAnalysisContext?> CreateAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var projectContext =
            await ProjectAnalysisContext.CreateAsync(
                document.Project,
                cancellationToken);

        if (projectContext is null)
        {
            return null;
        }

        return await CreateAsync(
            projectContext,
            document,
            cancellationToken);
    }

    public static async Task<DocumentAnalysisContext?> CreateAsync(
        ProjectAnalysisContext projectContext,
        Document document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectContext);
        ArgumentNullException.ThrowIfNull(document);

        if (!document.Project.Id.Equals(
                projectContext.Project.Id))
        {
            throw new ArgumentException(
                "The document does not belong to the provided project.",
                nameof(document));
        }

        if (document.FilePath is null)
        {
            return null;
        }

        var syntaxRoot =
            await document.GetSyntaxRootAsync(
                cancellationToken);

        if (syntaxRoot is null)
        {
            return null;
        }

        var semanticModel =
            projectContext.Compilation.GetSemanticModel(
                syntaxRoot.SyntaxTree);

        return new DocumentAnalysisContext(
            projectContext,
            document,
            document.FilePath,
            syntaxRoot,
            semanticModel);
    }
}