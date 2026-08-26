using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class ProjectAnalysisContextTests
{
    [Fact]
    public async Task Creates_context_with_compilation()
    {
        var document = RoslynTestDocument.Create(
            """
            public sealed class Service
            {
            }
            """,
            filePath: "Service.cs");

        var context =
            await ProjectAnalysisContext.CreateAsync(
                document.Project,
                CancellationToken.None);

        Assert.NotNull(context);
        Assert.Same(document.Project, context.Project);
        Assert.Equal(
            document.Project.Name,
            context.Name);

        Assert.Contains(
            context.Compilation.SyntaxTrees,
            tree => tree.FilePath == "Service.cs");
    }
}