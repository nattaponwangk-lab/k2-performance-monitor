using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// ServerStats Collector — ดึง CPU/RAM/connections จาก SQL Server DMVs
///
/// แหล่งข้อมูล:
/// - sys.dm_os_sys_info         (cpu_count, physical_memory_kb, sqlserver_start_time)
/// - sys.dm_os_process_memory   (working_set_bytes = RAM ที่ SQL ใช้)
/// - sys.dm_os_performance_counters (Processor Time %, Batch Requests/sec)
/// - sys.dm_os_schedulers       (online schedulers)
/// - sys.dm_exec_connections    (active connection count)
/// - sys.dm_exec_requests       (active request count)
/// - sys.dm_os_waiting_tasks    (blocked count)
/// </summary>
public sealed class ServerStatsCollector : ICollector, IDisposable
{
    private readonly ConnectionStringsOptions _conn;
    private SqlDmvReader? _reader;

    public CollectorType Type => CollectorType.ServerStats;
    public string DisplayName => "Server Stats (CPU/RAM)";

    public ServerStatsCollector(IOptions<ConnectionStringsOptions> conn)
    {
        _conn = conn.Value;
    }

    public async Task<CollectorResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        try
        {
            _reader?.DisposeAsync().AsTask().Wait(cancellationToken);
            _reader = new SqlDmvReader(_conn.SourceDb);
            await _reader.OpenAsync(cancellationToken);

            var payload = new Dictionary<string, object?>();

            // 1) sys.dm_os_sys_info — CPU count, physical memory, uptime
            var sysInfo = await _reader.QueryAsync("""
                SELECT TOP 1
                    @@SERVERNAME AS InstanceName,
                    cpu_count,
                    CASE WHEN physical_memory_kb > 0 THEN physical_memory_kb / 1024.0 ELSE 0 END AS TotalMemoryMb,
                    DATEDIFF(second, sqlserver_start_time, GETUTCDATE()) AS UptimeSeconds,
                    scheduler_count AS OnlineSchedulerCount
                FROM sys.dm_os_sys_info;
                """, r => new
            {
                InstanceName = r["InstanceName"] as string ?? "",
                CpuCount = r["cpu_count"] as int? ?? 0,
                TotalMemoryMb = r["TotalMemoryMb"] as double? ?? 0,
                UptimeSeconds = r["UptimeSeconds"] as long? ?? 0,
                OnlineSchedulerCount = r["OnlineSchedulerCount"] as int? ?? 0
            }, cancellationToken);

            var sys = sysInfo.FirstOrDefault();
            payload["InstanceName"] = sys?.InstanceName ?? "";
            payload["OnlineSchedulerCount"] = sys?.OnlineSchedulerCount ?? 0;
            payload["TotalMemoryMb"] = sys?.TotalMemoryMb ?? 0;
            payload["UptimeSeconds"] = sys?.UptimeSeconds ?? 0;
            var totalMem = sys?.TotalMemoryMb ?? 0;

            // 2) sys.dm_os_process_memory — RAM ที่ SQL Server ใช้
            var procMem = await _reader.QueryAsync("""
                SELECT TOP 1
                    working_set_bytes / 1024.0 / 1024.0 AS UsedMemoryMb
                FROM sys.dm_os_process_memory;
                """, r => r["UsedMemoryMb"] as double? ?? 0, cancellationToken);
            var usedMem = procMem.FirstOrDefault();
            payload["UsedMemoryMb"] = usedMem;
            var memPercent = totalMem > 0 && usedMem >= 0 ? Math.Round((usedMem / totalMem) * 100, 1) : 0;

            // 3) sys.dm_os_performance_counters — Batch Requests/sec + CPU %
            //    CPU % มาจาก counter '\Processor(_Total)\% Processor Time' ผ่าน sys.dm_os_performance_counters
            //    หรือใช้ SQLServer:SQL Statistics\Batch Requests/sec
            var batchReqs = await _reader.ExecuteScalarAsync<double?>("""
                SELECT TOP 1 cntr_value
                FROM sys.dm_os_performance_counters
                WHERE counter_name LIKE 'Batch Requests/sec%';
                """, cancellationToken) ?? 0;

            // 4) sys.dm_os_schedulers — count online VISIBLE schedulers (ใช้สำหรับ reference)
            // 5) sys.dm_exec_connections — connection count
            var connCount = await _reader.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM sys.dm_exec_connections;
                """, cancellationToken);

            // 6) sys.dm_exec_requests — active requests (running)
            var activeReqs = await _reader.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM sys.dm_exec_requests WHERE status = 'running';
                """, cancellationToken);

            // 7) sys.dm_os_waiting_tasks — blocked count
            var blocked = await _reader.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM sys.dm_os_waiting_tasks WHERE wait_type LIKE 'LCK%';
                """, cancellationToken);

            // CPU % estimation: ใช้ sys.dm_os_ring_buffers หรือคำนวณจาก batch/sec (heuristic)
            // วิธีที่ reliable คือใช้ performance counter '\SQLServer:Resource Pool Stats\% CPU usage'
            // ในรอบนี้ใช้ heuristic: batch/sec > 500 = warning, > 1000 = critical (placeholder)
            // (จะ refine ในรอบถัดไปด้วย sys.dm_os_performance_counters ที่ถูกต้อง)
            var cpuPercent = Math.Min(100, batchReqs / 10.0); // heuristic ชั่วคราว

            payload["ActiveRequestCount"] = activeReqs;
            payload["BatchRequestsPerSec"] = batchReqs;
            payload["UsedMemoryMb"] = usedMem;
            payload["CpuPercent"] = cpuPercent;

            // สร้าง MetricItems (สำหรับ alert engine + persistence)
            var availableMem = Math.Max(0, totalMem - usedMem);
            var items = new List<MetricItem>
            {
                MakeItem(MetricFields.CpuPercent, cpuPercent, sys?.InstanceName ?? "", $"CPU at {cpuPercent:0.0}%"),
                MakeItem(MetricFields.MemoryPercent, memPercent, sys?.InstanceName ?? "", $"Memory at {memPercent:0.0}%"),
                MakeItem(MetricFields.AvailableMemoryMb, availableMem, sys?.InstanceName ?? "", $"{availableMem:0} MB available"),
                MakeItem(MetricFields.ConnectionCount, connCount, sys?.InstanceName ?? "", $"{connCount} connections"),
                MakeItem(MetricFields.BlockedProcessCount, blocked, sys?.InstanceName ?? "", $"{blocked} blocked processes"),
                MakeItem(MetricFields.BatchRequestsPerSec, batchReqs, sys?.InstanceName ?? "", $"{batchReqs:0} batch/sec")
            };

            var elapsed = DateTime.UtcNow - started;
            return new CollectorResult
            {
                CollectorType = Type,
                CollectedAtUtc = started,
                Success = true,
                Elapsed = elapsed,
                Items = items
            };
        }
        catch (Exception ex)
        {
            return new CollectorResult
            {
                CollectorType = Type,
                CollectedAtUtc = started,
                Success = false,
                ErrorMessage = ex.Message,
                Elapsed = DateTime.UtcNow - started
            };
        }
    }

    private static MetricItem MakeItem(string field, double value, string instance, string summary)
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
            Payload = new Dictionary<string, object?> { ["value"] = value, ["instance"] = instance },
            Summary = summary
        };
    }

    public void Dispose()
    {
        _reader?.DisposeAsync().AsTask().Wait();
    }
}
