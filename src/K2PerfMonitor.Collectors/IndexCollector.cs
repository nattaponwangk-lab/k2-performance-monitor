using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// Index Collector — คำแนะนำ index สองแบบ (point-in-time, ไม่ต้อง delta):
///   1) Missing  — จาก sys.dm_db_missing_index_* (impact = avg_user_impact)
///   2) Unused   — index ที่มี write overhead แต่ไม่เคยถูกอ่าน (sys.dm_db_index_usage_stats)
///
/// MetricField = MissingIndexImpact (เฉพาะ missing) → ผูกกับ alert rule "Missing Index (high impact)"
/// Unused ไม่มี alert (informational) แต่ persist เพื่อแสดงในหน้า Indexes
/// </summary>
public sealed class IndexCollector : SqlCollectorBase
{
    public override CollectorType Type => CollectorType.Index;
    public override string DisplayName => "Index Recommendations";

    public IndexCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<IndexCollector> logger)
        : base(conn, schedule, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        var items = new List<MetricItem>();

        // ---- Missing indexes (server-wide via DMV) ----
        items.AddRange(await reader.QueryAsync("""
            SELECT TOP (@TopN)
                DB_NAME(mid.database_id)                        AS DatabaseName,
                OBJECT_SCHEMA_NAME(mid.object_id, mid.database_id) AS SchemaName,
                OBJECT_NAME(mid.object_id, mid.database_id)     AS TableName,
                mid.equality_columns                            AS EqualityColumns,
                mid.inequality_columns                          AS InequalityColumns,
                mid.included_columns                            AS IncludedColumns,
                migs.avg_user_impact                            AS Impact,
                migs.user_seeks                                 AS UserSeeks,
                migs.user_scans                                 AS UserScans
            FROM sys.dm_db_missing_index_group_stats migs
            JOIN sys.dm_db_missing_index_groups mig  ON migs.group_handle = mig.index_group_handle
            JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
            ORDER BY migs.avg_user_impact * (migs.user_seeks + migs.user_scans) DESC;
            """,
            r =>
            {
                var db = r.GetStrOrNull("DatabaseName");
                var schema = r.GetStrOrNull("SchemaName");
                var table = r.GetStrOrNull("TableName");
                var eq = r.GetStrOrNull("EqualityColumns");
                var ineq = r.GetStrOrNull("InequalityColumns");
                var incl = r.GetStrOrNull("IncludedColumns");
                var impact = r.GetDouble("Impact");
                var key = $"Missing:{db}.{schema}.{table}|{eq}|{ineq}";
                return new MetricItem
                {
                    Key = key,
                    MetricField = MetricFields.MissingIndexImpact,
                    NumericValue = impact,
                    Severity = impact > 80 ? Severity.Warning : Severity.Info,
                    Summary = $"Missing index on {schema}.{table} (impact {impact:0}%)",
                    Payload = new Dictionary<string, object?>
                    {
                        ["RecommendationType"] = "Missing",
                        ["DatabaseName"] = db,
                        ["SchemaName"] = schema,
                        ["TableName"] = table,
                        ["EqualityColumns"] = eq,
                        ["InequalityColumns"] = ineq,
                        ["IncludedColumns"] = incl,
                        ["Impact"] = impact,
                        ["UserSeeks"] = r.GetLong("UserSeeks"),
                        ["UserScans"] = r.GetLong("UserScans"),
                        ["UserLookups"] = 0L,
                        ["IndexName"] = (string?)null,
                        ["RecommendationScript"] = BuildCreateIndexScript(db, schema, table, eq, ineq, incl),
                        ["SourceKey"] = key
                    }
                };
            },
            ct, ("@TopN", Schedule.TopN)));

        // ---- Unused indexes (connected database only) ----
        items.AddRange(await reader.QueryAsync("""
            SELECT TOP (@TopN)
                DB_NAME()                          AS DatabaseName,
                OBJECT_SCHEMA_NAME(i.object_id)    AS SchemaName,
                OBJECT_NAME(i.object_id)           AS TableName,
                i.name                             AS IndexName,
                ISNULL(ius.user_seeks, 0)          AS UserSeeks,
                ISNULL(ius.user_scans, 0)          AS UserScans,
                ISNULL(ius.user_lookups, 0)        AS UserLookups,
                ISNULL(ius.user_updates, 0)        AS UserUpdates
            FROM sys.indexes i
            INNER JOIN sys.objects o ON o.object_id = i.object_id AND o.type = 'U'
            LEFT JOIN sys.dm_db_index_usage_stats ius
                   ON ius.object_id = i.object_id AND ius.index_id = i.index_id AND ius.database_id = DB_ID()
            WHERE i.type_desc = 'NONCLUSTERED'
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
              AND ISNULL(ius.user_seeks,0) + ISNULL(ius.user_scans,0) + ISNULL(ius.user_lookups,0) = 0
              AND ISNULL(ius.user_updates,0) > 0
            ORDER BY ISNULL(ius.user_updates,0) DESC;
            """,
            r =>
            {
                var db = r.GetStrOrNull("DatabaseName");
                var schema = r.GetStrOrNull("SchemaName");
                var table = r.GetStrOrNull("TableName");
                var idx = r.GetStrOrNull("IndexName");
                var updates = r.GetLong("UserUpdates");
                var key = $"Unused:{db}.{schema}.{table}|{idx}";
                return new MetricItem
                {
                    Key = key,
                    MetricField = null, // informational — no alert
                    NumericValue = updates,
                    Severity = Severity.Info,
                    Summary = $"Unused index {idx} on {schema}.{table} ({updates} writes, 0 reads)",
                    Payload = new Dictionary<string, object?>
                    {
                        ["RecommendationType"] = "Unused",
                        ["DatabaseName"] = db,
                        ["SchemaName"] = schema,
                        ["TableName"] = table,
                        ["EqualityColumns"] = (string?)null,
                        ["InequalityColumns"] = (string?)null,
                        ["IncludedColumns"] = (string?)null,
                        ["Impact"] = 0.0,
                        ["UserSeeks"] = r.GetLong("UserSeeks"),
                        ["UserScans"] = r.GetLong("UserScans"),
                        ["UserLookups"] = r.GetLong("UserLookups"),
                        ["IndexName"] = idx,
                        ["RecommendationScript"] = $"-- Consider DROP INDEX [{idx}] ON [{schema}].[{table}]; ({updates} writes, 0 reads)",
                        ["SourceKey"] = key
                    }
                };
            },
            ct, ("@TopN", Schedule.TopN)));

        return items;
    }

    internal static string BuildCreateIndexScript(string? db, string? schema, string? table, string? eq, string? ineq, string? incl)
    {
        var keyCols = string.Join(", ", new[] { eq, ineq }.Where(c => !string.IsNullOrWhiteSpace(c)));
        var include = string.IsNullOrWhiteSpace(incl) ? "" : $" INCLUDE ({incl})";
        return $"CREATE NONCLUSTERED INDEX [IX_{table}_missing] ON [{schema}].[{table}] ({keyCols}){include};";
    }
}
