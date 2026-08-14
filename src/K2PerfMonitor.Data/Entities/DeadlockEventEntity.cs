using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// Deadlock — จาก Extended Events system_health session (deadlock graph XML)
/// SourceKey = deadlock graph hash หรือ victim process id + timestamp
/// </summary>
public class DeadlockEventEntity : MetricEntityBase
{
    /// <summary>เวลาที่ deadlock เกิด (จาก XE event)</summary>
    public DateTime DeadlockAtUtc { get; set; }

    /// <summary>process id ของ victim (ที่ถูก kill)</summary>
    [MaxLength(128)]
    public string VictimProcessId { get; set; } = string.Empty;

    /// <summary>SQL ของ victim</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string VictimQueryText { get; set; } = string.Empty;

    /// <summary>login ของ victim</summary>
    [MaxLength(256)]
    public string? VictimLoginName { get; set; }

    /// <summary>SQL ของฝ่ายที่รอด</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string SurvivorQueryText { get; set; } = string.Empty;

    /// <summary>login ของฝ่ายที่รอด</summary>
    [MaxLength(256)]
    public string? SurvivorLoginName { get; set; }

    /// <summary>deadlock graph XML ต้นฉบับ (เก็บไว้แสดงใน viewer)</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string DeadlockGraphXml { get; set; } = string.Empty;
}
