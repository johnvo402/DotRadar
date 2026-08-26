using DotRadar.Abstractions;
using DotRadar.Analysis.Roslyn;
using DotRadar.Core;

using Microsoft.Build.Locator;

namespace DotRadar.Tool;

internal static class DotRadarApplication
{
    private static readonly object MsBuildLock = new();

    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter errorOutput,
        CancellationToken cancellationToken)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage(output);
                return ExitCodes.Success;
            }

            if (args.Length == 1 && IsHelp(args[0]))
            {
                PrintUsage(output);
                return ExitCodes.Success;
            }

            if (args.Length == 1 && IsVersion(args[0]))
            {
                output.WriteLine(
                    $"DotRadar {ToolVersionProvider.GetVersion()}");

                return ExitCodes.Success;
            }

            if (args.Length == 2 && IsHelp(args[1]))
            {
                return PrintCommandHelp(
                    args[0],
                    output,
                    errorOutput);
            }
            if (args.Length == 1 &&
                args[0].Equals(
                    "list-rules",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ListRulesCommand.Execute(output);
            }

            if (args.Length > 0 &&
                args[0].Equals(
                    "scan",
                    StringComparison.OrdinalIgnoreCase))
            {
                return await RunScanAsync(
                    args,
                    output,
                    errorOutput,
                    cancellationToken);
            }

            if (args.Length > 0 &&
                args[0].Equals(
                    "baseline",
                    StringComparison.OrdinalIgnoreCase))
            {
                return await RunBaselineAsync(
                    args,
                    output,
                    errorOutput,
                    cancellationToken);
            }

            errorOutput.WriteLine(
                    $"Unknown command: {args[0]}");

            errorOutput.WriteLine();

            PrintUsage(errorOutput);

            return ExitCodes.InvalidArguments;
        }
        catch (DotRadarConfigurationException exception)
        {
            errorOutput.WriteLine(
                $"Configuration error: {exception.Message}");

            return ExitCodes.InvalidArguments;
        }
        catch (DotRadarBaselineException exception)
        {
            errorOutput.WriteLine(
                $"Baseline error: {exception.Message}");

            return ExitCodes.InvalidArguments;
        }
        catch (OperationCanceledException)
        {
            errorOutput.WriteLine("Operation cancelled.");
            return ExitCodes.Canceled;
        }
        catch (FileNotFoundException exception)
        {
            errorOutput.WriteLine(exception.Message);
            return ExitCodes.ProjectLoadFailure;
        }
        catch (InvalidOperationException exception)
        {
            errorOutput.WriteLine(exception.Message);
            return ExitCodes.ProjectLoadFailure;
        }
        catch (Exception exception)
        {
            errorOutput.WriteLine(
                $"Unexpected error: {exception.Message}");

            return ExitCodes.InternalError;
        }
    }

    private static async Task<int> RunScanAsync(
        string[] args,
        TextWriter output,
        TextWriter errorOutput,
        CancellationToken cancellationToken)
    {
        if (!ScanCommandOptions.TryParse(
                args,
                out var options,
                out var error))
        {
            errorOutput.WriteLine(
                error ?? "Invalid scan arguments.");

            PrintUsage(errorOutput);

            return ExitCodes.InvalidArguments;
        }
        cancellationToken.ThrowIfCancellationRequested();

        var result = await AnalyzeAsync(
            options.Target,
            options.ConfigPath,
            cancellationToken);

        IReadOnlyList<DotRadarDiagnostic> diagnostics =
            result.Diagnostics;

        var suppressedCount = 0;

        if (options.BaselinePath is not null)
        {
            var baseline = DotRadarBaselineStore.Load(
                options.BaselinePath);

            var filtered = DotRadarBaselineService.Filter(
                diagnostics,
                baseline,
                result.BaseDirectory);

            suppressedCount =
                diagnostics.Count - filtered.Count;

            diagnostics = filtered;
        }

        var report = new DiagnosticReport(
            diagnostics: diagnostics,
            rules: result.Rules,
            baseDirectory: result.BaseDirectory,
            suppressedCount: suppressedCount,
            failureThreshold: options.FailOn);

        DiagnosticOutputWriter.Write(
            report,
            options.Format,
            output);

        return report.FailureCount == 0
            ? ExitCodes.Success
            : ExitCodes.PolicyViolation;
    }

    private static async Task<int> RunBaselineAsync(
        string[] args,
        TextWriter output,
        TextWriter errorOutput,
        CancellationToken cancellationToken)
    {
        if (!BaselineCommandOptions.TryParse(
                args,
                out var options,
                out var error))
        {
            errorOutput.WriteLine(
                error ?? "Invalid baseline arguments.");

            PrintUsage(errorOutput);

            return ExitCodes.InvalidArguments;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = await AnalyzeAsync(
            options.Target,
            options.ConfigPath,
            cancellationToken);

        var baseline = DotRadarBaselineService.Create(
            result.Diagnostics,
            result.BaseDirectory);

        var outputPath = options.OutputPath
                         ?? Path.Combine(
                             result.BaseDirectory,
                             ".dotradar-baseline.json");

        DotRadarBaselineStore.Save(
            outputPath,
            baseline);

        output.WriteLine(
            $"Baseline created: {Path.GetFullPath(outputPath)}");

        output.WriteLine(
            $"Diagnostics recorded: " +
            $"{baseline.Diagnostics.Count}");

        return ExitCodes.Success;
    }

    private static async Task<AnalysisResult> AnalyzeAsync(
        string target,
        string? configPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureMsBuildRegistered();

        var availableRuleSet =
    RuleRegistry.CreateDefault();

        var configurationPath =
            DotRadarConfigurationLocator.Find(
                target,
                configPath);

        var configuration =
            DotRadarConfigurationLoader.Load(
                configurationPath);

        configuration.ValidateKnownRules(
    availableRuleSet.AllRules.Select(
        rule => rule.RuleId));

        var enabledRuleSet = availableRuleSet.Filter(
    rule => configuration.IsEnabled(rule.RuleId));

        var scanner = new DotRadarScanner(
    enabledRuleSet);

        var rawDiagnostics = await scanner.ScanAsync(
            target,
            cancellationToken);

        var diagnostics = rawDiagnostics
            .Select(configuration.Apply)
            .ToArray();

        return new AnalysisResult(
            Diagnostics: diagnostics,

           Rules: enabledRuleSet.AllRules
                .Select(rule => rule.Descriptor)
                .ToArray(),

            BaseDirectory:
                DotRadarTargetPath.GetBaseDirectory(target));
    }

    private static void EnsureMsBuildRegistered()
    {
        lock (MsBuildLock)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }
    }

    private static void PrintUsage(TextWriter output)
    {
        output.WriteLine(
            "DotRadar - Production diagnostics for .NET projects");

        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  dotradar <command> [options]");
        output.WriteLine();

        output.WriteLine("Commands:");

        output.WriteLine(
            "  scan         Scan a project or solution");

        output.WriteLine(
            "  baseline     Record existing diagnostics");

        output.WriteLine(
            "  list-rules   List available diagnostic rules");

        output.WriteLine();

        output.WriteLine("Global options:");
        output.WriteLine("  -h, --help       Show help");
        output.WriteLine("  -v, --version    Show version");
        output.WriteLine();

        output.WriteLine(
            "Run 'dotradar <command> --help' for details.");
    }

    private sealed record AnalysisResult(
        IReadOnlyList<DotRadarDiagnostic> Diagnostics,
        IReadOnlyList<DotRadarRuleDescriptor> Rules,
        string BaseDirectory);

    private static bool IsHelp(string value)
    {
        return value.Equals(
                   "--help",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "-h",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "help",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVersion(string value)
    {
        return value.Equals(
                   "--version",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "-v",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static int PrintCommandHelp(
        string command,
        TextWriter output,
        TextWriter errorOutput)
    {
        if (command.Equals(
                "scan",
                StringComparison.OrdinalIgnoreCase))
        {
            PrintScanHelp(output);
            return ExitCodes.Success;
        }

        if (command.Equals(
                "baseline",
                StringComparison.OrdinalIgnoreCase))
        {
            PrintBaselineHelp(output);
            return ExitCodes.Success;
        }

        if (command.Equals(
                "list-rules",
                StringComparison.OrdinalIgnoreCase))
        {
            PrintListRulesHelp(output);
            return ExitCodes.Success;
        }

        errorOutput.WriteLine($"Unknown command: {command}");
        PrintUsage(errorOutput);

        return ExitCodes.InvalidArguments;
    }

    private static void PrintScanHelp(TextWriter output)
    {
        output.WriteLine("Scan a .NET project or solution.");
        output.WriteLine();

        output.WriteLine("Usage:");
        output.WriteLine(
            "  dotradar scan <path> [options]");

        output.WriteLine();
        output.WriteLine("Options:");

        output.WriteLine(
            "  --format <text|json|sarif>       " +
            "Output format");

        output.WriteLine(
            "  --config <path>                  " +
            "Configuration file");

        output.WriteLine(
            "  --baseline <path>                " +
            "Suppress baseline diagnostics");

        output.WriteLine(
            "  --fail-on <info|warning|error>   " +
            "Failure threshold");

        output.WriteLine(
            "  -h, --help                       " +
            "Show command help");
    }

    private static void PrintBaselineHelp(TextWriter output)
    {
        output.WriteLine(
            "Create a baseline from existing diagnostics.");

        output.WriteLine();
        output.WriteLine("Usage:");

        output.WriteLine(
            "  dotradar baseline <path> [options]");

        output.WriteLine();
        output.WriteLine("Options:");

        output.WriteLine(
            "  --output <path>   Baseline output path");

        output.WriteLine(
            "  --config <path>   Configuration file");

        output.WriteLine(
            "  -h, --help        Show command help");
    }

    private static void PrintListRulesHelp(TextWriter output)
    {
        output.WriteLine(
            "List all diagnostic rules available in DotRadar.");

        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  dotradar list-rules");
    }
}