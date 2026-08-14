using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Models;

/// <summary>
/// กฎการแจ้งเตือน: เทียบค่า metric กับ threshold
/// ตัวอย่าง: "หาก avg_duration_ms ของ SlowQuery > 5000 → Warning"
/// </summary>
public sealed class AlertRule
{
    public long Id { get; set; }

    /// <summary>ชื่อ rule</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>เปิดใช้งานหรือไม่</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>collector ที่ rule นี้ใช้กับ</summary>
    public CollectorType CollectorType { get; set; }

    /// <summary>ชื่อฟิลด์ที่จะเทียบ (เช่น "DurationMs", "WaitTimeMs", "CpuPercent")</summary>
    public string MetricField { get; set; } = string.Empty;

    /// <summary>ตัวดำเนินการเปรียบเทียบ</summary>
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThan;

    /// <summary>ค่า threshold</summary>
    public double Threshold { get; set; }

    /// <summary>ระดับความรุนแรงเมื่อ trigger</summary>
    public Severity Severity { get; set; } = Severity.Warning;

    /// <summary>cooldown นาที ก่อนจะแจ้งซ้ำ rule/Key เดิม</summary>
    public int CooldownMinutes { get; set; } = 30;

    /// <summary>ช่องทางที่จะแจ้ง (flags: Line/Teams/Email)</summary>
    public NotificationChannel Channels { get; set; } = NotificationChannel.All;

    /// <summary>ข้อความ title template ถ้าว่างจะใช้ default</summary>
    public string? TitleTemplate { get; set; }

    /// <summary>template เวลาสร้าง (UTC) — สำหรับ audit</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
