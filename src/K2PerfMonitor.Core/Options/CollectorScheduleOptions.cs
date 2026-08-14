using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Options;

/// <summary>
/// การตั้งค่ารอบการเก็บข้อมูลของแต่ละ collector (วินาที)
/// ปรับได้ใน appsettings.json
/// </summary>
public sealed class CollectorScheduleOptions
{
    public const string SectionName = "CollectorSchedule";

    /// <summary>Server stats (CPU/RAM/connections) — ดึงบ่อยที่สุดเพื่อ real-time</summary>
    public int ServerStatsIntervalSeconds { get; set; } = 15;

    /// <summary>Slow query / Wait stats / Blocking — ดึกระดับกลาง</summary>
    public int SlowQueryIntervalSeconds { get; set; } = 60;
    public int WaitStatsIntervalSeconds { get; set; } = 60;
    public int BlockingIntervalSeconds { get; set; } = 30;

    /// <summary>Deadlock จาก Extended Events</summary>
    public int DeadlockIntervalSeconds { get; set; } = 120;

    /// <summary>Index / I/O — ดึกน้อยกว่า (ข้อมูลค่อนข้างคงที่)</summary>
    public int IndexIntervalSeconds { get; set; } = 300;
    public int IoIntervalSeconds { get; set; } = 120;

    /// <summary>Stored procedure stats</summary>
    public int StoredProcedureIntervalSeconds { get; set; } = 120;

    /// <summary>K2 collectors</summary>
    public int K2WorkflowIntervalSeconds { get; set; } = 60;
    public int K2SmartFormIntervalSeconds { get; set; } = 120;
    public int K2SmartObjectIntervalSeconds { get; set; } = 120;

    /// <summary>Top N สำหรับ queries ที่เรียงลำดับ (เช่น top 20 slow queries)</summary>
    public int TopN { get; set; } = 20;

    /// <summary>Threshold (ms) ที่ถือว่า query "ช้า" สำหรับ SlowQueryCollector</summary>
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>ระยะเวลาล้างข้อมูลเก่า (วัน) — retention policy</summary>
    public int RetentionDays { get; set; } = 30;
}
