using DotRadar.Abstractions;
using DotRadar.Analysis.Roslyn;
using DotRadar.Core;
using DotRadar.Tool;

using Microsoft.Build.Locator;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    try
    {
        if (args.Length == 1 &&
            args[0].Equals(
                "list-rules",
                StringComparison.OrdinalIgnoreCase))
        {
            return ListRulesCommand.Execute(Console.Out);
        }

        if (args.Length > 0 &&
            args[0].Equals(
                "scan",
                StringComparison.OrdinalIgnoreCase))
        {
            return await RunScanAsync(args);
        }

        if (args.Length > 0 &&
            args[0].Equals(
                "baseline",
                StringComparison.OrdinalIgnoreCase))
        {
            return await RunBaselineAsync(args);
        }

        PrintUsage(Console.Error);
        return ExitCodes.InvalidArguments;
    }
    catch (DotRadarConfigurationException exception)
    {
        Console.Error.WriteLine(
            $"Configuration error: {exception.Message}");

        return ExitCodes.InvalidArguments;
    }
    catch (DotRadarBaselineException exception)
    {
        Console.Error.WriteLine(
            $"Baseline error: {exception.Message}");

        return ExitCodes.InvalidArguments;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Scan cancelled.");
        return ExitCodes.InternalError;
    }
    catch (FileNotFoundException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return ExitCodes.ProjectLoadFailure;
    }
    catch (InvalidOperationException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return ExitCodes.ProjectLoadFailure;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(
            $"Unexpected error: {exception.Message}");

        return ExitCodes.InternalError;
    }
}

static async Task<int> RunScanAsync(string[] args)
{
    if (!ScanCommandOptions.TryParse(
            args,
            out var options,
            out var error))
    {
        Console.Error.WriteLine(
            error ?? "Invalid scan arguments.");

        PrintUsage(Console.Error);

        return ExitCodes.InvalidArguments;
    }

    var result = await AnalyzeAsync(
        options.Target,
        options.ConfigPath,
        CancellationToken.None);

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

    DiagnosticOutputWriter.Write(
        diagnostics,
        options.Format,
        Console.Out,
        suppressedCount);

    return diagnostics.Count == 0
        ? ExitCodes.Success
        : ExitCodes.DiagnosticsFound;
}

static async Task<int> RunBaselineAsync(string[] args)
{
    if (!BaselineCommandOptions.TryParse(
            args,
            out var options,
            out var error))
    {
        Console.Error.WriteLine(
            error ?? "Invalid baseline arguments.");

        PrintUsage(Console.Error);

        return ExitCodes.InvalidArguments;
    }

    var result = await AnalyzeAsync(
        options.Target,
        options.ConfigPath,
        CancellationToken.None);

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

    Console.Out.WriteLine(
        $"Baseline created: {Path.GetFullPath(outputPath)}");

    Console.Out.WriteLine(
        $"Diagnostics recorded: {baseline.Diagnostics.Count}");

    return ExitCodes.Success;
}

static async Task<AnalysisResult> AnalyzeAsync(
    string target,
    string? configPath,
    CancellationToken cancellationToken)
{
    if (!MSBuildLocator.IsRegistered)
    {
        MSBuildLocator.RegisterDefaults();
    }

    var availableRules = RuleRegistry.CreateDefault();

    var configurationPath =
        DotRadarConfigurationLocator.Find(
            target,
            configPath);

    var configuration =
        DotRadarConfigurationLoader.Load(configurationPath);

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
        BaseDirectory:
            DotRadarTargetPath.GetBaseDirectory(target));
}

static void PrintUsage(TextWriter output)
{
    output.WriteLine("Usage:");

    output.WriteLine(
        "  dotradar scan <path> " +
        "[--format text|json] " +
        "[--config <path>] " +
        "[--baseline <path>]");

    output.WriteLine(
        "  dotradar baseline <path> " +
        "[--output <path>] " +
        "[--config <path>]");

    output.WriteLine(
        "  dotradar list-rules");
}

internal sealed record AnalysisResult(
    IReadOnlyList<DotRadarDiagnostic> Diagnostics,
    string BaseDirectory);