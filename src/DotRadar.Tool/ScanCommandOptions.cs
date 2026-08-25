using System.Diagnostics.CodeAnalysis;

namespace DotRadar.Tool;

internal sealed class ScanCommandOptions
{
    private ScanCommandOptions(
        string target,
        DiagnosticOutputFormat format,
        string? configPath,
        string? baselinePath)
    {
        Target = target;
        Format = format;
        ConfigPath = configPath;
        BaselinePath = baselinePath;
    }

    public string Target { get; }

    public DiagnosticOutputFormat Format { get; }

    public string? ConfigPath { get; }

    public string? BaselinePath { get; }

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
        string? configPath = null;
        var configWasSpecified = false;
        string? baselinePath = null;
        var baselineWasSpecified = false;

        for (var index = 2; index < args.Length; index++)
        {
            var argument = args[index];

            if (argument.Equals(
                    "--format",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (formatWasSpecified)
                {
                    error =
                        "The --format option can only be specified once.";
                    return false;
                }

                if (!TryReadValue(
                        args,
                        ref index,
                        "--format",
                        out var value,
                        out error))
                {
                    return false;
                }

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
                continue;
            }

            if (argument.Equals(
                    "--config",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (configWasSpecified)
                {
                    error =
                        "The --config option can only be specified once.";
                    return false;
                }

                if (!TryReadValue(
                        args,
                        ref index,
                        "--config",
                        out configPath,
                        out error))
                {
                    return false;
                }

                configWasSpecified = true;
                continue;
            }
            if (argument.Equals(
                "--baseline",
                StringComparison.OrdinalIgnoreCase))
            {
                if (baselineWasSpecified)
                {
                    error =
                        "The --baseline option can only be specified once.";
                    return false;
                }

                if (!TryReadValue(
                        args,
                        ref index,
                        "--baseline",
                        out baselinePath,
                        out error))
                {
                    return false;
                }

                baselineWasSpecified = true;
                continue;
            }

            error = $"Unknown option: {argument}";
            return false;
        }
        options = new ScanCommandOptions(
                target,
                format,
                configPath,
                baselinePath);

        return true;
    }

    private static bool TryReadValue(
    string[] args,
    ref int index,
    string option,
    out string value,
    [NotNullWhen(false)] out string? error)
    {
        value = string.Empty;
        error = null;

        if (index + 1 >= args.Length)
        {
            error = $"The {option} option requires a value.";
            return false;
        }

        value = args[++index];

        if (value.StartsWith("--", StringComparison.Ordinal))
        {
            error = $"The {option} option requires a value.";
            return false;
        }

        return true;
    }
}