namespace DotRadar.Core;

public static class DotRadarTargetPath
{
    public static string GetBaseDirectory(string target)
    {
        var fullPath = Path.GetFullPath(target);

        if (Directory.Exists(fullPath))
        {
            return fullPath;
        }

        return Path.GetDirectoryName(fullPath)
               ?? Directory.GetCurrentDirectory();
    }
}