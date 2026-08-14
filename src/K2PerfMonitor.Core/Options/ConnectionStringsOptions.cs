namespace K2PerfMonitor.Core.Options;

/// <summary>
/// Connection strings ของระบบ
/// - MonitorDb: database K2PerfMonitor ที่ใช้เก็บประวัติ (DB แยกอิสระ)
/// - SourceDb: database ของระบบ K2/App ที่จะ monitor (read-only query)
/// </summary>
public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>Monitoring database (K2PerfMonitor) — เก็บ metrics/alerts/history</summary>
    public string MonitorDb { get; set; } = string.Empty;

    /// <summary>SQL Server instance ที่รัน K2 + App database (สำหรับ DMV queries)</summary>
    public string SourceDb { get; set; } = string.Empty;

    /// <summary>K2 source database เช่น [K2] (ProcInst/ActivityInst) — ถ้าใช้ instance เดียวกับ SourceDb ใส่เหมือนกันได้</summary>
    public string K2Db { get; set; } = string.Empty;
}
