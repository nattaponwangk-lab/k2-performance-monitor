using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// SlowQuery Collector — top-N query ที่ช้าที่สุด จาก sys.dm_exec_query_stats
///
/// - avg duration = total_elapsed_time / execution_count (µs → ms) → เป็นค่าเฉลี่ยสะสมของ plan
///   (ไม่ต้องทำ delta เพราะเป็นค่าเฉลี่ย ไม่ใช่ counter ที่โตเรื่อย ๆ)
/// - กรองด้วย threshold (@ThresholdMs) + จำกัด TopN → เบาต่อ source (ROADMAP §6)
/// - ใช้ parameter ทุกจุด (TopN/threshold) — ปลอด SQL injection
/// - SourceKey = query_hash hex → dedup/alert per query pattern
/// </summary>
public sealed class SlowQueryCollector : SqlCollectorBase
{
    public override CollectorType Type => CollectorType.SlowQuery;
    public override string DisplayName => "Slow Queries";

    public SlowQueryCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<SlowQueryCollector> logger)
        : base(conn, schedule, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        var rows = await reader.QueryAsync("""
            SELECT TOP (@TopN)
                CONVERT(varchar(34), qs.query_hash, 1)                        AS QueryHash,
                CONVERT(varchar(66), qs.plan_handle, 1)                       AS PlanHandle,
                qs.execution_count                                            AS ExecutionCount,
                qs.total_elapsed_time / 1000.0                                AS TotalDurationMs,
                (qs.total_elapsed_time / qs.execution_count) / 1000.0         AS AvgDurationMs,
                qs.max_elapsed_time / 1000.0                                  AS MaxDurationMs,
                CAST(qs.total_logical_reads AS float)                         AS TotalLogicalReads,
                qs.total_logical_reads * 1.0 / qs.execution_count             AS AvgLogicalReads,
                (qs.total_worker_time / qs.execution_count) / 1000.0          AS AvgCpuMs,
                qs.total_physical_reads * 1.0 / qs.execution_count            AS AvgPhysicalReads,
                qs.last_execution_time                                        AS LastExecutionUtc,
                DB_NAME(st.dbid)                                              AS DatabaseName,
                OBJECT_NAME(st.objectid, st.dbid)                             AS ObjectName,
                SUBSTRING(st.text,
                    (qs.statement_start_offset / 2) + 1,
                    ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text)
                        ELSE qs.statement_end_offset END - qs.statement_start_offset) / 2) + 1) AS QueryText
            FROM sys.dm_exec_query_stats qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
            WHERE qs.execution_count > 0
              AND (qs.total_elapsed_time / qs.execution_count) / 1000.0 >= @ThresholdMs
            ORDER BY AvgDurationMs DESC;
            """,
            r => new MetricItem
            {
                Key = r.GetStr("QueryHash"),
                MetricField = MetricFields.AvgDurationMs,
                NumericValue = r.GetDouble("AvgDurationMs"),
                Severity = Sev(r.GetDouble("AvgDurationMs")),
                Summary = $"{Trim(r.GetStrOrNull("QueryText"), 80)} — avg {r.GetDouble("AvgDurationMs"):0} ms ×{r.GetLong("ExecutionCount")}",
                Payload = new Dictionary<string, object?>
                {
                    ["QueryHash"] = r.GetStr("QueryHash"),
                    ["PlanHandle"] = r.GetStrOrNull("PlanHandle"),
                    ["ExecutionCount"] = r.GetLong("ExecutionCount"),
                    ["TotalDurationMs"] = r.GetDouble("TotalDurationMs"),
                    ["AvgDurationMs"] = r.GetDouble("AvgDurationMs"),
                    ["MaxDurationMs"] = r.GetDouble("MaxDurationMs"),
                    ["TotalLogicalReads"] = r.GetDouble("TotalLogicalReads"),
                    ["AvgLogicalReads"] = r.GetDouble("AvgLogicalReads"),
                    ["AvgCpuMs"] = r.GetDouble("AvgCpuMs"),
                    ["AvgPhysicalReads"] = r.GetDouble("AvgPhysicalReads"),
                    ["LastExecutionUtc"] = r.GetDateTimeOrNull("LastExecutionUtc"),
                    ["DatabaseName"] = r.GetStrOrNull("DatabaseName"),
                    ["ObjectName"] = r.GetStrOrNull("ObjectName"),
                    ["QueryText"] = r.GetStrOrNull("QueryText")
                }
            },
            ct,
            ("@TopN", Schedule.TopN),
            ("@ThresholdMs", (double)Schedule.SlowQueryThresholdMs));

        return rows;
    }

    private static Severity Sev(double avgMs)
        => avgMs > 15000 ? Severity.Critical : avgMs > 5000 ? Severity.Warning : Severity.Info;

    internal static string Trim(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return "(no text)";
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return s.Length <= max ? s : s[..max] + "…";
    }
}
