using DotRadar.Analysis.Roslyn;
using DotRadar.Core;

namespace DotRadar.Tool;

internal static class ListRulesCommand
{
    public static int Execute(TextWriter output)
    {
        var rules = RuleRegistry.CreateDefault()
            .OrderBy(rule => rule.RuleId)
            .ToArray();

        output.WriteLine("DotRadar rules");
        output.WriteLine();

        output.WriteLine(
            $"{"ID",-10} {"Severity",-10} {"Category",-16} Title");

        output.WriteLine(
            $"{new string('-', 8),-10} " +
            $"{new string('-', 8),-10} " +
            $"{new string('-', 14),-16} " +
            $"{new string('-', 30)}");

        foreach (var rule in rules)
        {
            var descriptor = rule.Descriptor;

            output.WriteLine(
                $"{descriptor.RuleId,-10} " +
                $"{descriptor.DefaultSeverity,-10} " +
                $"{descriptor.Category,-16} " +
                $"{descriptor.Title}");
        }

        output.WriteLine();
        output.WriteLine($"Total: {rules.Length} rules");

        return ExitCodes.Success;
    }
}