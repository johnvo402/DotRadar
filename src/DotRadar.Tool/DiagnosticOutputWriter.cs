using System.Text.Json;

using DotRadar.Abstractions;
using DotRadar.Core;

namespace DotRadar.Tool;

internal static class DiagnosticOutputWriter
{
    public static void Write(
    IReadOnlyList<DotRadarDiagnostic> diagnostics,
    DiagnosticOutputFormat format,
    TextWriter output,
    int suppressedCount = 0,
    DotRadarSeverity failureThreshold =
        DotRadarSeverity.Warning)
    {
        var failureCount = diagnostics.Count(
            diagnostic =>
                DotRadarSeverityPolicy.MeetsThreshold(
                    diagnostic.Severity,
                    failureThreshold));

        switch (format)
        {
            case DiagnosticOutputFormat.Text:
                WriteText(
                    diagnostics,
                    output,
                    suppressedCount,
                    failureThreshold,
                    failureCount);
                break;

            case DiagnosticOutputFormat.Json:
                WriteJson(
                    diagnostics,
                    output,
                    suppressedCount,
                    failureThreshold,
                    failureCount);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(format),
                    format,
                    "Unsupported output format.");
        }
    }

    private static void WriteText(
     IReadOnlyList<DotRadarDiagnostic> diagnostics,
     TextWriter output,
     int suppressedCount,
     DotRadarSeverity failureThreshold,
     int failureCount)
    {
        if (diagnostics.Count == 0)
        {
            output.WriteLine("No diagnostics found.");

            if (suppressedCount > 0)
            {
                output.WriteLine(
                    $"{suppressedCount} diagnostic(s) suppressed " +
                    "by baseline.");
            }

            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            var severity = diagnostic.Severity
                .ToString()
                .ToLowerInvariant();

            output.WriteLine(
                $"{diagnostic.FilePath}" +
                $"({diagnostic.Line},{diagnostic.Column}): " +
                $"{severity} {diagnostic.RuleId}: " +
                $"{diagnostic.Message}");
        }

        output.WriteLine();
        output.WriteLine(
            $"{diagnostics.Count} diagnostic(s) found.");
        output.WriteLine(
            $"{failureCount} diagnostic(s) meet failure threshold " +
            $"'{failureThreshold.ToString().ToLowerInvariant()}'.");
    }

    private static void WriteJson(
            IReadOnlyList<DotRadarDiagnostic> diagnostics,
            TextWriter output,
            int suppressedCount,
            DotRadarSeverity failureThreshold,
            int failureCount)
    {
        var report = new
        {
            schemaVersion = "1.0",
            diagnosticCount = diagnostics.Count,
            suppressedCount,

            failureThreshold = failureThreshold
              .ToString()
              .ToLowerInvariant(),

            failureCount,

            diagnostics = diagnostics.Select(diagnostic => new
            {
                ruleId = diagnostic.RuleId,
                title = diagnostic.Title,
                message = diagnostic.Message,

                severity = diagnostic.Severity
                    .ToString()
                    .ToLowerInvariant(),

                filePath = diagnostic.FilePath,
                line = diagnostic.Line,
                column = diagnostic.Column
            })
        };
        var json = JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        output.WriteLine(json);
    }
}