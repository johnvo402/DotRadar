using DotRadar.Abstractions;
using DotRadar.Core;

using Xunit;

namespace DotRadar.Tests;

public sealed class DotRadarSeverityPolicyTests
{
    [Theory]
    [InlineData(
        DotRadarSeverity.Info,
        DotRadarSeverity.Warning,
        false)]
    [InlineData(
        DotRadarSeverity.Warning,
        DotRadarSeverity.Warning,
        true)]
    [InlineData(
        DotRadarSeverity.Error,
        DotRadarSeverity.Warning,
        true)]
    [InlineData(
        DotRadarSeverity.Warning,
        DotRadarSeverity.Error,
        false)]
    [InlineData(
        DotRadarSeverity.Error,
        DotRadarSeverity.Error,
        true)]
    public void Evaluates_failure_threshold(
        DotRadarSeverity severity,
        DotRadarSeverity threshold,
        bool expected)
    {
        var result =
            DotRadarSeverityPolicy.MeetsThreshold(
                severity,
                threshold);

        Assert.Equal(expected, result);
    }
}