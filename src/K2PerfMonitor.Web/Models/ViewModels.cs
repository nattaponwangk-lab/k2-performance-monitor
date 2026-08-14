using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Web.Models;

/// <summary>
/// DTO สำหรับแสดงผลบน Dashboard (mock ก่อน, ภายหลัง map จาก EF entity)
/// </summary>

public class OverviewVm
{
    public double HealthScore { get; set; }          // 0-100
    public string? InstanceName { get; set; }        // SQL Server instance name
    public double CpuPercent { get; set; }
    public double MemoryPercent { get; set; }
    public double AvailableMemoryMb { get; set; }
    public double UsedMemoryMb { get; set; }
    public double TotalMemoryMb { get; set; }
    public int ConnectionCount { get; set; }
    public int ActiveRequestCount { get; set; }
    public int BlockedProcessCount { get; set; }
    public double BatchRequestsPerSec { get; set; }
    public long UptimeSeconds { get; set; }
    public int OnlineSchedulerCount { get; set; }
    public int ActiveAlertCount { get; set; }
    public int CriticalAlertCount { get; set; }

    /// <summary>series สำหรับ mini chart (CPU/RAM ย้อนหลัง)</summary>
    public List<ChartPoint> CpuHistory { get; set; } = new();
    public List<ChartPoint> MemoryHistory { get; set; } = new();
}

public class ChartPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

public class SlowQueryVm
{
    public string QueryHash { get; set; } = "";
    public string QueryText { get; set; } = "";
    public string? DatabaseName { get; set; }
    public string? ObjectName { get; set; }
    public long ExecutionCount { get; set; }
    public double AvgDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double TotalDurationMs { get; set; }
    public double AvgLogicalReads { get; set; }
    public double AvgCpuMs { get; set; }
    public DateTime? LastExecutionUtc { get; set; }
    public Severity Severity { get; set; } = Severity.Info;
}

public class WaitStatVm
{
    public string WaitType { get; set; } = "";
    public long WaitingTasksCount { get; set; }
    public double WaitTimeMs { get; set; }
    public double SignalWaitTimeMs { get; set; }
    public double MaxWaitTimeMs { get; set; }
    public double WaitPercent { get; set; }
    public bool IsBenign { get; set; }
    public string Category { get; set; } = "";
}

public class BlockingVm
{
    public int BlockedSessionId { get; set; }
    public int BlockingSessionId { get; set; }
    public double WaitDurationMs { get; set; }
    public string WaitType { get; set; } = "";
    public string? Resource { get; set; }
    public string? RequestedLockMode { get; set; }
    public string? BlockedQueryText { get; set; }
    public string? BlockingQueryText { get; set; }
    public string? BlockedLoginName { get; set; }
    public string? BlockingLoginName { get; set; }
    public Severity Severity { get; set; } = Severity.Info;
}

public class DeadlockVm
{
    public DateTime DeadlockAtUtc { get; set; }
    public string VictimProcessId { get; set; } = "";
    public string VictimQueryText { get; set; } = "";
    public string? VictimLoginName { get; set; }
    public string SurvivorQueryText { get; set; } = "";
    public string? SurvivorLoginName { get; set; }
}

public class IndexRecommendationVm
{
    public string RecommendationType { get; set; } = "Missing"; // Missing / Unused
    public string? DatabaseName { get; set; }
    public string? TableName { get; set; }
    public string? EqualityColumns { get; set; }
    public string? InequalityColumns { get; set; }
    public string? IncludedColumns { get; set; }
    public double Impact { get; set; }
    public long UserSeeks { get; set; }
    public long UserScans { get; set; }
    public string? IndexName { get; set; }
    public string? RecommendationScript { get; set; }
}

public class IoStatVm
{
    public string DatabaseName { get; set; } = "";
    public string? LogicalFileName { get; set; }
    public string? FileType { get; set; }
    public long NumOfReads { get; set; }
    public long NumOfWrites { get; set; }
    public double IoStallMsPerRead { get; set; }
    public double IoStallMsPerWrite { get; set; }
    public Severity Severity { get; set; } = Severity.Info;
}

public class StoredProcedureVm
{
    public string? DatabaseName { get; set; }
    public string? ObjectName { get; set; }
    public long ExecutionCount { get; set; }
    public double AvgElapsedMs { get; set; }
    public double MaxElapsedMs { get; set; }
    public double AvgLogicalReads { get; set; }
    public DateTime? LastExecutionUtc { get; set; }
    public Severity Severity { get; set; } = Severity.Info;
}

public class K2WorkflowVm
{
    public long ProcSetId { get; set; }
    public long? ProcInstId { get; set; }
    public string? WorkflowName { get; set; }
    public string? Folio { get; set; }
    public string Status { get; set; } = "";
    public double DurationMs { get; set; }
    public double? CurrentActivityWaitMs { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string? Originator { get; set; }
    public bool IsStuck { get; set; }
    public Severity Severity { get; set; } = Severity.Info;
}

public class K2SmartFormVm
{
    public string? FormName { get; set; }
    public string? FormId { get; set; }
    public double FormLoadMs { get; set; }
    public double? InitializeRuleMs { get; set; }
    public long LoadCount { get; set; }
    public double AvgLoadMs { get; set; }
    public double MaxLoadMs { get; set; }
    public string? FormUrl { get; set; }
    public Severity Severity { get; set; } = Severity.Info;
}

public class K2SmartObjectVm
{
    public string? SmartObjectName { get; set; }
    public string? Method { get; set; }
    public string? ServiceType { get; set; }
    public double DurationMs { get; set; }
    public long CallCount { get; set; }
    public double AvgDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public long? RowsReturned { get; set; }
    public Severity Severity { get; set; } = Severity.Info;
}
