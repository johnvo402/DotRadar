using System.Text.Json;

using DotRadar.Abstractions;
using DotRadar.Core;

namespace DotRadar.Tool;

internal static class SarifOutputWriter
{
    private const string Schema =
        "https://json.schemastore.org/sarif-2.1.0.json";

    private const string DocumentationBaseUrl =
        "https://github.com/johnvo402/DotRadar/blob/main/";

    public static void Write(
        DiagnosticReport report,
        TextWriter output)
    {
        var rules = report.Rules
            .OrderBy(rule => rule.RuleId)
            .ToArray();

        var ruleIndexes = rules
            .Select((rule, index) => new
            {
                rule.RuleId,
                Index = index
            })
            .ToDictionary(
                item => item.RuleId,
                item => item.Index,
                StringComparer.OrdinalIgnoreCase);

        var fingerprintGenerator =
            new DiagnosticFingerprintGenerator(
                report.BaseDirectory);

        var sarifRules = rules.Select(rule => new
        {
            id = rule.RuleId,
            name = rule.RuleId,

            shortDescription = new
            {
                text = rule.Title
            },

            fullDescription = new
            {
                text = rule.Description
            },

            defaultConfiguration = new
            {
                level = ToSarifLevel(rule.DefaultSeverity)
            },

            helpUri =
                DocumentationBaseUrl +
                rule.DocumentationPath.Replace('\\', '/'),

            properties = new
            {
                tags = new[]
                {
                    rule.Category
                        .ToString()
                        .ToLowerInvariant()
                },

                precision = ToSarifPrecision(
                    rule.Confidence),

                problem = new
                {
                    severity = ToProblemSeverity(
                        rule.DefaultSeverity)
                }
            }
        });

        var results = report.Diagnostics.Select(
            diagnostic =>
            {
                var relativePath =
                    fingerprintGenerator.GetRelativeFilePath(
                        diagnostic.FilePath);

                return new
                {
                    ruleId = diagnostic.RuleId,

                    ruleIndex = ruleIndexes.TryGetValue(
                        diagnostic.RuleId,
                        out var index)
                            ? index
                            : 0,

                    level = ToSarifLevel(
                        diagnostic.Severity),

                    message = new
                    {
                        text = diagnostic.Message
                    },

                    locations = new[]
                    {
                        new
                        {
                            physicalLocation = new
                            {
                                artifactLocation = new
                                {
                                    uri = ToArtifactUri(
                                        relativePath)
                                },

                                region = new
                                {
                                    startLine = Math.Max(
                                        diagnostic.Line,
                                        1),

                                    startColumn = Math.Max(
                                        diagnostic.Column,
                                        1)
                                }
                            }
                        }
                    },

                    partialFingerprints = new Dictionary<
                        string,
                        string>
                    {
                        ["primaryLocationLineHash"] =
                            fingerprintGenerator.Create(
                                diagnostic)
                    }
                };
            });

        var run = new
        {
            tool = new
            {
                driver = new
                {
                    name = "DotRadar",
                    semanticVersion = GetToolVersion(),

                    informationUri =
                        "https://github.com/johnvo402/DotRadar",

                    rules = sarifRules
                }
            },

            results,

            properties = new
            {
                suppressedCount = report.SuppressedCount,

                failureThreshold = report.FailureThreshold
                    .ToString()
                    .ToLowerInvariant(),

                failureCount = report.FailureCount
            }
        };

        var sarif = new Dictionary<string, object?>
        {
            ["$schema"] = Schema,
            ["version"] = "2.1.0",
            ["runs"] = new[] { run }
        };

        var json = JsonSerializer.Serialize(
            sarif,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        output.WriteLine(json);
    }

    private static string ToSarifLevel(
        DotRadarSeverity severity)
    {
        return severity switch
        {
            DotRadarSeverity.Info => "note",
            DotRadarSeverity.Warning => "warning",
            DotRadarSeverity.Error => "error",

            _ => throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                "Unsupported SARIF severity.")
        };
    }

    private static string ToProblemSeverity(
        DotRadarSeverity severity)
    {
        return severity switch
        {
            DotRadarSeverity.Info => "recommendation",
            DotRadarSeverity.Warning => "warning",
            DotRadarSeverity.Error => "error",

            _ => throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                "Unsupported SARIF severity.")
        };
    }

    private static string ToArtifactUri(string relativePath)
    {
        return string.Join(
            "/",
            relativePath
                .Replace('\\', '/')
                .Split('/')
                .Select(Uri.EscapeDataString));
    }

    private static string GetToolVersion()
    {
        var version = typeof(SarifOutputWriter)
            .Assembly
            .GetName()
            .Version;

        return version is null
            ? "0.1.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
    private static string ToSarifPrecision(
    DotRadarConfidence confidence)
    {
        return confidence switch
        {
            DotRadarConfidence.Low => "low",
            DotRadarConfidence.Medium => "medium",
            DotRadarConfidence.High => "high",

            _ => throw new ArgumentOutOfRangeException(
                nameof(confidence),
                confidence,
                "Unsupported rule confidence.")
        };
    }
}