using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class RuleRegistryTests
{
    [Fact]
    public void Default_rules_have_unique_rule_ids()
    {
        var rules = RuleRegistry.CreateDefault();

        Assert.NotEmpty(rules);

        var distinctRuleIds = rules
            .Select(rule => rule.RuleId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.Equal(rules.Count, distinctRuleIds);
    }

    [Fact]
    public void Default_rules_include_DTR1101()
    {
        var rules = RuleRegistry.CreateDefault();

        Assert.Contains(
            rules,
            rule => rule.RuleId == "DTR1101");
    }

    [Fact]
    public void Scanner_rejects_duplicate_rule_ids()
    {
        var duplicateRules = new IDotRadarRule[]
        {
            new Dtr1101Rule(),
            new Dtr1101Rule()
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new DotRadarScanner(duplicateRules));

        Assert.Contains("DTR1101", exception.Message);
    }
}