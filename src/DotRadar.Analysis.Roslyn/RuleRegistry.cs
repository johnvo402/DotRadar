namespace DotRadar.Analysis.Roslyn;

public static class RuleRegistry
{
    public static IReadOnlyList<IDotRadarRule> CreateDefault()
    {
        return
        [
            new Dtr1101Rule()
        ];
    }
}