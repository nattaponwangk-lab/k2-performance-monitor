using K2PerfMonitor.Worker.Jobs;

namespace K2PerfMonitor.Tests;

public class CronExprTests
{
    [Theory]
    [InlineData(15, "*/15 * * * * *")]   // sub-minute → 6-field seconds cron
    [InlineData(30, "*/30 * * * * *")]
    [InlineData(10, "*/10 * * * * *")]
    public void FromSeconds_SubMinute_UsesSecondsCron(int seconds, string expected)
        => Assert.Equal(expected, CronExpr.FromSeconds(seconds));

    [Theory]
    [InlineData(60, "*/1 * * * *")]      // 1 minute
    [InlineData(120, "*/2 * * * *")]     // 2 minutes
    [InlineData(300, "*/5 * * * *")]     // 5 minutes
    public void FromSeconds_MinuteRange_UsesMinuteCron(int seconds, string expected)
        => Assert.Equal(expected, CronExpr.FromSeconds(seconds));

    [Fact]
    public void FromSeconds_NonDivisor_SnapsToNearestDivisorOf60()
    {
        // 25s ไม่หาร 60 ลงตัว → ต้อง snap ไปหา divisor ที่ใกล้สุด (20 หรือ 30) และหาร 60 ลงตัว
        var cron = CronExpr.FromSeconds(25);
        Assert.Matches(@"^\*/(20|30) \* \* \* \* \*$", cron);
    }

    [Fact]
    public void FromSeconds_ZeroOrNegative_ClampedToOne()
        => Assert.Equal("*/1 * * * * *", CronExpr.FromSeconds(0));
}
