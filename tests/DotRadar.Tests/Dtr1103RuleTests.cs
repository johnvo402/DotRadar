using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class Dtr1103RuleTests
{
    [Fact]
    public async Task Reports_async_void_method()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class Service
            {
                public async void SaveAsync()
                {
                    await Task.Delay(10);
                }
            }
            """;

        var diagnostics = await new Dtr1103Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
                CancellationToken.None);

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("DTR1103", diagnostic.RuleId);
        Assert.Contains(
            "Return Task",
            diagnostic.Message);
    }

    [Fact]
    public async Task Reports_async_void_local_function()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class Service
            {
                public void Run()
                {
                    async void SaveAsync()
                    {
                        await Task.Delay(10);
                    }

                    SaveAsync();
                }
            }
            """;

        var diagnostics = await new Dtr1103Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
                CancellationToken.None);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_async_Task_method()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class Service
            {
                public async Task SaveAsync()
                {
                    await Task.Delay(10);
                }
            }
            """;

        var diagnostics = await new Dtr1103Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_event_handler()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            public sealed class Window
            {
                public async void Button_Click(
                    object sender,
                    EventArgs eventArgs)
                {
                    await Task.Delay(10);
                }
            }
            """;

        var diagnostics = await new Dtr1103Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
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
                public async void SaveAsync()
                {
                    await Task.Delay(10);
                }
            }
            """;

        var document = RoslynTestDocument.Create(
            source,
            filePath: "Service.generated.cs");

        var diagnostics = await new Dtr1103Rule()
            .AnalyzeAsync(
                document,
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }
}