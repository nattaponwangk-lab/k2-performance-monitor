using K2PerfMonitor.Alerts;
using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Results;
using K2PerfMonitor.Data.Entities;

namespace K2PerfMonitor.Tests;

public class AlertEvaluatorTests
{
    private static readonly AlertRuleEntity CpuWarn = new()
    {
        Id = 7, Name = "High CPU (> 80%)", Enabled = true,
        CollectorType = CollectorType.ServerStats, MetricField = MetricFields.CpuPercent,
        Operator = ComparisonOperator.GreaterThan, Threshold = 80, Severity = Severity.Warning
    };

    private static readonly AlertRuleEntity CpuCrit = new()
    {
        Id = 8, Name = "Critical CPU (> 95%)", Enabled = true,
        CollectorType = CollectorType.ServerStats, MetricField = MetricFields.CpuPercent,
        Operator = ComparisonOperator.GreaterThan, Threshold = 95, Severity = Severity.Critical
    };

    private static readonly AlertRuleEntity MemLow = new()
    {
        Id = 9, Name = "Low Memory (< 512MB free)", Enabled = true,
        CollectorType = CollectorType.ServerStats, MetricField = MetricFields.AvailableMemoryMb,
        Operator = ComparisonOperator.LessThan, Threshold = 512, Severity = Severity.Warning
    };

    private static CollectorResult ServerResult(params (string field, double value)[] metrics)
        => new()
        {
            CollectorType = CollectorType.ServerStats,
            Success = true,
            Items = metrics.Select(m => new MetricItem
            {
                Key = "SQL01",
                MetricField = m.field,
                NumericValue = m.value,
                Payload = new Dictionary<string, object?>()
            }).ToList()
        };

    [Fact]
    public void Match_NoBreach_ReturnsEmpty()
    {
        var result = ServerResult((MetricFields.CpuPercent, 40), (MetricFields.AvailableMemoryMb, 4096));
        var alerts = AlertEvaluator.Match(result, new[] { CpuWarn, CpuCrit, MemLow });
        Assert.Empty(alerts);
    }

    [Fact]
    public void Match_WarnThresholdOnly_RaisesWarning()
    {
        var result = ServerResult((MetricFields.CpuPercent, 85));
        var alerts = AlertEvaluator.Match(result, new[] { CpuWarn, CpuCrit });
        var alert = Assert.Single(alerts);
        Assert.Equal(Severity.Warning, alert.Severity);
        Assert.Equal(85, alert.MetricValue);
    }

    [Fact]
    public void Match_BothThresholds_KeepsHighestSeverityOnly()
    {
        // CPU 96% เข้าทั้ง >80 และ >95 → ต้องเหลือ Critical ตัวเดียว (dedup + escalate)
        var result = ServerResult((MetricFields.CpuPercent, 96));
        var alerts = AlertEvaluator.Match(result, new[] { CpuWarn, CpuCrit });
        var alert = Assert.Single(alerts);
        Assert.Equal(Severity.Critical, alert.Severity);
        Assert.Equal(8, alert.RuleId);
    }

    [Fact]
    public void Match_MultipleMetrics_RaisesPerDistinctMetric()
    {
        var result = ServerResult((MetricFields.CpuPercent, 90), (MetricFields.AvailableMemoryMb, 200));
        var alerts = AlertEvaluator.Match(result, new[] { CpuWarn, CpuCrit, MemLow });
        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a.DedupKey.Contains(MetricFields.CpuPercent));
        Assert.Contains(alerts, a => a.DedupKey.Contains(MetricFields.AvailableMemoryMb));
    }

    [Fact]
    public void Match_DisabledRule_Ignored()
    {
        var disabled = new AlertRuleEntity
        {
            Id = 99, Enabled = false, CollectorType = CollectorType.ServerStats,
            MetricField = MetricFields.CpuPercent, Operator = ComparisonOperator.GreaterThan,
            Threshold = 10, Severity = Severity.Critical
        };
        var result = ServerResult((MetricFields.CpuPercent, 85));
        var alerts = AlertEvaluator.Match(result, new[] { disabled });
        Assert.Empty(alerts);
    }

    [Fact]
    public void Match_DedupKey_IsStableAcrossRuns()
    {
        var r1 = ServerResult((MetricFields.CpuPercent, 90));
        var r2 = ServerResult((MetricFields.CpuPercent, 92));
        var k1 = AlertEvaluator.Match(r1, new[] { CpuWarn }).Single().DedupKey;
        var k2 = AlertEvaluator.Match(r2, new[] { CpuWarn }).Single().DedupKey;
        Assert.Equal(k1, k2); // key เดียวกัน → dedup ข้ามรอบได้
    }
}
