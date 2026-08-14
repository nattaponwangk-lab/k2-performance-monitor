using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Extensions;

namespace K2PerfMonitor.Tests;

public class ComparisonOperatorExtensionsTests
{
    [Theory]
    [InlineData(ComparisonOperator.GreaterThan, 81, 80, true)]
    [InlineData(ComparisonOperator.GreaterThan, 80, 80, false)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 80, 80, true)]
    [InlineData(ComparisonOperator.LessThan, 100, 128, true)]
    [InlineData(ComparisonOperator.LessThan, 128, 128, false)]
    [InlineData(ComparisonOperator.LessThanOrEqual, 128, 128, true)]
    [InlineData(ComparisonOperator.Equals, 50, 50, true)]
    [InlineData(ComparisonOperator.Equals, 50, 51, false)]
    public void Matches_EvaluatesThresholdCorrectly(
        ComparisonOperator op, double value, double threshold, bool expected)
        => Assert.Equal(expected, op.Matches(value, threshold));

    [Theory]
    [InlineData(ComparisonOperator.GreaterThan, ">")]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, ">=")]
    [InlineData(ComparisonOperator.LessThan, "<")]
    [InlineData(ComparisonOperator.LessThanOrEqual, "<=")]
    [InlineData(ComparisonOperator.Equals, "=")]
    public void ToSymbol_ReturnsExpected(ComparisonOperator op, string expected)
        => Assert.Equal(expected, op.ToSymbol());
}
