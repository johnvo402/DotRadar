using DotRadar.Analysis.Roslyn;

using Xunit;

namespace DotRadar.Tests;

public sealed class RuleContractTests
{
    [Fact]
    public void Document_rule_inherits_base_rule_contract()
    {
        Assert.True(
            typeof(IDotRadarRule)
                .IsAssignableFrom(typeof(IDocumentRule)));
    }

    [Fact]
    public void Default_registry_contains_document_rules()
    {
        var rules = RuleRegistry.CreateDefault();

        Assert.NotEmpty(rules);

        Assert.All(
            rules,
            rule => Assert.IsAssignableFrom<IDocumentRule>(rule));
    }
}