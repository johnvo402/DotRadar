using System.Text.Json;

using DotRadar.Abstractions;
using DotRadar.Tool;

using Xunit;

namespace DotRadar.Tests;

public sealed class DiagnosticOutputWriterTests
{
    [Fact]
    public void Writes_machine_readable_json()
    {
        var diagnostics = new[]
        {
            new DotRadarDiagnostic(
                ruleId: "DTR1101",
                title: "Avoid blocking",
                message: "Use await instead.",
                severity: DotRadarSeverity.Warning,
                filePath: "Service.cs",
                line: 10,
                column: 20)
        };

        using var output = new StringWriter();

        DiagnosticOutputWriter.Write(
            diagnostics,
            DiagnosticOutputFormat.Json,
            output);

        using var document =
            JsonDocument.Parse(output.ToString());

        var root = document.RootElement;

        Assert.Equal(
            "1.0",
            root.GetProperty("schemaVersion").GetString());

        Assert.Equal(
            1,
            root.GetProperty("diagnosticCount").GetInt32());

        var diagnostic = root
            .GetProperty("diagnostics")[0];

        Assert.Equal(
            "DTR1101",
            diagnostic.GetProperty("ruleId").GetString());

        Assert.Equal(
            "warning",
            diagnostic.GetProperty("severity").GetString());

        Assert.Equal(
            "Service.cs",
            diagnostic.GetProperty("filePath").GetString());
    }

    [Fact]
    public void Json_contains_suppressed_count()
    {
        using var output = new StringWriter();

        DiagnosticOutputWriter.Write(
            [],
            DiagnosticOutputFormat.Json,
            output,
            suppressedCount: 3);

        using var document =
            JsonDocument.Parse(output.ToString());

        Assert.Equal(
            3,
            document.RootElement
                .GetProperty("suppressedCount")
                .GetInt32());
    }
}