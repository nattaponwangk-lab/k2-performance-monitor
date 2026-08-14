using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// K2 Workflow stat — จาก [K2].[Server].[ProcInst] + [Server].[ActivityInst]
/// SourceKey = "{ProcSetId}|{ProcInstId}" หรือกลุ่ม workflow
/// </summary>
public class K2WorkflowStatEntity : MetricEntityBase
{
    /// <summary>workflow / process set id</summary>
    public long ProcSetId { get; set; }

    /// <summary>process instance id (ถ้าเก็บระดับ instance)</summary>
    public long? ProcInstId { get; set; }

    /// <summary>ชื่อ workflow</summary>
    [MaxLength(256)]
    public string? WorkflowName { get; set; }

    /// <summary>folio</summary>
    [MaxLength(256)]
    public string? Folio { get; set; }

    /// <summary>สถานะ process instance: Running/Completed/Error/Stuck</summary>
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    /// <summary>เวลาที่ workflow ทำงาน (ms) — start → end หรือ start → now (ถ้ายังรัน)</summary>
    public double DurationMs { get; set; }

    /// <summary>เวลาที่ activity ปัจจุบันค้าง (ms)</summary>
    public double? CurrentActivityWaitMs { get; set; }

    /// <summary>started at (UTC)</summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>finished at (UTC)</summary>
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>originator (ผู้เริ่ม workflow)</summary>
    [MaxLength(256)]
    public string? Originator { get; set; }

    /// <summary>flag ว่าเป็น workflow ที่ค้าง/stuck (เกิน threshold)</summary>
    public bool IsStuck { get; set; }
}
