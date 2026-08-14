using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// Stored procedure ที่ทำงานช้า — จาก sys.dm_exec_procedure_stats
/// SourceKey = "{DatabaseName}|{SchemaName}.{ObjectName}" (object id)
/// </summary>
public class StoredProcedureStatEntity : MetricEntityBase
{
    [MaxLength(128)] public string? DatabaseName { get; set; }
    [MaxLength(128)] public string? SchemaName { get; set; }
    [MaxLength(256)] public string? ObjectName { get; set; }

    public long ObjectId { get; set; }
    public long ExecutionCount { get; set; }

    public double TotalElapsedMs { get; set; }
    public double AvgElapsedMs { get; set; }
    public double MaxElapsedMs { get; set; }

    public double TotalWorkerMs { get; set; }
    public double AvgWorkerMs { get; set; }

    public double TotalLogicalReads { get; set; }
    public double AvgLogicalReads { get; set; }

    public double TotalPhysicalReads { get; set; }
    public double AvgPhysicalReads { get; set; }

    public DateTime? LastExecutionUtc { get; set; }
}
