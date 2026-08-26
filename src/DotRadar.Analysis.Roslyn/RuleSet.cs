namespace DotRadar.Analysis.Roslyn;

public sealed class RuleSet
{
    public RuleSet(
        IEnumerable<IDocumentRule>? documentRules = null,
        IEnumerable<IProjectRule>? projectRules = null,
        IEnumerable<IFileRule>? fileRules = null)
    {
        DocumentRules = (
            documentRules ??
            Array.Empty<IDocumentRule>())
            .ToArray();

        ProjectRules = (
            projectRules ??
            Array.Empty<IProjectRule>())
            .ToArray();

        FileRules = (
            fileRules ??
            Array.Empty<IFileRule>())
            .ToArray();

        AllRules = DocumentRules
            .Cast<IDotRadarRule>()
            .Concat(
                ProjectRules.Cast<IDotRadarRule>())
            .Concat(
                FileRules.Cast<IDotRadarRule>())
            .ToArray();

        ValidateRuleIds(AllRules);
    }

    public IReadOnlyList<IDocumentRule> DocumentRules { get; }

    public IReadOnlyList<IProjectRule> ProjectRules { get; }

    public IReadOnlyList<IFileRule> FileRules { get; }

    public IReadOnlyList<IDotRadarRule> AllRules { get; }

    public RuleSet Filter(
        Func<IDotRadarRule, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return new RuleSet(
            documentRules: DocumentRules.Where(
                rule => predicate(rule)),

            projectRules: ProjectRules.Where(
                rule => predicate(rule)),

            fileRules: FileRules.Where(
                rule => predicate(rule)));
    }

    private static void ValidateRuleIds(
        IReadOnlyList<IDotRadarRule> rules)
    {
        var duplicateRuleId = rules
            .GroupBy(
                rule => rule.RuleId,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateRuleId is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate rule ID: {duplicateRuleId}");
        }
    }
}