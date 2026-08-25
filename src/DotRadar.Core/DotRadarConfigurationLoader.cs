using System.Text.Json;

using DotRadar.Abstractions;

namespace DotRadar.Core;

public static class DotRadarConfigurationLoader
{
    public static DotRadarConfiguration Load(string? path)
    {
        if (path is null)
        {
            return CreateEmpty();
        }

        try
        {
            var json = File.ReadAllText(path);
            return Parse(json);
        }
        catch (JsonException exception)
        {
            throw new DotRadarConfigurationException(
                $"Invalid DotRadar configuration '{path}': " +
                exception.Message,
                exception);
        }
    }

    public static DotRadarConfiguration Parse(string json)
    {
        using var document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new DotRadarConfigurationException(
                "DotRadar configuration must be a JSON object.");
        }

        if (!root.TryGetProperty("rules", out var rulesElement))
        {
            return CreateEmpty();
        }

        if (rulesElement.ValueKind != JsonValueKind.Object)
        {
            throw new DotRadarConfigurationException(
                "The 'rules' property must be a JSON object.");
        }

        var rules = new Dictionary<
            string,
            DotRadarRuleConfiguration>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var ruleProperty in rulesElement.EnumerateObject())
        {
            if (ruleProperty.Value.ValueKind != JsonValueKind.Object)
            {
                throw new DotRadarConfigurationException(
                    $"Configuration for rule '{ruleProperty.Name}' " +
                    "must be a JSON object.");
            }

            var enabled = ReadEnabled(
                ruleProperty.Name,
                ruleProperty.Value);

            var severity = ReadSeverity(
                ruleProperty.Name,
                ruleProperty.Value);

            if (!rules.TryAdd(
                    ruleProperty.Name,
                    new DotRadarRuleConfiguration(
                        enabled,
                        severity)))
            {
                throw new DotRadarConfigurationException(
                    $"Rule '{ruleProperty.Name}' is configured more " +
                    "than once.");
            }
        }

        return new DotRadarConfiguration(rules);
    }

    private static bool ReadEnabled(
        string ruleId,
        JsonElement ruleElement)
    {
        if (!ruleElement.TryGetProperty(
                "enabled",
                out var enabledElement))
        {
            return true;
        }

        if (enabledElement.ValueKind is not
            JsonValueKind.True and not JsonValueKind.False)
        {
            throw new DotRadarConfigurationException(
                $"'enabled' for rule '{ruleId}' must be boolean.");
        }

        return enabledElement.GetBoolean();
    }

    private static DotRadarSeverity? ReadSeverity(
        string ruleId,
        JsonElement ruleElement)
    {
        if (!ruleElement.TryGetProperty(
                "severity",
                out var severityElement))
        {
            return null;
        }

        if (severityElement.ValueKind != JsonValueKind.String)
        {
            throw new DotRadarConfigurationException(
                $"'severity' for rule '{ruleId}' must be a string.");
        }

        var value = severityElement.GetString();

        if (!Enum.TryParse<DotRadarSeverity>(
                value,
                ignoreCase: true,
                out var severity) ||
            !Enum.IsDefined(severity))
        {
            throw new DotRadarConfigurationException(
                $"Unsupported severity '{value}' for rule " +
                $"'{ruleId}'. Supported values: info, warning, error.");
        }

        return severity;
    }

    private static DotRadarConfiguration CreateEmpty()
    {
        return new DotRadarConfiguration(
            new Dictionary<string, DotRadarRuleConfiguration>(
                StringComparer.OrdinalIgnoreCase));
    }
}