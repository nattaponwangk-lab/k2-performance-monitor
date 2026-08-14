using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Web.Models;

public class ExecutionPlanVm
{
    public string QueryHash { get; set; } = "";
    public string? DatabaseName { get; set; }
    public string? ObjectName { get; set; }
    public long ExecutionCount { get; set; }
    public double AvgDurationMs { get; set; }
    public double AvgCpuMs { get; set; }
    public double AvgLogicalReads { get; set; }
    public string? QueryText { get; set; }
    public string PlanXml { get; set; } = "";
    public Severity Severity { get; set; } = Severity.Info;
}
