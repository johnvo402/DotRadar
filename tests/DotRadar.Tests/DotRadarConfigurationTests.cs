using DotRadar.Abstractions;
using DotRadar.Core;

using Xunit;

namespace DotRadar.Tests;

public sealed class DotRadarConfigurationTests
{
    [Fact]
    public void Can_disable_rule()
    {
        var configuration =
            DotRadarConfigurationLoader.Parse(
                """
                {
                  "rules": {
                    "DTR1102": {
                      "enabled": false
                    }
                  }
                }
                """);

        Assert.False(configuration.IsEnabled("DTR1102"));
        Assert.True(configuration.IsEnabled("DTR1101"));
    }

    [Fact]
    public void Can_override_severity()
    {
        var configuration =
            DotRadarConfigurationLoader.Parse(
                """
                {
                  "rules": {
                    "DTR1101": {
                      "severity": "error"
                    }
                  }
                }
                """);

        var diagnostic = new DotRadarDiagnostic(
            ruleId: "DTR1101",
            title: "Test",
            message: "Test message",
            severity: DotRadarSeverity.Warning,
            filePath: "Service.cs",
            line: 1,
            column: 1);

        var configured = configuration.Apply(diagnostic);

        Assert.Equal(
            DotRadarSeverity.Error,
            configured.Severity);
    }

    [Fact]
    public void Rejects_invalid_severity()
    {
        var exception =
            Assert.Throws<DotRadarConfigurationException>(
                () => DotRadarConfigurationLoader.Parse(
                    """
                    {
                      "rules": {
                        "DTR1101": {
                          "severity": "critical"
                        }
                      }
                    }
                    """));

        Assert.Contains("critical", exception.Message);
    }

    [Fact]
    public void Allows_schema_property()
    {
        var configuration =
            DotRadarConfigurationLoader.Parse(
                """
            {
              "$schema": "https://example.com/schema.json",
              "rules": {
                "DTR1101": {
                  "severity": "error"
                }
              }
            }
            """);

        Assert.Equal(
            DotRadarSeverity.Error,
            configuration.ResolveSeverity(
                "DTR1101",
                DotRadarSeverity.Warning));
    }

    [Fact]
    public void Rejects_unknown_root_property()
    {
        var exception =
            Assert.Throws<DotRadarConfigurationException>(
                () => DotRadarConfigurationLoader.Parse(
                    """
                {
                  "rule": {
                    "DTR1101": {
                      "severity": "error"
                    }
                  }
                }
                """));

        Assert.Contains(
            "Unknown property 'rule'",
            exception.Message);
    }

    [Fact]
    public void Rejects_unknown_rule_property()
    {
        var exception =
            Assert.Throws<DotRadarConfigurationException>(
                () => DotRadarConfigurationLoader.Parse(
                    """
                {
                  "rules": {
                    "DTR1101": {
                      "severty": "error"
                    }
                  }
                }
                """));

        Assert.Contains(
            "Unknown property 'severty'",
            exception.Message);
    }
}