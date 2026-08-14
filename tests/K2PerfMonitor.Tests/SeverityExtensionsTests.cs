using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Extensions;

namespace K2PerfMonitor.Tests;

public class SeverityExtensionsTests
{
    [Theory]
    [InlineData(Severity.Critical, "CRITICAL")]
    [InlineData(Severity.Warning, "WARNING")]
    [InlineData(Severity.Info, "INFO")]
    public void ToLabel_ReturnsExpected(Severity severity, string expected)
        => Assert.Equal(expected, severity.ToLabel());

    [Fact]
    public void ToHexColor_DistinctPerSeverity()
    {
        var critical = Severity.Critical.ToHexColor();
        var warning = Severity.Warning.ToHexColor();
        Assert.StartsWith("#", critical);
        Assert.NotEqual(critical, warning);
    }
}
