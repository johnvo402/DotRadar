namespace DotRadar.Analysis.Roslyn;

public static class RuleRegistry
{
    public static IReadOnlyList<IDocumentRule> CreateDefault()
    {
        return
        [
            new Dtr1101Rule(),
            new Dtr1102Rule(),
            new Dtr1103Rule(),
            new Dtr1001Rule()
        ];
    }
}