using System.Diagnostics.CodeAnalysis;

namespace DotRadar.Tool;

internal sealed class ScanCommandOptions
{
    private ScanCommandOptions(
        string target,
        DiagnosticOutputFormat format)
    {
        Target = target;
        Format = format;
    }

    public string Target { get; }

    public DiagnosticOutputFormat Format { get; }

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out ScanCommandOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        options = null;
        error = null;

        if (args.Length < 2)
        {
            error = "The scan command requires a target path.";
            return false;
        }

        if (!args[0].Equals(
                "scan",
                StringComparison.OrdinalIgnoreCase))
        {
            error = $"Unknown command: {args[0]}";
            return false;
        }

        if (args[1].StartsWith("--", StringComparison.Ordinal))
        {
            error = "The scan command requires a target path.";
            return false;
        }

        var target = args[1];
        var format = DiagnosticOutputFormat.Text;
        var formatWasSpecified = false;

        for (var index = 2; index < args.Length; index++)
        {
            var argument = args[index];

            if (!argument.Equals(
                    "--format",
                    StringComparison.OrdinalIgnoreCase))
            {
                error = $"Unknown option: {argument}";
                return false;
            }

            if (formatWasSpecified)
            {
                error = "The --format option can only be specified once.";
                return false;
            }

            if (index + 1 >= args.Length)
            {
                error = "The --format option requires a value.";
                return false;
            }

            var value = args[++index];

            if (value.Equals(
                    "text",
                    StringComparison.OrdinalIgnoreCase))
            {
                format = DiagnosticOutputFormat.Text;
            }
            else if (value.Equals(
                         "json",
                         StringComparison.OrdinalIgnoreCase))
            {
                format = DiagnosticOutputFormat.Json;
            }
            else
            {
                error =
                    $"Unsupported output format: {value}. " +
                    "Supported formats: text, json.";

                return false;
            }

            formatWasSpecified = true;
        }

        options = new ScanCommandOptions(target, format);
        return true;
    }
}