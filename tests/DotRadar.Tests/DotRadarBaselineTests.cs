using DotRadar.Abstractions;
using DotRadar.Core;

using Xunit;

namespace DotRadar.Tests;

public sealed class DotRadarBaselineTests
{
    [Fact]
    public void Filters_existing_diagnostics()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "Service.cs");

        try
        {
            File.WriteAllLines(
                filePath,
                [
                    "return task.Result;",
                    "task.Wait();"
                ]);

            var existing = CreateDiagnostic(
                "DTR1101",
                filePath,
                line: 1);

            var newDiagnostic = CreateDiagnostic(
                "DTR1101",
                filePath,
                line: 2);

            var baseline = DotRadarBaselineService.Create(
                [existing],
                directory);

            var remaining = DotRadarBaselineService.Filter(
                [existing, newDiagnostic],
                baseline,
                directory);

            var diagnostic = Assert.Single(remaining);

            Assert.Equal(2, diagnostic.Line);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Can_save_and_load_baseline()
    {
        var directory = CreateTemporaryDirectory();
        var sourcePath = Path.Combine(directory, "Service.cs");
        var baselinePath = Path.Combine(
            directory,
            ".dotradar-baseline.json");

        try
        {
            File.WriteAllText(
                sourcePath,
                "return task.Result;");

            var baseline = DotRadarBaselineService.Create(
                [
                    CreateDiagnostic(
                        "DTR1101",
                        sourcePath,
                        line: 1)
                ],
                directory);

            DotRadarBaselineStore.Save(
                baselinePath,
                baseline);

            var loaded =
                DotRadarBaselineStore.Load(baselinePath);

            Assert.Equal(1, loaded.Version);

            var entry = Assert.Single(
                loaded.Diagnostics);

            Assert.Equal("DTR1101", entry.RuleId);
            Assert.Equal("Service.cs", entry.FilePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static DotRadarDiagnostic CreateDiagnostic(
        string ruleId,
        string filePath,
        int line)
    {
        return new DotRadarDiagnostic(
            ruleId: ruleId,
            title: "Test diagnostic",
            message: "Test message",
            severity: DotRadarSeverity.Warning,
            filePath: filePath,
            line: line,
            column: 1);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "dotradar-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);

        return path;
    }
}