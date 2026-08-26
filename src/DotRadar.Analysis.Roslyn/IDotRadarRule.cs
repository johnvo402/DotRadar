using DotRadar.Abstractions;

namespace DotRadar.Analysis.Roslyn;

public interface IDotRadarRule
{
    string RuleId { get; }

    DotRadarRuleDescriptor Descriptor { get; }
}