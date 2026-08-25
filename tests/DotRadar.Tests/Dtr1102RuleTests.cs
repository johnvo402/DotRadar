using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class Dtr1102RuleTests
{
    [Fact]
    public async Task Reports_unused_CancellationToken()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class ReportService
            {
                public async Task GenerateAsync(
                    CancellationToken cancellationToken)
                {
                    await Task.Delay(100);
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(source);

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("DTR1102", diagnostic.RuleId);
        Assert.Contains(
            "cancellationToken",
            diagnostic.Message);
    }

    [Fact]
    public async Task Does_not_report_propagated_CancellationToken()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class ReportService
            {
                public async Task GenerateAsync(
                    CancellationToken cancellationToken)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_method_without_CancellationToken()
    {
        const string source = """
            using System.Threading.Tasks;

            public sealed class ReportService
            {
                public async Task GenerateAsync()
                {
                    await Task.Delay(100);
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_interface_method()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;

            public interface IReportService
            {
                Task GenerateAsync(
                    CancellationToken cancellationToken);
            }
            """;

        var diagnostics = await AnalyzeAsync(source);

        Assert.Empty(diagnostics);
    }

    private static Task<IReadOnlyList<
        DotRadar.Abstractions.DotRadarDiagnostic>> AnalyzeAsync(
        string source)
    {
        return new Dtr1102Rule().AnalyzeAsync(
            RoslynTestDocument.Create(source),
            CancellationToken.None);
    }
}