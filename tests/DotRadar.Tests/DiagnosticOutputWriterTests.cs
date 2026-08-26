using System.Text.Json;

using DotRadar.Abstractions;
using DotRadar.Analysis.Roslyn;
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
            CreateReport(diagnostics),
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
    CreateReport(
        [],
        suppressedCount: 3),
    DiagnosticOutputFormat.Json,
    output);

        using var document =
            JsonDocument.Parse(output.ToString());

        Assert.Equal(
            3,
            document.RootElement
                .GetProperty("suppressedCount")
                .GetInt32());
    }

    [Fact]
    public void Json_contains_failure_information()
    {
        var diagnostics = new[]
        {
        new DotRadarDiagnostic(
            ruleId: "DTR1101",
            title: "Test",
            message: "Test",
            severity: DotRadarSeverity.Warning,
            filePath: "Service.cs",
            line: 1,
            column: 1)
    };

        using var output = new StringWriter();

        DiagnosticOutputWriter.Write(
                CreateReport(
                    diagnostics,
                    failureThreshold: DotRadarSeverity.Error),
                DiagnosticOutputFormat.Json,
                output);

        using var document =
            JsonDocument.Parse(output.ToString());

        var root = document.RootElement;

        Assert.Equal(
            "error",
            root.GetProperty("failureThreshold").GetString());

        Assert.Equal(
            0,
            root.GetProperty("failureCount").GetInt32());
    }

    [Fact]
    public void Writes_sarif_2_1_0()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "dotradar-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(
            directory,
            "Service.cs");

        try
        {
            File.WriteAllText(
                filePath,
                "return task.Result;");

            var diagnostic = new DotRadarDiagnostic(
                ruleId: "DTR1101",
                title: "Avoid blocking",
                message: "Use await instead.",
                severity: DotRadarSeverity.Warning,
                filePath: filePath,
                line: 1,
                column: 8);

            var report = new DiagnosticReport(
                diagnostics: [diagnostic],

                rules: RuleRegistry.CreateDefault().AllRules
                    .Select(rule => rule.Descriptor)
                    .ToArray(),

                baseDirectory: directory,
                suppressedCount: 0,
                failureThreshold: DotRadarSeverity.Warning);

            using var output = new StringWriter();

            DiagnosticOutputWriter.Write(
                report,
                DiagnosticOutputFormat.Sarif,
                output);

            using var document =
                JsonDocument.Parse(output.ToString());

            var root = document.RootElement;

            Assert.Equal(
                "2.1.0",
                root.GetProperty("version").GetString());

            var run = root.GetProperty("runs")[0];

            Assert.Equal(
                "DotRadar",
                run.GetProperty("tool")
                    .GetProperty("driver")
                    .GetProperty("name")
                    .GetString());

            var result = run.GetProperty("results")[0];

            Assert.Equal(
                "DTR1101",
                result.GetProperty("ruleId").GetString());

            Assert.Equal(
                "Service.cs",
                result.GetProperty("locations")[0]
                    .GetProperty("physicalLocation")
                    .GetProperty("artifactLocation")
                    .GetProperty("uri")
                    .GetString());

            Assert.True(
                result.GetProperty("partialFingerprints")
                    .TryGetProperty(
                        "primaryLocationLineHash",
                        out _));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static DiagnosticReport CreateReport(
        IReadOnlyList<DotRadarDiagnostic> diagnostics,
        int suppressedCount = 0,
        DotRadarSeverity failureThreshold =
            DotRadarSeverity.Warning)
    {
        return new DiagnosticReport(
            diagnostics: diagnostics,

            rules: RuleRegistry.CreateDefault().AllRules
                .Select(rule => rule.Descriptor)
                .ToArray(),

            baseDirectory: Directory.GetCurrentDirectory(),
            suppressedCount: suppressedCount,
            failureThreshold: failureThreshold);
    }
}