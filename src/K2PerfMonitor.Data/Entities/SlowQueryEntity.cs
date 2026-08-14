using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// Query ที่ช้า — จาก sys.dm_exec_query_stats
/// SourceKey = query hash hex
/// </summary>
public class SlowQueryEntity : MetricEntityBase
{
    /// <summary>SQL text (สูงสุด ~8KB)</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string QueryText { get; set; } = string.Empty;

    /// <summary>ชื่อ database</summary>
    [MaxLength(128)]
    public string? DatabaseName { get; set; }

    /// <summary>ชื่อ object ถ้ามี</summary>
    [MaxLength(256)]
    public string? ObjectName { get; set; }

    public long ExecutionCount { get; set; }
    public double TotalDurationMs { get; set; }
    public double AvgDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double TotalLogicalReads { get; set; }
    public double AvgLogicalReads { get; set; }
    public double AvgCpuMs { get; set; }
    public double AvgPhysicalReads { get; set; }
    public DateTime? LastExecutionUtc { get; set; }

    /// <summary>plan handle hex (สำหรับดึง execution plan)</summary>
    [MaxLength(256)]
    public string? PlanHandle { get; set; }
}
