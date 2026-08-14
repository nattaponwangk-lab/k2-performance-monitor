using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// ServerStats Collector — ดึง CPU/RAM/connections จาก SQL Server DMVs
///
/// แหล่งข้อมูล:
/// - sys.dm_os_sys_info            (cpu_count, physical_memory_kb, sqlserver_start_time, scheduler_count)
/// - sys.dm_os_process_memory      (physical_memory_in_use_kb = RAM ที่ SQL ใช้จริง)
/// - sys.dm_os_ring_buffers        (RING_BUFFER_SCHEDULER_MONITOR → CPU% จริง — ดู CPU note ด้านล่าง)
/// - sys.dm_os_performance_counters (Batch Requests/sec)
/// - sys.dm_exec_connections       (connection count)
/// - sys.dm_exec_requests          (active running requests)
/// - sys.dm_os_waiting_tasks       (blocked count จาก LCK% waits)
///
/// === CPU% Source (แก้จาก heuristic เดิม batch/sec ÷ 10) ===
///   source:    sys.dm_os_ring_buffers, ring_buffer_type = 'RING_BUFFER_SCHEDULER_MONITOR'
///   formula:   CpuPercent (host total) = 100 - SystemIdle
///              SqlProcessCpuPercent    = ProcessUtilization (ส่วนที่ SQL Server ใช้)
///              OtherProcessCpuPercent  = 100 - SystemIdle - ProcessUtilization
///   sampling:  SQL Server เขียน record นี้ ~1 ครั้ง/นาที (สูงสุด ~256 records ล่าสุด)
///              เราอ่าน record ล่าสุด → ค่าจึงอาจล่าช้าได้ถึง ~1 นาที
///   limits:    - granularity ระดับนาที (ไม่ใช่ instantaneous)
///              - เป็น CPU ของทั้งเครื่อง (host) ไม่แยกตาม resource pool
///              - ถ้า instance เพิ่งเริ่มและยังไม่มี record → คืน 0 (ยังไม่มีข้อมูล)
///   ทำงานได้ทุก edition (รวม Express/LocalDB) — ไม่พึ่ง Resource Governor (Enterprise-only)
/// </summary>
public sealed class ServerStatsCollector : SqlCollectorBase
{
    public override CollectorType Type => CollectorType.ServerStats;
    public override string DisplayName => "Server Stats (CPU/RAM)";

