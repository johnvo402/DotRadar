using Microsoft.CodeAnalysis;

namespace DotRadar.Analysis.Roslyn;

public sealed class DocumentAnalysisContext
{
    private DocumentAnalysisContext(
        Document document,
        string filePath,
        SyntaxNode syntaxRoot,
        SemanticModel semanticModel)
    {
        Document = document;
        FilePath = filePath;
        SyntaxRoot = syntaxRoot;
        SemanticModel = semanticModel;
        Compilation = semanticModel.Compilation;
    }

    public Document Document { get; }

    public string FilePath { get; }

    public SyntaxNode SyntaxRoot { get; }

    public SemanticModel SemanticModel { get; }

    public Compilation Compilation { get; }

    public static async Task<DocumentAnalysisContext?> CreateAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.FilePath is null)
        {
            return null;
        }

        var syntaxRoot =
            await document.GetSyntaxRootAsync(cancellationToken);

        if (syntaxRoot is null)
        {
            return null;
        }

        var semanticModel =
            await document.GetSemanticModelAsync(cancellationToken);

        if (semanticModel is null)
        {
            return null;
        }

        return new DocumentAnalysisContext(
            document,
            document.FilePath,
            syntaxRoot,
            semanticModel);
    }
}