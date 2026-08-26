namespace DotRadar.Analysis.Roslyn;

public static class ProjectFileDiscovery
{
    public static IReadOnlyList<string>
        FindConfigurationFiles(
            string? projectFilePath)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
        {
            return Array.Empty<string>();
        }

        var fullProjectPath =
            Path.GetFullPath(projectFilePath);

        var projectDirectory =
            Path.GetDirectoryName(fullProjectPath);

        if (projectDirectory is null ||
            !Directory.Exists(projectDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(
                projectDirectory,
                "appsettings*.json",
                SearchOption.TopDirectoryOnly)
            .Where(IsAppSettingsFile)
            .OrderBy(
                path => path,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsAppSettingsFile(
        string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        return fileName.Equals(
                   "appsettings.json",
                   StringComparison.OrdinalIgnoreCase)
               || (
                   fileName.StartsWith(
                       "appsettings.",
                       StringComparison.OrdinalIgnoreCase)
                   && fileName.EndsWith(
                       ".json",
                       StringComparison.OrdinalIgnoreCase));
    }
}