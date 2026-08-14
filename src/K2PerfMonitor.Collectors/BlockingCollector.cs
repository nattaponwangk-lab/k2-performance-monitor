using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// Blocking Collector — session ที่ถูก block ปัจจุบัน จาก sys.dm_exec_requests
/// (point-in-time snapshot ไม่ใช่ cumulative → ไม่ต้อง delta)
///
/// SourceKey = "{blocked}|{blocking}" · MetricField = BlockingDurationMs (= wait_time ms)
/// </summary>
public sealed class BlockingCollector : SqlCollectorBase
{
    public override CollectorType Type => CollectorType.Blocking;
    public override string DisplayName => "Blocking";

    public BlockingCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<BlockingCollector> logger)
        : base(conn, schedule, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        return await reader.QueryAsync("""
            SELECT
                r.session_id                          AS BlockedSessionId,
                r.blocking_session_id                 AS BlockingSessionId,
                r.wait_time                           AS WaitDurationMs,
                r.wait_type                           AS WaitType,
                r.wait_resource                       AS Resource,
                bs.login_name                         AS BlockedLoginName,
                bl.login_name                         AS BlockingLoginName,
                bt.text                               AS BlockedQueryText,
                bct.text                              AS BlockingQueryText
            FROM sys.dm_exec_requests r
            JOIN sys.dm_exec_sessions bs             ON bs.session_id = r.session_id
            LEFT JOIN sys.dm_exec_sessions bl        ON bl.session_id = r.blocking_session_id
            OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) bt
            LEFT JOIN sys.dm_exec_connections bc     ON bc.session_id = r.blocking_session_id
            OUTER APPLY sys.dm_exec_sql_text(bc.most_recent_sql_handle) bct
            WHERE r.blocking_session_id <> 0
            ORDER BY r.wait_time DESC;
            """,
            r =>
            {
                var blocked = r.GetInt("BlockedSessionId");
                var blocking = r.GetInt("BlockingSessionId");
                var waitMs = r.GetDouble("WaitDurationMs");
                var key = $"{blocked}|{blocking}";
                return new MetricItem
                {
                    Key = key,
                    MetricField = MetricFields.BlockingDurationMs,
                    NumericValue = waitMs,
                    Severity = waitMs > 120000 ? Severity.Critical : waitMs > 30000 ? Severity.Warning : Severity.Info,
                    Summary = $"SPID {blocked} blocked by {blocking} for {waitMs:0} ms ({r.GetStrOrNull("WaitType")})",
                    Payload = new Dictionary<string, object?>
                    {
                        ["BlockedSessionId"] = blocked,
                        ["BlockingSessionId"] = blocking,
                        ["WaitDurationMs"] = waitMs,
                        ["WaitType"] = r.GetStr("WaitType"),
                        ["Resource"] = r.GetStrOrNull("Resource"),
                        ["RequestedLockMode"] = (string?)null,
                        ["BlockedQueryText"] = r.GetStrOrNull("BlockedQueryText"),
                        ["BlockingQueryText"] = r.GetStrOrNull("BlockingQueryText"),
                        ["BlockedLoginName"] = r.GetStrOrNull("BlockedLoginName"),
                        ["BlockingLoginName"] = r.GetStrOrNull("BlockingLoginName"),
                        ["SourceKey"] = key
                    }
                };
            }, ct);
    }
}
