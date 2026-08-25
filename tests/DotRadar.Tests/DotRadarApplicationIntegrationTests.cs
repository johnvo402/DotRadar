using System.Text.Json;

using DotRadar.Core;
using DotRadar.Tool;

using Xunit;

namespace DotRadar.Tests;

public sealed class DotRadarApplicationIntegrationTests :
    IDisposable
{
    private readonly string _emptyConfigPath;

    public DotRadarApplicationIntegrationTests()
    {
        _emptyConfigPath = Path.Combine(
            Path.GetTempPath(),
            $"dotradar-config-{Guid.NewGuid():N}.json");

        File.WriteAllText(
            _emptyConfigPath,
            """
            {
              "rules": {}
            }
            """);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Broken_project_returns_policy_violation()
    {
        var projectPath = GetSampleProject(
            "BrokenWebApp");

        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            [
                "scan",
                projectPath,
                "--config",
                _emptyConfigPath,
                "--format",
                "json"
            ],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(
            ExitCodes.PolicyViolation,
            exitCode);

        Assert.Equal(string.Empty, error.ToString());

        using var document =
            JsonDocument.Parse(output.ToString());

        Assert.True(
            document.RootElement
                .GetProperty("diagnosticCount")
                .GetInt32() >= 2);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Clean_project_returns_success()
    {
        var projectPath = GetSampleProject(
            "CleanWebApp");

        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            [
                "scan",
                projectPath,
                "--config",
                _emptyConfigPath
            ],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal(string.Empty, error.ToString());

        Assert.Contains(
            "No diagnostics found",
            output.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Error_threshold_allows_warnings()
    {
        var projectPath = GetSampleProject(
            "BrokenWebApp");

        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            [
                "scan",
                projectPath,
                "--config",
                _emptyConfigPath,
                "--fail-on",
                "error"
            ],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal(string.Empty, error.ToString());

        Assert.Contains(
            "0 diagnostic(s) meet failure threshold 'error'",
            output.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Baseline_suppresses_existing_diagnostics()
    {
        var projectPath = GetSampleProject(
            "BrokenWebApp");

        var baselinePath = Path.Combine(
            Path.GetTempPath(),
            $"dotradar-baseline-{Guid.NewGuid():N}.json");

        try
        {
            using var baselineOutput = new StringWriter();
            using var baselineError = new StringWriter();

            var baselineExitCode =
                await DotRadarApplication.RunAsync(
                    [
                        "baseline",
                        projectPath,
                        "--config",
                        _emptyConfigPath,
                        "--output",
                        baselinePath
                    ],
                    baselineOutput,
                    baselineError,
                    CancellationToken.None);

            Assert.Equal(
                ExitCodes.Success,
                baselineExitCode);

            Assert.True(File.Exists(baselinePath));

            using var scanOutput = new StringWriter();
            using var scanError = new StringWriter();

            var scanExitCode =
                await DotRadarApplication.RunAsync(
                    [
                        "scan",
                        projectPath,
                        "--config",
                        _emptyConfigPath,
                        "--baseline",
                        baselinePath,
                        "--format",
                        "json"
                    ],
                    scanOutput,
                    scanError,
                    CancellationToken.None);

            Assert.Equal(
                ExitCodes.Success,
                scanExitCode);

            Assert.Equal(
                string.Empty,
                scanError.ToString());

            using var document =
                JsonDocument.Parse(scanOutput.ToString());

            var root = document.RootElement;

            Assert.Equal(
                0,
                root.GetProperty(
                    "diagnosticCount").GetInt32());

            Assert.True(
                root.GetProperty(
                    "suppressedCount").GetInt32() >= 2);
        }
        finally
        {
            if (File.Exists(baselinePath))
            {
                File.Delete(baselinePath);
            }
        }
    }

    public void Dispose()
    {
        if (File.Exists(_emptyConfigPath))
        {
            File.Delete(_emptyConfigPath);
        }
    }

    private static string GetSampleProject(
        string projectName)
    {
        var root = FindRepositoryRoot();

        return Path.Combine(
            root,
            "samples",
            projectName,
            $"{projectName}.csproj");
    }

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var brokenProject = Path.Combine(
                directory.FullName,
                "samples",
                "BrokenWebApp",
                "BrokenWebApp.csproj");

            if (File.Exists(brokenProject))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Unable to locate DotRadar repository root.");
    }
}