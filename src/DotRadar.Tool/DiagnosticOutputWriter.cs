using System.Text.Json;

using DotRadar.Abstractions;

namespace DotRadar.Tool;

internal static class DiagnosticOutputWriter
{
    public static void Write(
        IReadOnlyList<DotRadarDiagnostic> diagnostics,
        DiagnosticOutputFormat format,
        TextWriter output)
    {
        switch (format)
        {
            case DiagnosticOutputFormat.Text:
                WriteText(diagnostics, output);
                break;

            case DiagnosticOutputFormat.Json:
                WriteJson(diagnostics, output);
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
        TextWriter output)
    {
        if (diagnostics.Count == 0)
        {
            output.WriteLine("No diagnostics found.");
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
    }

    private static void WriteJson(
        IReadOnlyList<DotRadarDiagnostic> diagnostics,
        TextWriter output)
    {
        var report = new
        {
            schemaVersion = "1.0",
            diagnosticCount = diagnostics.Count,

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