using DotRadar.Tool;

using Xunit;

namespace DotRadar.Tests;

public sealed class BaselineCommandOptionsTests
{
    [Fact]
    public void Parses_target_with_default_output()
    {
        var success = BaselineCommandOptions.TryParse(
            ["baseline", "."],
            out var options,
            out var error);

        Assert.True(success);
        Assert.Null(error);

        var parsed =
            Assert.IsType<BaselineCommandOptions>(options);

        Assert.Equal(".", parsed.Target);
        Assert.Null(parsed.OutputPath);
    }

    [Fact]
    public void Parses_output_and_config_paths()
    {
        var success = BaselineCommandOptions.TryParse(
            [
                "baseline",
                ".",
                "--output",
                "baseline.json",
                "--config",
                "strict.json"
            ],
            out var options,
            out var error);

        Assert.True(success);
        Assert.Null(error);

        var parsed =
            Assert.IsType<BaselineCommandOptions>(options);

        Assert.Equal(
            "baseline.json",
            parsed.OutputPath);

        Assert.Equal(
            "strict.json",
            parsed.ConfigPath);
    }
}