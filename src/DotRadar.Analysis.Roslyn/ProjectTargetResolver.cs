namespace DotRadar.Analysis.Roslyn;

internal static class ProjectTargetResolver
{
    public static string Resolve(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                "A solution, project, or directory path is required.",
                nameof(input));
        }

        var fullPath = Path.GetFullPath(input);

        if (File.Exists(fullPath))
        {
            ValidateExtension(fullPath);
            return fullPath;
        }

        if (!Directory.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Target '{fullPath}' does not exist.",
                fullPath);
        }

        var solutions = Directory
            .EnumerateFiles(fullPath, "*", SearchOption.TopDirectoryOnly)
            .Where(IsSolution)
            .ToArray();

        if (solutions.Length == 1)
        {
            return solutions[0];
        }

        if (solutions.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple solutions were found in '{fullPath}'. " +
                "Pass a specific .sln or .slnx file.");
        }

        var projects = Directory
            .EnumerateFiles(fullPath, "*.csproj", SearchOption.TopDirectoryOnly)
            .ToArray();

        return projects.Length switch
        {
            1 => projects[0],

            0 => throw new FileNotFoundException(
                $"No .sln, .slnx, or .csproj file was found in '{fullPath}'."),

            _ => throw new InvalidOperationException(
                $"Multiple projects were found in '{fullPath}'. " +
                "Pass a specific .csproj file.")
        };
    }

    private static void ValidateExtension(string path)
    {
        if (!IsSolution(path) &&
            !path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only .sln, .slnx, and .csproj targets are supported.");
        }
    }

    private static bool IsSolution(string path)
    {
        return path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
    }
}