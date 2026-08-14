using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Models;

/// <summary>
/// ข้อความที่จะส่งผ่าน notification provider
/// แต่ละ provider ใช้ field ที่เหมาะสม (LINE = text, Teams = adaptive card, Email = html)
/// </summary>
public sealed class NotificationMessage
{
    public required string Title { get; init; }

    /// <summary>บทสรุปสั้น (1-2 บรรทัด)</summary>
    public required string Summary { get; init; }

    /// <summary>รายละเอียดเต็ม (อาจมีหลายบรรทัด / link)</summary>
    public string? Detail { get; init; }

    /// <summary>ระดับความรุนแรง — provider ใช้เลือกสี/icon</summary>
    public Severity Severity { get; init; } = Severity.Warning;

    /// <summary>collector ที่เป็นต้นเหตุ (เผื่อแสดง tag)</summary>
    public CollectorType? CollectorType { get; init; }

    /// <summary>ลิงก์ไปหน้า dashboard ที่เกี่ยวข้อง</summary>
    public string? DashboardUrl { get; init; }

    /// <summary>เวลาที่เกิดเหตุการณ์ (local time string)</summary>
    public string? Timestamp { get; init; }
}
