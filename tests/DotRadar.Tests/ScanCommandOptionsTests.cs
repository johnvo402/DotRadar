using DotRadar.Tool;

using Xunit;

namespace DotRadar.Tests;

public sealed class ScanCommandOptionsTests
{
    [Fact]
    public void Uses_text_format_by_default()
    {
        var success = ScanCommandOptions.TryParse(
            ["scan", "."],
            out var options,
            out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal(".", options.Target);
        Assert.Equal(
            DiagnosticOutputFormat.Text,
            options.Format);
    }

    [Fact]
    public void Parses_json_format()
    {
        var success = ScanCommandOptions.TryParse(
            ["scan", ".", "--format", "json"],
            out var options,
            out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal(
            DiagnosticOutputFormat.Json,
            options.Format);
    }

    [Fact]
    public void Rejects_unknown_format()
    {
        var success = ScanCommandOptions.TryParse(
            ["scan", ".", "--format", "xml"],
            out var options,
            out var error);

        Assert.False(success);
        Assert.Null(options);
        Assert.Contains("xml", error);
    }
}