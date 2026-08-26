namespace DotRadar.Analysis.Roslyn;

public sealed class FileAnalysisContext
{
    private FileAnalysisContext(
        ProjectAnalysisContext projectContext,
        string filePath,
        string relativePath,
        string content)
    {
        ProjectContext = projectContext;
        FilePath = filePath;
        RelativePath = relativePath;
        Content = content;
    }

    public ProjectAnalysisContext ProjectContext { get; }

    public string FilePath { get; }

    public string RelativePath { get; }

    public string Content { get; }

    public static async Task<FileAnalysisContext?> CreateAsync(
        ProjectAnalysisContext projectContext,
        string filePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullFilePath = Path.GetFullPath(filePath);

        if (!File.Exists(fullFilePath))
        {
            return null;
        }

        var projectDirectory = GetProjectDirectory(
            projectContext,
            fullFilePath);

        var relativePath = Path.GetRelativePath(
            projectDirectory,
            fullFilePath);

        if (IsOutsideProject(relativePath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(
            fullFilePath,
            cancellationToken);

        return new FileAnalysisContext(
            projectContext,
            fullFilePath,
            NormalizePath(relativePath),
            content);
    }

    private static string GetProjectDirectory(
        ProjectAnalysisContext projectContext,
        string fullFilePath)
    {
        if (projectContext.FilePath is null)
        {
            return Path.GetDirectoryName(fullFilePath)
                ?? Directory.GetCurrentDirectory();
        }

        var projectFilePath = Path.GetFullPath(
            projectContext.FilePath);

        return Path.GetDirectoryName(projectFilePath)
            ?? Directory.GetCurrentDirectory();
    }

    private static bool IsOutsideProject(
        string relativePath)
    {
        return relativePath.Equals(
                   "..",
                   StringComparison.Ordinal)
               || relativePath.StartsWith(
                   $"..{Path.DirectorySeparatorChar}",
                   StringComparison.Ordinal)
               || relativePath.StartsWith(
                   $"..{Path.AltDirectorySeparatorChar}",
                   StringComparison.Ordinal)
               || Path.IsPathRooted(relativePath);
    }

    private static string NormalizePath(
        string path)
    {
        return path.Replace(
            Path.DirectorySeparatorChar,
            '/');
    }
}