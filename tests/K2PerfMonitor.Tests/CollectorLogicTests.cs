using K2PerfMonitor.Collectors;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;

namespace K2PerfMonitor.Tests;

/// <summary>
/// Unit tests สำหรับ pure logic ของ collector framework (Phase 1)
/// - delta/baseline handling ของ DMV สะสม
/// - registry schedule mapping
/// - helper functions
/// </summary>
public class DeltaMathTests
{
    [Theory]
    [InlineData(10, 15, 5)]     // ปกติ: current > previous
    [InlineData(0, 100, 100)]   // จาก 0
    [InlineData(100, 100, 0)]   // ไม่เปลี่ยน
    public void Diff_long_returns_delta(long prev, long cur, long expected)
        => Assert.Equal(expected, DeltaMath.Diff(prev, cur));

    [Fact]
    public void Diff_long_handles_counter_reset()
    {
        // server restart → current < previous → ถือ current เป็น delta
        Assert.Equal(30, DeltaMath.Diff(1000, 30));
    }

    [Fact]
    public void Diff_double_handles_reset()
    {
        Assert.Equal(5.5, DeltaMath.Diff(10.0, 15.5));
        Assert.Equal(3.0, DeltaMath.Diff(100.0, 3.0)); // reset
    }
}

public class DeltaBaselineTests
{
    [Fact]
    public void First_update_returns_no_deltas_and_sets_baseline()
    {
        var b = new DeltaBaseline<long>();
        Assert.False(b.HasBaseline);

        var d1 = b.Update(new Dictionary<string, long> { ["a"] = 100 }, DeltaMath.Diff);

        Assert.Empty(d1);          // รอบแรก = baseline เท่านั้น
        Assert.True(b.HasBaseline);
    }

    [Fact]
    public void Second_update_returns_delta_vs_previous()
    {
        var b = new DeltaBaseline<long>();
        b.Update(new Dictionary<string, long> { ["a"] = 100 }, DeltaMath.Diff);

        var d2 = b.Update(new Dictionary<string, long> { ["a"] = 175 }, DeltaMath.Diff);

        Assert.Equal(75, d2["a"]);
    }

    [Fact]
    public void New_key_appears_only_after_it_has_a_baseline()
    {
        var b = new DeltaBaseline<long>();
        b.Update(new Dictionary<string, long> { ["a"] = 10 }, DeltaMath.Diff);

        // key "b" ปรากฏครั้งแรก → ยังไม่มี delta
        var d2 = b.Update(new Dictionary<string, long> { ["a"] = 20, ["b"] = 5 }, DeltaMath.Diff);
        Assert.True(d2.ContainsKey("a"));
        Assert.False(d2.ContainsKey("b"));

        // รอบถัดไป key "b" มี baseline แล้ว
        var d3 = b.Update(new Dictionary<string, long> { ["a"] = 25, ["b"] = 12 }, DeltaMath.Diff);
        Assert.Equal(7, d3["b"]);
    }
}

public class CollectorRegistryMappingTests
{
    [Fact]
    public void IntervalFor_maps_each_type_to_configured_seconds()
    {
        var s = new CollectorScheduleOptions
        {
            ServerStatsIntervalSeconds = 15,
            BlockingIntervalSeconds = 30,
            DeadlockIntervalSeconds = 120,
            IndexIntervalSeconds = 300
        };
        Assert.Equal(15, CollectorRegistry.IntervalFor(CollectorType.ServerStats, s));
        Assert.Equal(30, CollectorRegistry.IntervalFor(CollectorType.Blocking, s));
        Assert.Equal(120, CollectorRegistry.IntervalFor(CollectorType.Deadlock, s));
        Assert.Equal(300, CollectorRegistry.IntervalFor(CollectorType.Index, s));
    }

    [Fact]
    public void ExecutionPlan_shares_slowquery_interval()
    {
        var s = new CollectorScheduleOptions { SlowQueryIntervalSeconds = 45 };
        Assert.Equal(45, CollectorRegistry.IntervalFor(CollectorType.ExecutionPlan, s));
    }
}

