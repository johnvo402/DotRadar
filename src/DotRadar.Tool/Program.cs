using DotRadar.Analysis.Roslyn;
using DotRadar.Core;

using Microsoft.Build.Locator;

if (args.Length != 2 ||
    !string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: dotradar scan <solution|project|directory>");
    return ExitCodes.InvalidArguments;
}

if (!MSBuildLocator.IsRegistered)
{
    MSBuildLocator.RegisterDefaults();
}

return await RunScanAsync(args[1]);

static async Task<int> RunScanAsync(string target)
{
    try
    {
        var scanner = new DotRadarScanner();

        var diagnostics = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        if (diagnostics.Count == 0)
        {
            Console.WriteLine("No production risks found.");
            return ExitCodes.Success;
        }

        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine(
                $"{diagnostic.RuleId}  " +
                $"{diagnostic.Severity,-7}  " +
                $"{diagnostic.FilePath}:" +
                $"{diagnostic.Line}:" +
                $"{diagnostic.Column}");

            Console.WriteLine($"  {diagnostic.Message}");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"{diagnostics.Count} diagnostic(s) found.");

        return ExitCodes.DiagnosticsFound;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(
            $"Failed to scan target: {exception.Message}");

        return ExitCodes.ProjectLoadFailure;
    }
}