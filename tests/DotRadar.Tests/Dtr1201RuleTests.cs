using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class Dtr1201RuleTests
{
    [Fact]
    public async Task Reports_HttpClient_created_inside_method()
    {
        const string source = """
            using System.Net.Http;

            public sealed class WeatherService
            {
                public async Task<string> GetAsync()
                {
                    using var client = new HttpClient();

                    return await client.GetStringAsync(
                        "https://example.com");
                }
            }
            """;

        var diagnostics = await new Dtr1201Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
                CancellationToken.None);

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("DTR1201", diagnostic.RuleId);
        Assert.Contains(
            "IHttpClientFactory",
            diagnostic.Message);
    }

    [Fact]
    public async Task Reports_target_typed_creation()
    {
        const string source = """
            using System.Net.Http;

            public sealed class WeatherService
            {
                public void Run()
                {
                    HttpClient client = new();
                }
            }
            """;

        var diagnostics = await new Dtr1201Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
                CancellationToken.None);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_unrelated_HttpClient_type()
    {
        const string source = """
            public sealed class HttpClient
            {
            }

            public sealed class Service
            {
                public void Run()
                {
                    var client = new HttpClient();
                }
            }
            """;

        var diagnostics = await new Dtr1201Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_not_report_long_lived_static_field()
    {
        const string source = """
            using System.Net.Http;

            public sealed class Service
            {
                private static readonly HttpClient Client = new();
            }
            """;

        var diagnostics = await new Dtr1201Rule()
            .AnalyzeAsync(
                RoslynTestDocument.Create(source),
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Ignores_generated_files()
    {
        const string source = """
            using System.Net.Http;

            public sealed class Service
            {
                public void Run()
                {
                    var client = new HttpClient();
                }
            }
            """;

        var document = RoslynTestDocument.Create(
            source,
            filePath: "Service.g.cs");

        var diagnostics = await new Dtr1201Rule()
            .AnalyzeAsync(
                document,
                CancellationToken.None);

        Assert.Empty(diagnostics);
    }
}