public class AlertHysteresisTests
{
    [Theory]
    [InlineData(K2PerfMonitor.Core.Enums.ComparisonOperator.GreaterThan, 80, 0.1, 72)]   // 80*(1-0.1)
    [InlineData(K2PerfMonitor.Core.Enums.ComparisonOperator.LessThan, 500, 0.1, 550)]    // 500*(1+0.1)
    [InlineData(K2PerfMonitor.Core.Enums.ComparisonOperator.Equals, 100, 0.1, 100)]      // no band
    public void HoldThreshold_widens_toward_ok_side(K2PerfMonitor.Core.Enums.ComparisonOperator op, double threshold, double frac, double expected)
        => Assert.Equal(expected, K2PerfMonitor.Alerts.AlertEvaluator.HoldThreshold(threshold, op, frac), 3);

    [Fact]
    public void Active_alert_stays_firing_within_hold_band()
    {
        // rule: CPU > 80. ค่า 75 ปกติจะ clear แต่ alert active + hysteresis 10% (hold=72) → คง firing
        var rule = new K2PerfMonitor.Data.Entities.AlertRuleEntity
        {
            Id = 1, Enabled = true, CollectorType = K2PerfMonitor.Core.Enums.CollectorType.ServerStats,
            MetricField = K2PerfMonitor.Core.Constants.MetricFields.CpuPercent,
            Operator = K2PerfMonitor.Core.Enums.ComparisonOperator.GreaterThan, Threshold = 80,
            Severity = K2PerfMonitor.Core.Enums.Severity.Warning
        };
        var result = new K2PerfMonitor.Core.Results.CollectorResult
        {
            CollectorType = K2PerfMonitor.Core.Enums.CollectorType.ServerStats,
            Items = new[] { new K2PerfMonitor.Core.Results.MetricItem
            {
                Key = "srv", MetricField = K2PerfMonitor.Core.Constants.MetricFields.CpuPercent,
                NumericValue = 75, Payload = new Dictionary<string, object?>()
            }}
        };
        var dedup = "ServerStats:CpuPercent:srv";

        // ไม่มี active alert → 75 < 80 → ไม่ fire
        Assert.Empty(K2PerfMonitor.Alerts.AlertEvaluator.Match(result, new[] { rule }));

        // มี active alert + hysteresis → คง firing (75 > 72)
        var held = K2PerfMonitor.Alerts.AlertEvaluator.Match(result, new[] { rule }, new HashSet<string> { dedup }, 0.10);
        Assert.Single(held);

        // ค่าหลุดแบนด์ (70 < 72) → clear แม้ active
        var below = new K2PerfMonitor.Core.Results.CollectorResult
        {
            CollectorType = K2PerfMonitor.Core.Enums.CollectorType.ServerStats,
            Items = new[] { new K2PerfMonitor.Core.Results.MetricItem
            {
                Key = "srv", MetricField = K2PerfMonitor.Core.Constants.MetricFields.CpuPercent,
                NumericValue = 70, Payload = new Dictionary<string, object?>()
            }}
        };
        Assert.Empty(K2PerfMonitor.Alerts.AlertEvaluator.Match(below, new[] { rule }, new HashSet<string> { dedup }, 0.10));
    }
}

public class CollectorHelperTests
{
    [Fact]
    public void Trim_returns_placeholder_for_empty()
        => Assert.Equal("(no text)", SlowQueryCollector.Trim("   ", 50));

    [Fact]
    public void Trim_truncates_and_flattens_newlines()
    {
        var result = SlowQueryCollector.Trim("SELECT *\r\nFROM BigTable", 10);
        Assert.DoesNotContain('\n', result);
        Assert.EndsWith("…", result);
        Assert.True(result.Length <= 11);
    }

    [Fact]
    public void BuildCreateIndexScript_includes_key_and_include_columns()
    {
        var script = IndexCollector.BuildCreateIndexScript("Db", "dbo", "Orders", "[CustomerId]", "[OrderDate]", "[Total]");
        Assert.Contains("CREATE NONCLUSTERED INDEX", script);
        Assert.Contains("[dbo].[Orders]", script);
        Assert.Contains("[CustomerId]", script);
        Assert.Contains("INCLUDE ([Total])", script);
    }
}
