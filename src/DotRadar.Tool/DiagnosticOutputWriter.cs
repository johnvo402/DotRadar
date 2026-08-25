using System.Text.Json;

namespace DotRadar.Tool;

internal static class DiagnosticOutputWriter
{
    public static void Write(
        DiagnosticReport report,
        DiagnosticOutputFormat format,
        TextWriter output)
    {
        switch (format)
        {
            case DiagnosticOutputFormat.Text:
                WriteText(report, output);
                break;

            case DiagnosticOutputFormat.Json:
                WriteJson(report, output);
                break;

            case DiagnosticOutputFormat.Sarif:
                SarifOutputWriter.Write(report, output);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(format),
                    format,
                    "Unsupported output format.");
        }
    }

    private static void WriteText(
        DiagnosticReport report,
        TextWriter output)
    {
        if (report.Diagnostics.Count == 0)
        {
            output.WriteLine("No diagnostics found.");

            if (report.SuppressedCount > 0)
            {
                output.WriteLine(
                    $"{report.SuppressedCount} diagnostic(s) " +
                    "suppressed by baseline.");
            }

            return;
        }

        foreach (var diagnostic in report.Diagnostics)
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
            $"{report.Diagnostics.Count} diagnostic(s) found.");

        if (report.SuppressedCount > 0)
        {
            output.WriteLine(
                $"{report.SuppressedCount} diagnostic(s) " +
                "suppressed by baseline.");
        }

        output.WriteLine(
            $"{report.FailureCount} diagnostic(s) meet failure " +
            $"threshold '{report.FailureThreshold
                .ToString()
                .ToLowerInvariant()}'.");
    }

    private static void WriteJson(
        DiagnosticReport report,
        TextWriter output)
    {
        var jsonReport = new
        {
            schemaVersion = "1.0",
            diagnosticCount = report.Diagnostics.Count,
            suppressedCount = report.SuppressedCount,

            failureThreshold = report.FailureThreshold
                .ToString()
                .ToLowerInvariant(),

            failureCount = report.FailureCount,

            diagnostics = report.Diagnostics.Select(
                diagnostic => new
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
            jsonReport,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        output.WriteLine(json);
    }
}