using System.Text.RegularExpressions;

using DotRadar.Core;
using DotRadar.Tool;

using Xunit;

namespace DotRadar.Tests;

public sealed class DotRadarCliTests
{
    [Fact]
    public async Task No_arguments_shows_help()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            [],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Usage:", output.ToString());
        Assert.Contains("scan", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task Global_help_returns_success(
        string argument)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            [argument],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("DotRadar", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Scan_help_returns_success()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            ["scan", "--help"],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);

        Assert.Contains(
            "--fail-on",
            output.ToString());

        Assert.Contains(
            "--format",
            output.ToString());

        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Version_returns_semantic_version()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            ["--version"],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);

        Assert.Matches(
            new Regex(
                @"^DotRadar \d+\.\d+\.\d+",
                RegexOptions.CultureInvariant),
            output.ToString().Trim());

        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Unknown_command_returns_invalid_arguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            ["unknown"],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(
            ExitCodes.InvalidArguments,
            exitCode);

        Assert.Contains(
            "Unknown command",
            error.ToString());
    }

    [Fact]
    public async Task Cancelled_scan_returns_130()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var cancellationSource =
            new CancellationTokenSource();

        cancellationSource.Cancel();

        var exitCode = await DotRadarApplication.RunAsync(
            ["scan", "."],
            output,
            error,
            cancellationSource.Token);

        Assert.Equal(ExitCodes.Canceled, exitCode);

        Assert.Contains(
            "cancelled",
            error.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_rules_displays_confidence()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await DotRadarApplication.RunAsync(
            ["list-rules"],
            output,
            error,
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Confidence", output.ToString());
        Assert.Contains("High", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }
}