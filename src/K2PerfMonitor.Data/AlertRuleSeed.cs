using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Data;

/// <summary>
/// Default alert rules ที่ seed ตอนสร้าง DB
/// (เลียนแบบเกณฑ์ทั่วไป — ปรับได้ในหน้า Settings หรือ init script)
/// </summary>
public static class AlertRuleSeed
{
    public static void Apply(ModelBuilder mb)
    {
        var rules = new[]
        {
            // Slow queries
            Rule(1, "Slow Query (avg > 5s)", CollectorType.SlowQuery, MetricFields.AvgDurationMs,
                 ComparisonOperator.GreaterThan, 5000, Severity.Warning, 30),
            Rule(2, "Slow Query (avg > 15s)", CollectorType.SlowQuery, MetricFields.AvgDurationMs,
                 ComparisonOperator.GreaterThan, 15000, Severity.Critical, 30),

            // Stored procedure
            Rule(3, "Slow Stored Proc (avg > 5s)", CollectorType.StoredProcedure, MetricFields.AvgDurationMs,
                 ComparisonOperator.GreaterThan, 5000, Severity.Warning, 30),

            // Wait stats
            Rule(4, "High Wait Time", CollectorType.WaitStatistics, MetricFields.WaitTimeMs,
                 ComparisonOperator.GreaterThan, 30000, Severity.Warning, 60),

            // Blocking
            Rule(5, "Long Blocking (> 30s)", CollectorType.Blocking, MetricFields.BlockingDurationMs,
                 ComparisonOperator.GreaterThan, 30000, Severity.Warning, 15),
            Rule(6, "Severe Blocking (> 120s)", CollectorType.Blocking, MetricFields.BlockingDurationMs,
                 ComparisonOperator.GreaterThan, 120000, Severity.Critical, 15),

            // Server stats — CPU/RAM
            Rule(7, "High CPU (> 80%)", CollectorType.ServerStats, MetricFields.CpuPercent,
                 ComparisonOperator.GreaterThan, 80, Severity.Warning, 5),
            Rule(8, "Critical CPU (> 95%)", CollectorType.ServerStats, MetricFields.CpuPercent,
                 ComparisonOperator.GreaterThan, 95, Severity.Critical, 5),
            Rule(9, "Low Memory (< 512MB free)", CollectorType.ServerStats, MetricFields.AvailableMemoryMb,
                 ComparisonOperator.LessThan, 512, Severity.Warning, 5),
            Rule(10, "Critical Memory (< 128MB free)", CollectorType.ServerStats, MetricFields.AvailableMemoryMb,
                 ComparisonOperator.LessThan, 128, Severity.Critical, 5),

            // I/O
            Rule(11, "Slow I/O Read (> 20ms/op)", CollectorType.Io, MetricFields.IoStallMsPerRead,
                 ComparisonOperator.GreaterThan, 20, Severity.Warning, 60),

            // Index
            Rule(12, "Missing Index (high impact)", CollectorType.Index, MetricFields.MissingIndexImpact,
                 ComparisonOperator.GreaterThan, 80, Severity.Info, 360),

            // K2 workflow
            Rule(13, "Stuck Workflow (> 24h)", CollectorType.K2Workflow, MetricFields.WorkflowDurationMs,
                 ComparisonOperator.GreaterThan, 24 * 3600 * 1000.0, Severity.Warning, 60),

            // K2 SmartForm
            Rule(14, "Slow Form Load (> 8s)", CollectorType.K2SmartForm, MetricFields.FormLoadMs,
                 ComparisonOperator.GreaterThan, 8000, Severity.Warning, 30),

            // K2 SmartObject
            Rule(15, "Slow SmartObject Call (> 5s)", CollectorType.K2SmartObject, MetricFields.SmartObjectCallMs,
                 ComparisonOperator.GreaterThan, 5000, Severity.Warning, 30),
        };

        mb.Entity<AlertRuleEntity>().HasData(rules);
    }

    private static AlertRuleEntity Rule(
        long id, string name, CollectorType collector, string field,
        ComparisonOperator op, double threshold, Severity severity, int cooldownMin,
        NotificationChannel channels = NotificationChannel.All)
        => new()
        {
            Id = id,
            Name = name,
            Enabled = true,
            CollectorType = collector,
            MetricField = field,
            Operator = op,
            Threshold = threshold,
            Severity = severity,
            CooldownMinutes = cooldownMin,
            Channels = channels,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
}
