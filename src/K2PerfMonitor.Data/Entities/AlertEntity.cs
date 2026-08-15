using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// EF entity สำหรับ Alerts table
/// (map กับ Core.Models.Alert แต่เป็น EF-tracked entity)
/// </summary>
public class AlertEntity
{
    [Key]
    public long Id { get; set; }

    public long? RuleId { get; set; }

    /// <summary>collector ต้นเหตุ (stored as int)</summary>
    public CollectorType CollectorType { get; set; }

    /// <summary>instance ต้นเหตุ (multi-instance)</summary>
    public long InstanceId { get; set; }

    [MaxLength(128)]
    public string InstanceName { get; set; } = "Default";

    /// <summary>key สำหรับ dedup/group (รวม instanceId แล้ว)</summary>
    [MaxLength(256)]
    public string DedupKey { get; set; } = string.Empty;

    public Severity Severity { get; set; } = Severity.Warning;

    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string Summary { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Detail { get; set; }

    public double? MetricValue { get; set; }
    public double? ThresholdValue { get; set; }

    public AlertStatus Status { get; set; } = AlertStatus.New;

    public DateTime RaisedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? LastNotifiedAtUtc { get; set; }

    public int NotifyCount { get; set; }
}
