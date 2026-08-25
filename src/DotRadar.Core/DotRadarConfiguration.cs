using DotRadar.Abstractions;

namespace DotRadar.Core;

public sealed class DotRadarConfiguration
{
    private readonly IReadOnlyDictionary<
        string,
        DotRadarRuleConfiguration> _rules;

    internal DotRadarConfiguration(
        IReadOnlyDictionary<string, DotRadarRuleConfiguration> rules)
    {
        _rules = rules;
    }

    public IReadOnlyCollection<string> ConfiguredRuleIds =>
        _rules.Keys.ToArray();

    public bool IsEnabled(string ruleId)
    {
        return !_rules.TryGetValue(ruleId, out var configuration)
               || configuration.Enabled;
    }

    public DotRadarSeverity ResolveSeverity(
        string ruleId,
        DotRadarSeverity defaultSeverity)
    {
        if (_rules.TryGetValue(ruleId, out var configuration) &&
            configuration.Severity is not null)
        {
            return configuration.Severity.Value;
        }

        return defaultSeverity;
    }

    public DotRadarDiagnostic Apply(
        DotRadarDiagnostic diagnostic)
    {
        var severity = ResolveSeverity(
            diagnostic.RuleId,
            diagnostic.Severity);

        if (severity == diagnostic.Severity)
        {
            return diagnostic;
        }

        return new DotRadarDiagnostic(
            ruleId: diagnostic.RuleId,
            title: diagnostic.Title,
            message: diagnostic.Message,
            severity: severity,
            filePath: diagnostic.FilePath,
            line: diagnostic.Line,
            column: diagnostic.Column);
    }

    public void ValidateKnownRules(
        IEnumerable<string> knownRuleIds)
    {
        var knownRules = knownRuleIds.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        var unknownRule = _rules.Keys.FirstOrDefault(
            ruleId => !knownRules.Contains(ruleId));

        if (unknownRule is not null)
        {
            throw new DotRadarConfigurationException(
                $"Unknown rule ID in configuration: {unknownRule}");
        }
    }
}

internal sealed class DotRadarRuleConfiguration
{
    public DotRadarRuleConfiguration(
        bool enabled,
        DotRadarSeverity? severity)
    {
        Enabled = enabled;
        Severity = severity;
    }

    public bool Enabled { get; }

    public DotRadarSeverity? Severity { get; }
}