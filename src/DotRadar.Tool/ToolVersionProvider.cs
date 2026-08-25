using System.Reflection;

namespace DotRadar.Tool;

internal static class ToolVersionProvider
{
    public static string GetVersion()
    {
        var assembly = typeof(ToolVersionProvider).Assembly;

        var informationalVersion = assembly
            .GetCustomAttribute<
                AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(
                informationalVersion))
        {
            var metadataIndex =
                informationalVersion.IndexOf('+');

            return metadataIndex >= 0
                ? informationalVersion[..metadataIndex]
                : informationalVersion;
        }

        var version = assembly.GetName().Version;

        return version is null
            ? "0.0.0"
            : $"{version.Major}.{version.Minor}." +
              $"{Math.Max(version.Build, 0)}";
    }
}