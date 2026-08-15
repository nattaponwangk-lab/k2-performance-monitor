using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// สถานะ + ขนาด ต่อ database ใน target instance — จาก sys.databases + sys.master_files
/// (database discovery — ไม่ hard-code ชื่อ database)
/// SourceKey = database name
/// </summary>
public class DatabaseStatEntity : MetricEntityBase
{
    public int DatabaseId { get; set; }
    [MaxLength(128)] public string DatabaseName { get; set; } = string.Empty;

    /// <summary>ONLINE / OFFLINE / RESTORING / RECOVERING / RECOVERY_PENDING / SUSPECT / EMERGENCY</summary>
    [MaxLength(32)] public string State { get; set; } = string.Empty;

    /// <summary>SIMPLE / FULL / BULK_LOGGED</summary>
    [MaxLength(16)] public string? RecoveryModel { get; set; }

    public int CompatibilityLevel { get; set; }
    public bool IsSystemDatabase { get; set; }

    public double DataSizeMb { get; set; }
    public double LogSizeMb { get; set; }
    public double TotalSizeMb { get; set; }
}
