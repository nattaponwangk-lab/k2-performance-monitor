using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// Execution plan ของ top slow queries — จาก sys.dm_exec_query_plan(plan_handle)
/// SourceKey = query hash hex (จับคู่กับ SlowQueryEntity ได้)
/// </summary>
public class ExecutionPlanEntity : MetricEntityBase
{
    /// <summary>query hash hex</summary>
    [MaxLength(64)]
    public string QueryHash { get; set; } = string.Empty;

    /// <summary>plan handle hex</summary>
    [MaxLength(256)]
    public string? PlanHandle { get; set; }

    [MaxLength(128)] public string? DatabaseName { get; set; }
    [MaxLength(256)] public string? ObjectName { get; set; }

    public long ExecutionCount { get; set; }
    public double AvgDurationMs { get; set; }
    public double AvgCpuMs { get; set; }
    public double AvgLogicalReads { get; set; }

    /// <summary>เป็น query plan XML (showplan) — เก็บไว้แสดงใน viewer</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string PlanXml { get; set; } = string.Empty;

    /// <summary>ตัด SQL text ไว้แสดงคู่กับ plan</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? QueryText { get; set; }
}
