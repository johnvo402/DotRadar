using DotRadar.Abstractions;
using DotRadar.Core;

using Xunit;

namespace DotRadar.Tests;

public sealed class DiagnosticFingerprintTests
{
    [Fact]
    public void Fingerprint_survives_inserted_lines_above_issue()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "Service.cs");

        try
        {
            File.WriteAllText(
                filePath,
                "return task.Result;");

            var firstDiagnostic = CreateDiagnostic(
                filePath,
                line: 1);

            var firstFingerprint =
                new DiagnosticFingerprintGenerator(directory)
                    .Create(firstDiagnostic);

            File.WriteAllText(
                filePath,
                "\n\nreturn task.Result;");

            var movedDiagnostic = CreateDiagnostic(
                filePath,
                line: 3);

            var movedFingerprint =
                new DiagnosticFingerprintGenerator(directory)
                    .Create(movedDiagnostic);

            Assert.Equal(
                firstFingerprint,
                movedFingerprint);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Different_rules_have_different_fingerprints()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "Service.cs");

        try
        {
            File.WriteAllText(
                filePath,
                "return task.Result;");

            var generator =
                new DiagnosticFingerprintGenerator(directory);

            var first = generator.Create(
                CreateDiagnostic(
                    filePath,
                    line: 1,
                    ruleId: "DTR1101"));

            var second = generator.Create(
                CreateDiagnostic(
                    filePath,
                    line: 1,
                    ruleId: "DTR1102"));

            Assert.NotEqual(first, second);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static DotRadarDiagnostic CreateDiagnostic(
        string filePath,
        int line,
        string ruleId = "DTR1101")
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