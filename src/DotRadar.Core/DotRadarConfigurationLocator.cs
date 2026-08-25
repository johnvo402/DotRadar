namespace DotRadar.Core;

public static class DotRadarConfigurationLocator
{
    private const string DefaultFileName = ".dotradar.json";

    public static string? Find(
        string target,
        string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var fullPath = Path.GetFullPath(explicitPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"DotRadar configuration not found: {fullPath}",
                    fullPath);
            }

            return fullPath;
        }

        var fullTargetPath = Path.GetFullPath(target);

        var directory = Directory.Exists(fullTargetPath)
            ? fullTargetPath
            : Path.GetDirectoryName(fullTargetPath);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory,
                DefaultFileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
    }
}