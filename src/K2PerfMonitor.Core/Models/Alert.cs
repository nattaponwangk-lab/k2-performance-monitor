using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Models;

/// <summary>
/// Alert ที่ประเมินได้หลังเทียบ metric กับ rule
/// จะถูก persist ลง Alerts table และส่งผ่าน notification providers
/// </summary>
public sealed class Alert
{
    public long Id { get; set; }

    /// <summary>AlertRule ที่ trigger (FK)</summary>
    public long? RuleId { get; set; }

    /// <summary>collector ต้นเหตุ</summary>
    public CollectorType CollectorType { get; set; }

    /// <summary>instance ต้นเหตุ (multi-instance)</summary>
    public long InstanceId { get; set; }

    /// <summary>ชื่อ instance</summary>
    public string InstanceName { get; set; } = "Default";

    /// <summary>key สำหรับ dedup/group (รวม instanceId + collector + field + itemKey)</summary>
    public string DedupKey { get; set; } = string.Empty;

    /// <summary>ระดับความรุนแรง</summary>
    public Severity Severity { get; set; } = Severity.Warning;

    /// <summary>หัวข้อ</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>ข้อความสรุป</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>รายละเอียด/บริบทเพิ่มเติม</summary>
    public string? Detail { get; set; }

    /// <summary>ค่าที่วัดได้</summary>
    public double? MetricValue { get; set; }

    /// <summary>ค่า threshold ที่ใช้เทียบ</summary>
    public double? ThresholdValue { get; set; }

    /// <summary>สถานะ lifecycle</summary>
    public AlertStatus Status { get; set; } = AlertStatus.New;

    /// <summary>เวลาที่เกิด (UTC)</summary>
    public DateTime RaisedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>เวลาที่ acknowledged</summary>
    public DateTime? AcknowledgedAtUtc { get; set; }

    /// <summary>เวลาที่ resolved</summary>
    public DateTime? ResolvedAtUtc { get; set; }

    /// <summary>เวลาที่ส่ง notification ครั้งล่าสุด (เพื่อ cooldown)</summary>
    public DateTime? LastNotifiedAtUtc { get; set; }

    /// <summary>จำนวนครั้งที่ส่ง notification</summary>
    public int NotifyCount { get; set; }
}
