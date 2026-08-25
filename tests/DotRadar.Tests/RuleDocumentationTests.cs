using System.Text.Json;

using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class RuleDocumentationTests
{
    [Fact]
    public void Every_rule_has_documentation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rules = RuleRegistry.CreateDefault();

        Assert.All(rules, rule =>
        {
            var documentationPath = Path.Combine(
                repositoryRoot,
                rule.Descriptor.DocumentationPath);

            Assert.True(
                File.Exists(documentationPath),
                $"Documentation not found: " +
                $"{rule.Descriptor.DocumentationPath}");
        });
    }

    [Fact]
    public void Configuration_schema_is_valid_json()
    {
        var repositoryRoot = FindRepositoryRoot();

        var schemaPath = Path.Combine(
            repositoryRoot,
            "schemas",
            "dotradar.schema.json");

        Assert.True(
            File.Exists(schemaPath),
            $"Schema not found: {schemaPath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(schemaPath));

        Assert.Equal(
            "https://json-schema.org/draft/2020-12/schema",
            document.RootElement
                .GetProperty("$schema")
                .GetString());
    }

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "README.md")) &&
                Directory.Exists(Path.Combine(
                    directory.FullName,
                    "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Unable to locate DotRadar repository root.");
    }

    [Theory]
    [InlineData("docs/getting-started.md")]
    [InlineData("docs/configuration.md")]
    [InlineData("docs/baseline.md")]
    [InlineData("docs/ci.md")]
    [InlineData("schemas/dotradar.schema.json")]
    public void Required_documentation_exists(
    string relativePath)
    {
        var repositoryRoot = FindRepositoryRoot();

        var fullPath = Path.Combine(
            repositoryRoot,
            relativePath);

        Assert.True(
            File.Exists(fullPath),
            $"Required documentation not found: {relativePath}");
    }
}