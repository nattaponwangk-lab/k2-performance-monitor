using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// Server stats (CPU/RAM/connections) — จาก sys.dm_os_* DMVs + performance counters
/// SourceKey = "Server" (1 แถวต่อ snapshot) หรือต่อ scheduler
/// </summary>
public class ServerStatEntity : MetricEntityBase
{
    /// <summary>SQL Server instance name</summary>
    [MaxLength(128)]
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>เวลาทำงาน (วินาที) นับจาก startup</summary>
    public long UptimeSeconds { get; set; }

    /// <summary>CPU % (ประมาณการ จาก performance counter / scheduler)</summary>
    public double CpuPercent { get; set; }

    /// <summary>memory % ที่ใช้</summary>
    public double MemoryPercent { get; set; }

    /// <summary>memory ที่ใช้ (MB)</summary>
    public double UsedMemoryMb { get; set; }

    /// <summary>memory ที่เหลือ (MB)</summary>
    public double AvailableMemoryMb { get; set; }

    /// <summary>total physical memory (MB)</summary>
    public double TotalMemoryMb { get; set; }

    /// <summary>จำนวน connection / session ปัจจุบัน</summary>
    public int ConnectionCount { get; set; }

    /// <summary>active requests</summary>
    public int ActiveRequestCount { get; set; }

    /// <summary>batch requests/sec</summary>
    public double BatchRequestsPerSec { get; set; }

    /// <summary>online schedulers</summary>
    public int OnlineSchedulerCount { get; set; }

    /// <summary>blocked processes</summary>
    public int BlockedProcessCount { get; set; }
}
