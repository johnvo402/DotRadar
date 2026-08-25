using System.Text.Json;

namespace DotRadar.Core;

public static class DotRadarBaselineStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

    public static DotRadarBaseline Load(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"DotRadar baseline not found: {fullPath}",
                fullPath);
        }

        try
        {
            var json = File.ReadAllText(fullPath);

            var baseline =
                JsonSerializer.Deserialize<DotRadarBaseline>(
                    json,
                    JsonOptions)
                ?? throw new DotRadarBaselineException(
                    "Baseline file is empty.");

            Validate(baseline);

            return baseline;
        }
        catch (JsonException exception)
        {
            throw new DotRadarBaselineException(
                $"Invalid baseline file '{fullPath}': " +
                exception.Message,
                exception);
        }
    }

    public static void Save(
        string path,
        DotRadarBaseline baseline)
    {
        Validate(baseline);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(
            baseline,
            JsonOptions);

        File.WriteAllText(
            fullPath,
            json + Environment.NewLine);
    }

    private static void Validate(DotRadarBaseline baseline)
    {
        if (baseline.Version != DotRadarBaseline.CurrentVersion)
        {
            throw new DotRadarBaselineException(
                $"Unsupported baseline version: {baseline.Version}. " +
                $"Supported version: " +
                $"{DotRadarBaseline.CurrentVersion}.");
        }

        if (baseline.Diagnostics is null)
        {
            throw new DotRadarBaselineException(
                "Baseline diagnostics cannot be null.");
        }

        var invalidEntry = baseline.Diagnostics.FirstOrDefault(
            entry =>
                string.IsNullOrWhiteSpace(entry.Fingerprint) ||
                string.IsNullOrWhiteSpace(entry.RuleId) ||
                string.IsNullOrWhiteSpace(entry.FilePath));

        if (invalidEntry is not null)
        {
            throw new DotRadarBaselineException(
                "Baseline contains an invalid diagnostic entry.");
        }
    }
}