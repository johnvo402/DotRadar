using DotRadar.Analysis.Roslyn;
using DotRadar.Core;
using DotRadar.Tool;

using Microsoft.Build.Locator;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    if (args.Length == 1 &&
        args[0].Equals(
            "list-rules",
            StringComparison.OrdinalIgnoreCase))
    {
        return ListRulesCommand.Execute(Console.Out);
    }

    if (!ScanCommandOptions.TryParse(
            args,
            out var options,
            out var error))
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine();

        PrintUsage(Console.Error);

        return ExitCodes.InvalidArguments;
    }



    try
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        var availableRules = RuleRegistry.CreateDefault();

        var configurationPath =
            DotRadarConfigurationLocator.Find(
                options.Target,
                options.ConfigPath);

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
            options.Target,
            CancellationToken.None);

        var diagnostics = rawDiagnostics
            .Select(configuration.Apply)
            .ToArray();



        DiagnosticOutputWriter.Write(
     diagnostics,
     options.Format,
     Console.Out);

        return diagnostics.Length == 0
            ? ExitCodes.Success
            : ExitCodes.DiagnosticsFound;

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
    catch (DotRadarConfigurationException exception)
    {
        Console.Error.WriteLine(
            $"Configuration error: {exception.Message}");

        return ExitCodes.InvalidArguments;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(
            $"Unexpected error: {exception.Message}");

        return ExitCodes.InternalError;
    }
}

static void PrintUsage(TextWriter output)
{
    output.WriteLine("Usage:");
    output.WriteLine(
     "  dotradar scan <path> " +
     "[--format text|json] [--config <path>]");
    output.WriteLine(
        "  dotradar list-rules");
}