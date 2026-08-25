using DotRadar.Analysis.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using Xunit;

namespace DotRadar.Tests;

public sealed class Dtr1101RuleTests
{
    [Fact]
    public async Task Reports_Task_Result()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class Service
            {
                public int Run()
                {
                    var task = Task.FromResult(42);
                    return task.Result;
                }
            }
            """;

        var document = CreateDocument(source);

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(document, CancellationToken.None);

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("DTR1101", diagnostic.RuleId);
        Assert.Equal(8, diagnostic.Line);
        Assert.Equal(16, diagnostic.Column);
    }

    [Fact]
    public async Task Reports_ValueTask_Result()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class Service
            {
                public int Run()
                {
                    var operation = new ValueTask<int>(42);
                    return operation.Result;
                }
            }
            """;

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(
                CreateDocument(source),
                CancellationToken.None);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_unrelated_Result_property()
    {
        const string source = """
            public sealed class Operation
            {
                public int Result => 42;
            }

            public sealed class Service
            {
                public int Run()
                {
                    var operation = new Operation();
                    return operation.Result;
                }
            }
            """;

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(
                CreateDocument(source),
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_await()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class Service
            {
                public async Task<int> RunAsync()
                {
                    var task = Task.FromResult(42);
                    return await task;
                }
            }
            """;

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(
                CreateDocument(source),
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Ignores_generated_files()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class Service
            {
                public int Run()
                {
                    return Task.FromResult(42).Result;
                }
            }
            """;

        var document = CreateDocument(
            source,
            filePath: "Service.g.cs");

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(document, CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_Task_Wait()
    {
        const string source = """
        using System.Threading.Tasks;

        public sealed class Service
        {
            public void Run()
            {
                var task = Task.Delay(100);
                task.Wait();
            }
        }
        """;

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(
                CreateDocument(source),
                CancellationToken.None);

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("DTR1101", diagnostic.RuleId);
    }

    [Fact]
    public async Task Reports_GetAwaiter_GetResult()
    {
        const string source = """
        using System.Threading.Tasks;

        public sealed class Service
        {
            public int Run()
            {
                var task = Task.FromResult(42);
                return task.GetAwaiter().GetResult();
            }
        }
        """;

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(
                CreateDocument(source),
                CancellationToken.None);

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("DTR1101", diagnostic.RuleId);
    }

    [Fact]
    public async Task Does_not_report_unrelated_Wait_method()
    {
        const string source = """
        public sealed class Operation
        {
            public void Wait()
            {
            }
        }

        public sealed class Service
        {
            public void Run()
            {
                var operation = new Operation();
                operation.Wait();
            }
        }
        """;

        var diagnostics = await new Dtr1101Rule()
            .AnalyzeAsync(
                CreateDocument(source),
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    private static Document CreateDocument(
        string source,
        string filePath = "Test.cs")
    {
        var workspace = new AdhocWorkspace();

        var project = workspace
            .AddProject(
                "DotRadar.TestProject",
                LanguageNames.CSharp)
            .WithCompilationOptions(
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(
                new CSharpParseOptions(LanguageVersion.Latest))
            .AddMetadataReferences(GetFrameworkReferences());

        return project.AddDocument(
            Path.GetFileName(filePath),
            SourceText.From(source),
            filePath: filePath);
    }

    private static IEnumerable<MetadataReference>
        GetFrameworkReferences()
    {
        var trustedAssemblies =
            AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
                as string
            ?? throw new InvalidOperationException(
                "Unable to locate platform assemblies.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path =>
                MetadataReference.CreateFromFile(path));
    }
}