using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// EF entity สำหรับ AlertRules table
/// (seeded ด้วย default rules ใน migration / init script)
/// </summary>
public class AlertRuleEntity
{
    [Key]
    public long Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public CollectorType CollectorType { get; set; }

    [MaxLength(64)]
    public string MetricField { get; set; } = string.Empty;

    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThan;

    public double Threshold { get; set; }

    public Severity Severity { get; set; } = Severity.Warning;

    public int CooldownMinutes { get; set; } = 30;

    public NotificationChannel Channels { get; set; } = NotificationChannel.All;

    [MaxLength(256)]
    public string? TitleTemplate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
