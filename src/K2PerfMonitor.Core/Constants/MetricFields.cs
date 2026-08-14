namespace K2PerfMonitor.Core.Constants;

/// <summary>
/// ชื่อฟิลด์ metric มาตรฐาน (ใช้สื่อสารระหว่าง collector → alert rule → dashboard)
/// AlertRule.MetricField จะใช้ค่าใน class นี้เพื่อจับคู่กับ MetricItem.MetricField
/// </summary>
public static class MetricFields
{
    // SlowQuery / StoredProcedure
    public const string AvgDurationMs = "AvgDurationMs";
    public const string TotalDurationMs = "TotalDurationMs";
    public const string AvgLogicalReads = "AvgLogicalReads";
    public const string AvgCpuMs = "AvgCpuMs";
    public const string ExecutionCount = "ExecutionCount";

    // WaitStatistics
    public const string WaitTimeMs = "WaitTimeMs";
    public const string WaitTimePerSec = "WaitTimePerSec";
    public const string WaitingTasksCount = "WaitingTasksCount";

    // Blocking
    public const string BlockingDurationMs = "BlockingDurationMs";
    public const string BlockedProcessCount = "BlockedProcessCount";

    // ServerStats (CPU/RAM)
    public const string CpuPercent = "CpuPercent";
    public const string MemoryPercent = "MemoryPercent";
    public const string AvailableMemoryMb = "AvailableMemoryMb";
    public const string ConnectionCount = "ConnectionCount";
    public const string BatchRequestsPerSec = "BatchRequestsPerSec";

    // I/O
    public const string IoStallMsPerRead = "IoStallMsPerRead";
    public const string IoStallMsPerWrite = "IoStallMsPerWrite";

    // Index
    public const string MissingIndexImpact = "MissingIndexImpact";
    public const string IndexUserScans = "IndexUserScans";
    public const string IndexUserSeeks = "IndexUserSeeks";

    // K2 Workflow
    public const string WorkflowDurationMs = "WorkflowDurationMs";
    public const string StuckWorkflowCount = "StuckWorkflowCount";

    // K2 SmartForm / SmartObject
    public const string FormLoadMs = "FormLoadMs";
    public const string SmartObjectCallMs = "SmartObjectCallMs";
}
