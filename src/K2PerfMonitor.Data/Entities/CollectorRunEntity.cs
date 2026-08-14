using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// ประวัติการรัน collector แต่ละรอบ — เก็บ success/elapsed/error
/// ใช้สำหรับ Dashboard > System status และ debug
/// </summary>
public class CollectorRunEntity
{
    [Key]
    public long Id { get; set; }

    /// <summary>collector type</summary>
    public CollectorType CollectorType { get; set; }

    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>เริ่มรอบนี้เมื่อไร (UTC)</summary>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>เสร็จเมื่อไร</summary>
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>เวลาที่ใช้ (ms)</summary>
    public double ElapsedMs { get; set; }

    public bool Success { get; set; }

    /// <summary>จำนวน items ที่เก็บได้</summary>
    public int ItemsCollected { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }
}
