using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// แถว metric พื้นฐาน — เก็บค่า metric หนึ่งรายการที่ collector ดึงได้ในรอบหนึ่ง
/// ใช้ table-per-collector จริงๆ แต่ออกแบบ payload JSON เพื่อความยืดหยุ่น
/// (เก็บข้อมูลดิบเต็มรูปแบบใน PayloadJson)
/// </summary>
public abstract class MetricEntityBase
{
    [Key]
    public long Id { get; set; }

    /// <summary>เวลาที่เก็บข้อมูล (UTC) — สร้าง index</summary>
    public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>key สำหรับ grouping/dedup (query hash / wait type / sp name)</summary>
    [MaxLength(256)]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>ข้อมูลดิบเต็มรูปแบบ (JSON)</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string PayloadJson { get; set; } = "{}";
}
