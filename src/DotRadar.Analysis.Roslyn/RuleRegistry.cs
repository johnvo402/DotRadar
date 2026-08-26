namespace DotRadar.Analysis.Roslyn;

public static class RuleRegistry
{
    public static RuleSet CreateDefault()
    {
        return new RuleSet(
            documentRules:
            [
                new Dtr1001Rule(),
                new Dtr1101Rule(),
                new Dtr1102Rule(),
                new Dtr1103Rule()
            ]);
    }
}