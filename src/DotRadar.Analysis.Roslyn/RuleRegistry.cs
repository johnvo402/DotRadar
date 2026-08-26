namespace DotRadar.Analysis.Roslyn;

public static class RuleRegistry
{
    public static IReadOnlyList<IDotRadarRule> CreateDefault()
    {
        return
        [
            new Dtr1101Rule(),
            new Dtr1102Rule(),
            new Dtr1103Rule(),
            new Dtr1201Rule()
        ];
    }
}