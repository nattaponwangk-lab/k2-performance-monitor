namespace K2PerfMonitor.Core.Enums;

/// <summary>
/// ช่องทางการแจ้งเตือน
/// </summary>
[Flags]
public enum NotificationChannel
{
    None = 0,
    Line = 1,
    Teams = 2,
    Email = 4,
    All = Line | Teams | Email
}
