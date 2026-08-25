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
            errorOutput.WriteLine("Scan cancelled.");
            return ExitCodes.InternalError;
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
        EnsureMsBuildRegistered();

        var availableRules = RuleRegistry.CreateDefault();

        var configurationPath =
            DotRadarConfigurationLocator.Find(
                target,
                configPath);

        var configuration =
            DotRadarConfigurationLoader.Load(
                configurationPath);

        configuration.ValidateKnownRules(
            availableRules.Select(rule => rule.RuleId));

        var enabledRules = availableRules
            .Where(rule =>
                configuration.IsEnabled(rule.RuleId))
            .ToArray();

        var scanner = new DotRadarScanner(enabledRules);

        var rawDiagnostics = await scanner.ScanAsync(
            target,
            cancellationToken);

        var diagnostics = rawDiagnostics
            .Select(configuration.Apply)
            .ToArray();

        return new AnalysisResult(
            Diagnostics: diagnostics,

            Rules: enabledRules
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
        output.WriteLine("Usage:");

        output.WriteLine(
            "  dotradar scan <path> " +
            "[--format text|json|sarif] " +
            "[--config <path>] " +
            "[--baseline <path>] " +
            "[--fail-on info|warning|error]");

        output.WriteLine(
            "  dotradar baseline <path> " +
            "[--output <path>] " +
            "[--config <path>]");

        output.WriteLine(
            "  dotradar list-rules");
    }

    private sealed record AnalysisResult(
        IReadOnlyList<DotRadarDiagnostic> Diagnostics,
        IReadOnlyList<DotRadarRuleDescriptor> Rules,
        string BaseDirectory);
}