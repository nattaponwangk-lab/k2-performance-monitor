using System.Collections.Generic;
using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Results;

/// <summary>
/// ผลลัพธ์ที่ collector คืนหลังเก็บข้อมูลหนึ่งรอบ
/// ประกอบด้วย metrics ดิบ + severity ที่ประเมินได้ (เผื่อ alert engine ใช้)
/// </summary>
public sealed class CollectorResult
{
    /// <summary>Collector type ที่ผลลัพธ์นี้อ้างถึง</summary>
    public required CollectorType CollectorType { get; init; }

    /// <summary>instance ที่เก็บข้อมูลนี้ (multi-instance isolation) — 0 = Default (SourceDb ที่ config)</summary>
    public long InstanceId { get; init; }

    /// <summary>ชื่อ instance (แสดงผล)</summary>
    public string InstanceName { get; init; } = "Default";

    /// <summary>เวลาที่เก็บข้อมูล (UTC)</summary>
    public DateTime CollectedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>สำเร็จหรือไม่ (false = error/empty)</summary>
    public bool Success { get; init; } = true;

    /// <summary>ข้อความ error ถ้าไม่สำเร็จ</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>เวลาที่ใช้เก็บข้อมูลรอบนี้</summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>
    /// metric items ที่เก็บได้ — เป็น dictionary ที่ key ตามชื่อฟิลด์
    /// แต่ละ item คือ object ที่ collector-specific (เช่น SlowQueryItem, WaitStatItem)
    /// </summary>
    public IReadOnlyList<MetricItem> Items { get; init; } = Array.Empty<MetricItem>();
}

/// <summary>
/// หน่วยข้อมูล metric หนึ่งรายการ
/// พิเศษ: เก็บค่าที่ใช้ประเมิน alert ใน <see cref="NumericValue"/> + key สำหรับ dedup
/// </summary>
public sealed class MetricItem
{
    /// <summary>key สำหรับ dedup/group (เช่น query hash, wait type, stored proc name)</summary>
    public required string Key { get; init; }

    /// <summary>ชื่อฟิลด์ที่จะใช้เทียบกับ alert rule (เช่น "DurationMs", "WaitTimeMs")</summary>
    public string? MetricField { get; init; }

    /// <summary>ค่าตัวเลขที่ใช้เทียบ threshold (nullable ถ้าไม่มี alert)</summary>
    public double? NumericValue { get; init; }

    /// <summary>ระดับความรุนแรงที่ประเมินเบื้องต้น</summary>
    public Severity Severity { get; init; } = Severity.Info;

    /// <summary>ข้อมูลดิบเต็ม (payload) — JSON-serializable</summary>
    public required IReadOnlyDictionary<string, object?> Payload { get; init; }

    /// <summary>ข้อความสรุปสำหรับแสดงใน alert/notification</summary>
    public string? Summary { get; init; }
}
