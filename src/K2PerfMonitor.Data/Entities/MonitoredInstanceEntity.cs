using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using K2PerfMonitor.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// SQL/K2 instance ที่จะ monitor (multi-instance) — connection string เก็บ **เข้ารหัส** (Data Protection)
/// ห้ามเก็บ/แสดง/ล็อก connection string แบบ plaintext
/// </summary>
[Index(nameof(Name), IsUnique = true)]
public class MonitoredInstanceEntity
{
    [Key] public long Id { get; set; }

    [MaxLength(128)] public string Name { get; set; } = string.Empty;

    public InstanceType InstanceType { get; set; } = InstanceType.Sql;

    /// <summary>host/instance (แสดงผลได้ — ไม่ใช่ความลับ)</summary>
    [MaxLength(256)] public string? Host { get; set; }

    /// <summary>connection string ที่เข้ารหัสด้วย IDataProtector (protected payload)</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string EncryptedConnectionString { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
