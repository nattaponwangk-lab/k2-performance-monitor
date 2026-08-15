using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// Database Stats Collector — discover database ใน target instance + ขนาด/สถานะ
/// จาก sys.databases + sys.master_files (ไม่ hard-code ชื่อ database)
///
/// - รองรับทุกสถานะ: ONLINE/OFFLINE/RESTORING/RECOVERY_PENDING/SUSPECT ฯลฯ
/// - policy include/exclude system database ผ่าน CollectorSchedule.IncludeSystemDatabases
/// - size = ผลรวม master_files (allocated); free space ระดับไฟล์ต้อง query ต่อ database (documented — ไม่ทำที่นี่)
/// - MetricField = null (informational) — NumericValue = TotalSizeMb
/// </summary>
public sealed class DatabaseStatsCollector : SqlCollectorBase
{
    public override CollectorType Type => CollectorType.DatabaseStats;
    public override string DisplayName => "Database Discovery";

    public DatabaseStatsCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        CollectionContext context,
        ILogger<DatabaseStatsCollector> logger)
        : base(conn, schedule, context, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        // include system db เป็น parameter (ไม่ hard-code ชื่อ) — d.database_id <= 4 = system
        return await reader.QueryAsync("""
            SELECT
                d.database_id                          AS DatabaseId,
                d.name                                 AS DatabaseName,
                d.state_desc                           AS State,
                d.recovery_model_desc                  AS RecoveryModel,
                d.compatibility_level                  AS CompatibilityLevel,
                CASE WHEN d.database_id <= 4 THEN 1 ELSE 0 END AS IsSystem,
                ISNULL(SUM(CASE WHEN mf.type = 0 THEN CAST(mf.size AS bigint) END) * 8 / 1024.0, 0) AS DataSizeMb,
                ISNULL(SUM(CASE WHEN mf.type = 1 THEN CAST(mf.size AS bigint) END) * 8 / 1024.0, 0) AS LogSizeMb
            FROM sys.databases d
            LEFT JOIN sys.master_files mf ON mf.database_id = d.database_id
            WHERE (@IncludeSystem = 1 OR d.database_id > 4)
            GROUP BY d.database_id, d.name, d.state_desc, d.recovery_model_desc, d.compatibility_level;
            """,
            r =>
            {
                var name = r.GetStr("DatabaseName");
                var data = r.GetDouble("DataSizeMb");
                var log = r.GetDouble("LogSizeMb");
                var total = Math.Round(data + log, 1);
                var state = r.GetStr("State");
                return new MetricItem
                {
                    Key = name,
                    MetricField = null, // informational
                    NumericValue = total,
                    Severity = state == "ONLINE" ? Severity.Info : Severity.Warning,
                    Summary = $"{name} — {state}, {total:0} MB (data {data:0} / log {log:0})",
                    Payload = new Dictionary<string, object?>
                    {
                        ["DatabaseId"] = r.GetInt("DatabaseId"),
                        ["DatabaseName"] = name,
                        ["State"] = state,
                        ["RecoveryModel"] = r.GetStrOrNull("RecoveryModel"),
                        ["CompatibilityLevel"] = r.GetInt("CompatibilityLevel"),
                        ["IsSystemDatabase"] = r.GetInt("IsSystem") == 1,
                        ["DataSizeMb"] = Math.Round(data, 1),
                        ["LogSizeMb"] = Math.Round(log, 1),
                        ["TotalSizeMb"] = total,
                        ["SourceKey"] = name
                    }
                };
            },
            ct,
            ("@IncludeSystem", Schedule.IncludeSystemDatabases ? 1 : 0));
    }
}
