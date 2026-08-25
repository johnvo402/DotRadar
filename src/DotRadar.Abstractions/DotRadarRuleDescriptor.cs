namespace DotRadar.Abstractions;

public sealed class DotRadarRuleDescriptor
{
    public DotRadarRuleDescriptor(
        string ruleId,
        string title,
        string description,
        DotRadarCategory category,
        DotRadarSeverity defaultSeverity,
        string documentationPath)
    {
        RuleId = ruleId;
        Title = title;
        Description = description;
        Category = category;
        DefaultSeverity = defaultSeverity;
        DocumentationPath = documentationPath;
    }

    public string RuleId { get; }

    public string Title { get; }

    public string Description { get; }

    public DotRadarCategory Category { get; }

    public DotRadarSeverity DefaultSeverity { get; }

    public string DocumentationPath { get; }
}