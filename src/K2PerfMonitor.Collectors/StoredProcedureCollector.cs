using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// StoredProcedure Collector — SP ที่ทำงานช้า จาก sys.dm_exec_procedure_stats
/// avg elapsed = total_elapsed_time / execution_count (µs → ms)
/// SourceKey = "{DatabaseName}|{Schema}.{Object}" (unique ต่อ SP)
/// </summary>
public sealed class StoredProcedureCollector : SqlCollectorBase
{
    public override CollectorType Type => CollectorType.StoredProcedure;
    public override string DisplayName => "Stored Procedures";

    public StoredProcedureCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<StoredProcedureCollector> logger)
        : base(conn, schedule, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        return await reader.QueryAsync("""
            SELECT TOP (@TopN)
                ps.database_id,
                DB_NAME(ps.database_id)                                  AS DatabaseName,
                OBJECT_SCHEMA_NAME(ps.object_id, ps.database_id)         AS SchemaName,
                OBJECT_NAME(ps.object_id, ps.database_id)                AS ObjectName,
                ps.object_id                                            AS ObjectId,
                ps.execution_count                                      AS ExecutionCount,
                ps.total_elapsed_time / 1000.0                          AS TotalElapsedMs,
                (ps.total_elapsed_time / ps.execution_count) / 1000.0   AS AvgElapsedMs,
                ps.max_elapsed_time / 1000.0                            AS MaxElapsedMs,
                ps.total_worker_time / 1000.0                           AS TotalWorkerMs,
                (ps.total_worker_time / ps.execution_count) / 1000.0    AS AvgWorkerMs,
                CAST(ps.total_logical_reads AS float)                   AS TotalLogicalReads,
                ps.total_logical_reads * 1.0 / ps.execution_count       AS AvgLogicalReads,
                CAST(ps.total_physical_reads AS float)                  AS TotalPhysicalReads,
                ps.total_physical_reads * 1.0 / ps.execution_count      AS AvgPhysicalReads,
                ps.last_execution_time                                  AS LastExecutionUtc
            FROM sys.dm_exec_procedure_stats ps
            WHERE ps.execution_count > 0
            ORDER BY AvgElapsedMs DESC;
            """,
            r =>
            {
                var db = r.GetStrOrNull("DatabaseName");
                var schema = r.GetStrOrNull("SchemaName");
                var obj = r.GetStrOrNull("ObjectName");
                var avg = r.GetDouble("AvgElapsedMs");
                var key = $"{db}|{schema}.{obj}";
                return new MetricItem
                {
                    Key = key,
                    MetricField = MetricFields.AvgDurationMs,
                    NumericValue = avg,
                    Severity = avg > 15000 ? Severity.Critical : avg > 5000 ? Severity.Warning : Severity.Info,
                    Summary = $"{schema}.{obj} — avg {avg:0} ms ×{r.GetLong("ExecutionCount")}",
                    Payload = new Dictionary<string, object?>
                    {
                        ["DatabaseName"] = db,
                        ["SchemaName"] = schema,
                        ["ObjectName"] = obj,
                        ["ObjectId"] = r.GetLong("ObjectId"),
                        ["ExecutionCount"] = r.GetLong("ExecutionCount"),
                        ["TotalElapsedMs"] = r.GetDouble("TotalElapsedMs"),
                        ["AvgElapsedMs"] = avg,
                        ["MaxElapsedMs"] = r.GetDouble("MaxElapsedMs"),
                        ["TotalWorkerMs"] = r.GetDouble("TotalWorkerMs"),
                        ["AvgWorkerMs"] = r.GetDouble("AvgWorkerMs"),
                        ["TotalLogicalReads"] = r.GetDouble("TotalLogicalReads"),
                        ["AvgLogicalReads"] = r.GetDouble("AvgLogicalReads"),
                        ["TotalPhysicalReads"] = r.GetDouble("TotalPhysicalReads"),
                        ["AvgPhysicalReads"] = r.GetDouble("AvgPhysicalReads"),
                        ["LastExecutionUtc"] = r.GetDateTimeOrNull("LastExecutionUtc"),
                        ["SourceKey"] = key
                    }
                };
            },
            ct,
            ("@TopN", Schedule.TopN));
    }
}
