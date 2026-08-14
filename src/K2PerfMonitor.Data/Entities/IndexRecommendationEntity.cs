using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// คำแนะนำ index — รวม missing index (จาก sys.dm_db_missing_index_*) และ unused index (จาก sys.dm_db_index_usage_stats)
/// SourceKey = "{Type}:{DbSchemaObject}|{IndexName}"
/// </summary>
public class IndexRecommendationEntity : MetricEntityBase
{
    /// <summary>ประเภทคำแนะนำ: Missing / Unused</summary>
    [MaxLength(16)]
    public string RecommendationType { get; set; } = "Missing";

    [MaxLength(128)] public string? DatabaseName { get; set; }
    [MaxLength(128)] public string? SchemaName { get; set; }
    [MaxLength(256)] public string? TableName { get; set; }

    /// <summary>คอลัมน์ equality ที่แนะนำ (missing)</summary>
    [MaxLength(512)]
    public string? EqualityColumns { get; set; }

    /// <summary>คอลัมน์ inequality ที่แนะนำ</summary>
    [MaxLength(512)]
    public string? InequalityColumns { get; set; }

    /// <summary>คอลัมน์ include</summary>
    [MaxLength(512)]
    public string? IncludedColumns { get; set; }

    /// <summary>คะแนน impact (avg_user_impact ของ missing index หรือ 0-100)</summary>
    public double Impact { get; set; }

    /// <summary>user seeks/scans/lookups (missing) หรือจำนวนครั้งที่ใช้ (unused)</summary>
    public long UserSeeks { get; set; }
    public long UserScans { get; set; }
    public long UserLookups { get; set; }

    /// <summary>ชื่อ index ที่มีอยู่ (unused)</summary>
    [MaxLength(256)]
    public string? IndexName { get; set; }

    /// <summary>คำแนะนำเป็นข้อความ (CREATE INDEX ...)</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? RecommendationScript { get; set; }
}
