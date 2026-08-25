using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using DotRadar.Abstractions;

namespace DotRadar.Core;

public sealed class DiagnosticFingerprintGenerator
{
    private readonly string _baseDirectory;

    private readonly Dictionary<string, string[]> _sourceFiles =
        new(StringComparer.OrdinalIgnoreCase);

    public DiagnosticFingerprintGenerator(string baseDirectory)
    {
        _baseDirectory = Path.GetFullPath(baseDirectory);
    }

    public string Create(DotRadarDiagnostic diagnostic)
    {
        var relativePath = GetRelativeFilePath(
            diagnostic.FilePath);

        var sourceLine = GetSourceLine(diagnostic);

        var locationIdentity = sourceLine is null
            ? $"line:{diagnostic.Line};column:{diagnostic.Column}"
            : NormalizeSourceLine(sourceLine);

        // Severity không được đưa vào fingerprint vì người dùng
        // có thể đổi severity trong .dotradar.json.
        var content =
            $"{diagnostic.RuleId.ToUpperInvariant()}\n" +
            $"{relativePath.ToLowerInvariant()}\n" +
            locationIdentity;

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }

    public string GetRelativeFilePath(string filePath)
    {
        var fullPath = Path.IsPathRooted(filePath)
            ? Path.GetFullPath(filePath)
            : Path.GetFullPath(filePath, _baseDirectory);

        return Path
            .GetRelativePath(_baseDirectory, fullPath)
            .Replace('\\', '/');
    }

    private string? GetSourceLine(
        DotRadarDiagnostic diagnostic)
    {
        if (diagnostic.Line <= 0)
        {
            return null;
        }

        var fullPath = Path.IsPathRooted(diagnostic.FilePath)
            ? Path.GetFullPath(diagnostic.FilePath)
            : Path.GetFullPath(
                diagnostic.FilePath,
                _baseDirectory);

        if (!_sourceFiles.TryGetValue(
                fullPath,
                out var lines))
        {
            try
            {
                lines = File.ReadAllLines(fullPath);
            }
            catch (IOException)
            {
                lines = [];
            }
            catch (UnauthorizedAccessException)
            {
                lines = [];
            }

            _sourceFiles[fullPath] = lines;
        }

        var index = diagnostic.Line - 1;

        return index >= 0 && index < lines.Length
            ? lines[index]
            : null;
    }

    private static string NormalizeSourceLine(string sourceLine)
    {
        return Regex.Replace(
            sourceLine.Trim(),
            @"\s+",
            " ");
    }
}