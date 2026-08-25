using System.Diagnostics.CodeAnalysis;

namespace DotRadar.Tool;

internal sealed class BaselineCommandOptions
{
    private BaselineCommandOptions(
        string target,
        string? outputPath,
        string? configPath)
    {
        Target = target;
        OutputPath = outputPath;
        ConfigPath = configPath;
    }

    public string Target { get; }

    public string? OutputPath { get; }

    public string? ConfigPath { get; }

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out BaselineCommandOptions? options,
        out string? error)
    {
        options = null;
        error = null;

        if (args.Length < 2)
        {
            error =
                "The baseline command requires a target path.";
            return false;
        }

        if (!args[0].Equals(
                "baseline",
                StringComparison.OrdinalIgnoreCase))
        {
            error = $"Unknown command: {args[0]}";
            return false;
        }

        if (args[1].StartsWith("--", StringComparison.Ordinal))
        {
            error =
                "The baseline command requires a target path.";
            return false;
        }

        var target = args[1];
        string? outputPath = null;
        string? configPath = null;

        for (var index = 2; index < args.Length; index++)
        {
            var option = args[index];

            if (option.Equals(
                    "--output",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (outputPath is not null)
                {
                    error =
                        "The --output option can only be " +
                        "specified once.";
                    return false;
                }

                if (!TryReadValue(
                        args,
                        ref index,
                        "--output",
                        out outputPath,
                        out error))
                {
                    return false;
                }

                continue;
            }

            if (option.Equals(
                    "--config",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (configPath is not null)
                {
                    error =
                        "The --config option can only be " +
                        "specified once.";
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

                continue;
            }

            error = $"Unknown option: {option}";
            return false;
        }

        options = new BaselineCommandOptions(
            target,
            outputPath,
            configPath);

        return true;
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        string option,
        out string value,
        out string? error)
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