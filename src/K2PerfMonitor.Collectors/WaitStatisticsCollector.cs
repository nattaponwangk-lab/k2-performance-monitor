using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// WaitStatistics Collector — จาก sys.dm_os_wait_stats (cumulative → คำนวณ delta)
///
/// DMV นี้เป็นค่าสะสมตั้งแต่ server start → ต้องเทียบ snapshot ก่อนหน้า (ROADMAP §6)
/// - รอบแรกหลัง worker start = เก็บ baseline (คืน 0 items)
/// - รอบถัดไป = คืน delta ของ wait type ที่ "ไม่ benign" เรียงตาม wait time delta (TopN)
/// - ต้องลงทะเบียนเป็น Singleton (เก็บ baseline ใน memory)
///
/// max_wait_time_ms เป็นค่า max (ไม่ใช่ counter สะสม) จึงเก็บค่าปัจจุบันตรง ๆ
/// </summary>
public sealed class WaitStatisticsCollector : SqlCollectorBase
{
    private readonly DeltaBaseline<WaitRaw> _baseline = new();

    public override CollectorType Type => CollectorType.WaitStatistics;
    public override string DisplayName => "Wait Statistics";

    public WaitStatisticsCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<WaitStatisticsCollector> logger)
        : base(conn, schedule, logger) { }

    private sealed record WaitRaw(long Tasks, double WaitMs, double SignalMs, double MaxMs);

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        // ดึงทุก wait type ที่ยังไม่ถูกกรอง benign (กรอง benign ใน SQL เพื่อลดขนาด)
        var current = (await reader.QueryAsync("""
            SELECT
                wait_type                              AS WaitType,
                waiting_tasks_count                    AS Tasks,
                wait_time_ms                           AS WaitMs,
                signal_wait_time_ms                    AS SignalMs,
                max_wait_time_ms                       AS MaxMs
            FROM sys.dm_os_wait_stats
            WHERE waiting_tasks_count > 0;
            """,
            r => new KeyValuePair<string, WaitRaw>(
                r.GetStr("WaitType"),
                new WaitRaw(r.GetLong("Tasks"), r.GetDouble("WaitMs"), r.GetDouble("SignalMs"), r.GetDouble("MaxMs"))),
            ct))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var deltas = _baseline.Update(current, (prev, cur) => new WaitRaw(
            DeltaMath.Diff(prev.Tasks, cur.Tasks),
            DeltaMath.Diff(prev.WaitMs, cur.WaitMs),
            DeltaMath.Diff(prev.SignalMs, cur.SignalMs),
            cur.MaxMs));

        if (deltas.Count == 0)
            return Array.Empty<MetricItem>(); // รอบ baseline แรก

        // รวม wait time ของ non-benign เพื่อคำนวณ percent
        var nonBenign = deltas
            .Where(d => !BenignWaits.Contains(d.Key) && d.Value.WaitMs > 0)
            .ToList();
        var totalWaitMs = nonBenign.Sum(d => d.Value.WaitMs);
        if (totalWaitMs <= 0)
            return Array.Empty<MetricItem>();

        return nonBenign
            .OrderByDescending(d => d.Value.WaitMs)
            .Take(Schedule.TopN)
            .Select(d =>
            {
                var (type, w) = (d.Key, d.Value);
                var pct = Math.Round(w.WaitMs / totalWaitMs * 100, 1);
                return new MetricItem
                {
                    Key = type,
                    MetricField = MetricFields.WaitTimeMs,
                    NumericValue = w.WaitMs,
                    Severity = w.WaitMs > 60000 ? Severity.Warning : Severity.Info,
                    Summary = $"{type} — {w.WaitMs:0} ms wait ({pct:0.0}%) across {w.Tasks} tasks",
                    Payload = new Dictionary<string, object?>
                    {
                        ["WaitType"] = type,
                        ["WaitingTasksCount"] = w.Tasks,
                        ["WaitTimeMs"] = w.WaitMs,
                        ["SignalWaitTimeMs"] = w.SignalMs,
                        ["MaxWaitTimeMs"] = w.MaxMs,
                        ["WaitPercent"] = pct,
                        ["IsBenign"] = false,
                        ["SourceKey"] = type
                    }
                };
            })
            .ToList();
    }

    /// <summary>
    /// waits ที่ไม่มีนัยต่อ performance (idle/queue waits) — กรองออกก่อนวิเคราะห์
    /// (ชุดมาตรฐานจาก community wait-stats scripts)
    /// </summary>
    private static readonly HashSet<string> BenignWaits = new(StringComparer.OrdinalIgnoreCase)
    {
        "BROKER_EVENTHANDLER", "BROKER_RECEIVE_WAITFOR", "BROKER_TASK_STOP", "BROKER_TO_FLUSH",
        "BROKER_TRANSMITTER", "CHECKPOINT_QUEUE", "CHKPT", "CLR_AUTO_EVENT", "CLR_MANUAL_EVENT",
        "CLR_SEMAPHORE", "DBMIRROR_DBM_EVENT", "DBMIRROR_EVENTS_QUEUE", "DBMIRROR_WORKER_QUEUE",
        "DBMIRRORING_CMD", "DIRTY_PAGE_POLL", "DISPATCHER_QUEUE_SEMAPHORE", "EXECSYNC",
        "FSAGENT", "FT_IFTS_SCHEDULER_IDLE_WAIT", "FT_IFTSHC_MUTEX", "HADR_CLUSAPI_CALL",
        "HADR_FILESTREAM_IOMGR_IOCOMPLETION", "HADR_LOGCAPTURE_WAIT", "HADR_NOTIFICATION_DEQUEUE",
        "HADR_TIMER_TASK", "HADR_WORK_QUEUE", "KSOURCE_WAKEUP", "LAZYWRITER_SLEEP",
        "LOGMGR_QUEUE", "MEMORY_ALLOCATION_EXT", "ONDEMAND_TASK_QUEUE",
        "PARALLEL_REDO_DRAIN_WORKER", "PARALLEL_REDO_LOG_CACHE", "PARALLEL_REDO_TRAN_LIST",
        "PARALLEL_REDO_WORKER_SYNC", "PARALLEL_REDO_WORKER_WAIT_WORK",
        "PREEMPTIVE_XE_GETTARGETSTATE", "PWAIT_ALL_COMPONENTS_INITIALIZED",
        "PWAIT_DIRECTLOGCONSUMER_GETNEXT", "QDS_PERSIST_TASK_MAIN_LOOP_SLEEP",
        "QDS_ASYNC_QUEUE", "QDS_CLEANUP_STALE_QUERIES_TASK_MAIN_LOOP_SLEEP",
        "QDS_SHUTDOWN_QUEUE", "REDO_THREAD_PENDING_WORK", "REQUEST_FOR_DEADLOCK_SEARCH",
        "RESOURCE_QUEUE", "SERVER_IDLE_CHECK", "SLEEP_BPOOL_FLUSH", "SLEEP_DBSTARTUP",
        "SLEEP_DCOMSTARTUP", "SLEEP_MASTERDBREADY", "SLEEP_MASTERMDREADY",
        "SLEEP_MASTERUPGRADED", "SLEEP_MSDBSTARTUP", "SLEEP_SYSTEMTASK", "SLEEP_TASK",
        "SLEEP_TEMPDBSTARTUP", "SNI_HTTP_ACCEPT", "SP_SERVER_DIAGNOSTICS_SLEEP",
        "SQLTRACE_BUFFER_FLUSH", "SQLTRACE_INCREMENTAL_FLUSH_SLEEP", "SQLTRACE_WAIT_ENTRIES",
        "WAIT_FOR_RESULTS", "WAITFOR", "WAITFOR_TASKSHUTDOWN", "WAIT_XTP_RECOVERY",
        "WAIT_XTP_HOST_WAIT", "WAIT_XTP_OFFLINE_CKPT_NEW_LOG", "WAIT_XTP_CKPT_CLOSE",
        "XE_DISPATCHER_JOIN", "XE_DISPATCHER_WAIT", "XE_TIMER_EVENT", "XE_LIVE_TARGET_TVF"
    };
}
