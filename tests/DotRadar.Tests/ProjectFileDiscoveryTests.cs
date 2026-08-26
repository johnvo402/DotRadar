using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class ProjectFileDiscoveryTests :
    IDisposable
{
    private readonly string _directory;

    public ProjectFileDiscoveryTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            $"dotradar-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Finds_only_top_level_appsettings_files()
    {
        var projectFile = Path.Combine(
            _directory,
            "TestProject.csproj");

        var defaultSettings = Path.Combine(
            _directory,
            "appsettings.json");

        var developmentSettings = Path.Combine(
            _directory,
            "appsettings.Development.json");

        var invalidSettings = Path.Combine(
            _directory,
            "appsettingsLocal.json");

        var otherJson = Path.Combine(
            _directory,
            "other.json");

        File.WriteAllText(projectFile, "<Project />");
        File.WriteAllText(defaultSettings, "{}");
        File.WriteAllText(developmentSettings, "{}");
        File.WriteAllText(invalidSettings, "{}");
        File.WriteAllText(otherJson, "{}");

        var files =
            ProjectFileDiscovery.FindConfigurationFiles(
                projectFile);

        Assert.Equal(2, files.Count);
        Assert.Contains(defaultSettings, files);
        Assert.Contains(developmentSettings, files);
        Assert.DoesNotContain(invalidSettings, files);
        Assert.DoesNotContain(otherJson, files);
    }

    [Fact]
    public void Returns_empty_when_project_path_is_missing()
    {
        var files =
            ProjectFileDiscovery.FindConfigurationFiles(
                projectFilePath: null);

        Assert.Empty(files);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(
                _directory,
                recursive: true);
        }
    }
}