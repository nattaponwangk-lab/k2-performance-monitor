using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// ExecutionPlan Collector — เก็บ query plan XML ของ top-N slow queries
/// จาก sys.dm_exec_query_plan(plan_handle)
///
/// Informational (ไม่มี numeric alert) — เก็บ plan ไว้ให้หน้า ExecutionPlans เปิดดู
/// SourceKey = query_hash hex (จับคู่ SlowQuery ได้)
/// </summary>
public sealed class ExecutionPlanCollector : SqlCollectorBase
{
    public override CollectorType Type => CollectorType.ExecutionPlan;
    public override string DisplayName => "Execution Plans";

    public ExecutionPlanCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        CollectionContext context,
        ILogger<ExecutionPlanCollector> logger)
        : base(conn, schedule, context, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        return await reader.QueryAsync("""
            SELECT TOP (@TopN)
                CONVERT(varchar(34), qs.query_hash, 1)                AS QueryHash,
                CONVERT(varchar(66), qs.plan_handle, 1)               AS PlanHandle,
                qs.execution_count                                    AS ExecutionCount,
                (qs.total_elapsed_time / qs.execution_count) / 1000.0 AS AvgDurationMs,
                (qs.total_worker_time / qs.execution_count) / 1000.0  AS AvgCpuMs,
                qs.total_logical_reads * 1.0 / qs.execution_count     AS AvgLogicalReads,
                DB_NAME(st.dbid)                                      AS DatabaseName,
                OBJECT_NAME(st.objectid, st.dbid)                     AS ObjectName,
                st.text                                               AS QueryText,
                CAST(qp.query_plan AS nvarchar(max))                  AS PlanXml
            FROM sys.dm_exec_query_stats qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
            CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
            WHERE qs.execution_count > 0
              AND (qs.total_elapsed_time / qs.execution_count) / 1000.0 >= @ThresholdMs
              AND qp.query_plan IS NOT NULL
            ORDER BY AvgDurationMs DESC;
            """,
            r =>
            {
                var hash = r.GetStr("QueryHash");
                return new MetricItem
                {
                    Key = hash,
                    MetricField = null,
                    NumericValue = r.GetDouble("AvgDurationMs"),
                    Severity = Severity.Info,
                    Summary = $"Plan for {SlowQueryCollector.Trim(r.GetStrOrNull("QueryText"), 60)} (avg {r.GetDouble("AvgDurationMs"):0} ms)",
                    Payload = new Dictionary<string, object?>
                    {
                        ["QueryHash"] = hash,
                        ["PlanHandle"] = r.GetStrOrNull("PlanHandle"),
                        ["ExecutionCount"] = r.GetLong("ExecutionCount"),
                        ["AvgDurationMs"] = r.GetDouble("AvgDurationMs"),
                        ["AvgCpuMs"] = r.GetDouble("AvgCpuMs"),
                        ["AvgLogicalReads"] = r.GetDouble("AvgLogicalReads"),
                        ["DatabaseName"] = r.GetStrOrNull("DatabaseName"),
                        ["ObjectName"] = r.GetStrOrNull("ObjectName"),
                        ["QueryText"] = r.GetStrOrNull("QueryText"),
                        ["PlanXml"] = r.GetStrOrNull("PlanXml") ?? "",
                        ["SourceKey"] = hash
                    }
                };
            },
            ct,
            ("@TopN", Schedule.TopN),
            ("@ThresholdMs", (double)Schedule.SlowQueryThresholdMs));
    }
}
