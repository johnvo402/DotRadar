using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class DocumentAnalysisContextTests
{
    [Fact]
    public async Task Creates_context_with_Roslyn_models()
    {
        var document = RoslynTestDocument.Create(
            """
            public sealed class Service
            {
            }
            """,
            filePath: "Service.cs");

        var context =
            await DocumentAnalysisContext.CreateAsync(
                document,
                CancellationToken.None);

        Assert.NotNull(context);
        Assert.Same(document, context.Document);
        Assert.Equal("Service.cs", context.FilePath);
        Assert.NotNull(context.SyntaxRoot);
        Assert.NotNull(context.SemanticModel);

        Assert.Same(
            context.Compilation,
            context.SemanticModel.Compilation);

        Assert.Same(
            context.SyntaxRoot.SyntaxTree,
            context.SemanticModel.SyntaxTree);
    }

    [Fact]
    public async Task Returns_null_when_document_has_no_file_path()
    {
        var document = RoslynTestDocument.Create(
            """
            public sealed class Service
            {
            }
            """,
            filePath: null);

        var context =
            await DocumentAnalysisContext.CreateAsync(
                document,
                CancellationToken.None);

        Assert.Null(context);
    }
}