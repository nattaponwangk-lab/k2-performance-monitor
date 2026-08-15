namespace K2PerfMonitor.Web.Models;

public class DatabaseStatVm
{
    public int DatabaseId { get; set; }
    public string DatabaseName { get; set; } = "";
    public string State { get; set; } = "";
    public string? RecoveryModel { get; set; }
    public int CompatibilityLevel { get; set; }
    public bool IsSystemDatabase { get; set; }
    public double DataSizeMb { get; set; }
    public double LogSizeMb { get; set; }
    public double TotalSizeMb { get; set; }
}