    public ServerStatsCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<ServerStatsCollector> logger)
        : base(conn, schedule, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        var payload = new Dictionary<string, object?>();

        // 1) sys.dm_os_sys_info — CPU count, physical memory, uptime, schedulers
        var sys = (await reader.QueryAsync("""
            SELECT TOP 1
                @@SERVERNAME AS InstanceName,
                cpu_count,
                CASE WHEN physical_memory_kb > 0 THEN physical_memory_kb / 1024.0 ELSE 0 END AS TotalMemoryMb,
                DATEDIFF(second, sqlserver_start_time, GETDATE()) AS UptimeSeconds,
                scheduler_count AS OnlineSchedulerCount
            FROM sys.dm_os_sys_info;
            """, r => new
            {
                InstanceName = r.GetStr("InstanceName"),
                CpuCount = r.GetInt("cpu_count"),
                TotalMemoryMb = r.GetDouble("TotalMemoryMb"),
                UptimeSeconds = r.GetLong("UptimeSeconds"),
                OnlineSchedulerCount = r.GetInt("OnlineSchedulerCount")
            }, ct)).FirstOrDefault();

        var instance = sys?.InstanceName ?? "";
        var totalMem = sys?.TotalMemoryMb ?? 0;
        payload["InstanceName"] = instance;
        payload["CpuCount"] = sys?.CpuCount ?? 0;
        payload["OnlineSchedulerCount"] = sys?.OnlineSchedulerCount ?? 0;
        payload["TotalMemoryMb"] = totalMem;
        payload["UptimeSeconds"] = sys?.UptimeSeconds ?? 0;

        // 2) sys.dm_os_process_memory — RAM ที่ SQL Server ใช้จริง
        var usedMem = await reader.ExecuteScalarAsync<double?>("""
            SELECT TOP 1 physical_memory_in_use_kb / 1024.0 AS UsedMemoryMb
            FROM sys.dm_os_process_memory;
            """, ct) ?? 0;
        payload["UsedMemoryMb"] = usedMem;
        var memPercent = totalMem > 0 ? Math.Round(usedMem / totalMem * 100, 1) : 0;
        var availableMem = Math.Max(0, totalMem - usedMem);

        // 3) CPU% จาก ring buffer (ดู CPU note ที่ header)
        var cpu = (await reader.QueryAsync("""
            SELECT TOP 1
                record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') AS SqlCpu,
                record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int')          AS SystemIdle
            FROM (
                SELECT CONVERT(xml, record) AS record, timestamp
                FROM sys.dm_os_ring_buffers
                WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
                  AND record LIKE '%<SystemHealth>%'
            ) AS x
            ORDER BY timestamp DESC;
            """, r => new
            {
                SqlCpu = r.GetInt("SqlCpu"),
                SystemIdle = r.GetInt("SystemIdle")
            }, ct)).FirstOrDefault();

        double cpuPercent = 0, sqlCpuPercent = 0, otherCpuPercent = 0;
        if (cpu is not null)
        {
            sqlCpuPercent = cpu.SqlCpu;
            cpuPercent = Math.Clamp(100 - cpu.SystemIdle, 0, 100);
            otherCpuPercent = Math.Max(0, cpuPercent - sqlCpuPercent);
        }
        payload["CpuPercent"] = cpuPercent;
        payload["SqlProcessCpuPercent"] = sqlCpuPercent;
        payload["OtherProcessCpuPercent"] = otherCpuPercent;

        // 4) Batch Requests/sec
        var batchReqs = await reader.ExecuteScalarAsync<double?>("""
            SELECT TOP 1 cntr_value
            FROM sys.dm_os_performance_counters
            WHERE counter_name LIKE 'Batch Requests/sec%';
            """, ct) ?? 0;
        payload["BatchRequestsPerSec"] = batchReqs;

        // 5) connection count
        var connCount = await reader.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM sys.dm_exec_connections;
            """, ct);

        // 6) active running requests
        var activeReqs = await reader.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM sys.dm_exec_requests WHERE status = 'running';
            """, ct);
        payload["ActiveRequestCount"] = activeReqs;

        // 7) blocked processes (LCK waits)
        var blocked = await reader.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM sys.dm_os_waiting_tasks WHERE wait_type LIKE 'LCK%';
            """, ct);

        return new List<MetricItem>
        {
            MakeItem(MetricFields.CpuPercent, cpuPercent, instance, payload, $"CPU at {cpuPercent:0}% (SQL {sqlCpuPercent:0}%)"),
            MakeItem(MetricFields.MemoryPercent, memPercent, instance, payload, $"Memory at {memPercent:0.0}%"),
            MakeItem(MetricFields.AvailableMemoryMb, availableMem, instance, payload, $"{availableMem:0} MB available"),
            MakeItem(MetricFields.ConnectionCount, connCount, instance, payload, $"{connCount} connections"),
            MakeItem(MetricFields.BlockedProcessCount, blocked, instance, payload, $"{blocked} blocked processes"),
            MakeItem(MetricFields.BatchRequestsPerSec, batchReqs, instance, payload, $"{batchReqs:0} batch/sec")
        };
    }

    private static MetricItem MakeItem(
        string field, double value, string instance,
        IReadOnlyDictionary<string, object?> payload, string summary)
    {
        var sev = field switch
        {
            MetricFields.CpuPercent => value > 95 ? Severity.Critical : value > 80 ? Severity.Warning : Severity.Info,
            MetricFields.MemoryPercent => value > 90 ? Severity.Critical : value > 75 ? Severity.Warning : Severity.Info,
            MetricFields.AvailableMemoryMb => value < 128 ? Severity.Critical : value < 512 ? Severity.Warning : Severity.Info,
            MetricFields.BlockedProcessCount => value > 5 ? Severity.Critical : value > 0 ? Severity.Warning : Severity.Info,
            _ => Severity.Info
        };
        return new MetricItem
        {
            Key = instance,
            MetricField = field,
            NumericValue = value,
            Severity = sev,
            // แนบ payload เต็มไปกับ item แรก ๆ เพื่อให้ repository เขียนคอลัมน์ครบ (dispatch ตาม MetricField)
            Payload = payload,
            Summary = summary
        };
    }
}
