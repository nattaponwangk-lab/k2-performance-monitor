namespace K2PerfMonitor.Core.Enums;

/// <summary>
/// สถานะของ alert ใน lifecycle
/// </summary>
public enum AlertStatus
{
    /// <summary>เพิ่งสร้าง ยังไม่ได้รับทราบ</summary>
    New = 0,

    /// <summary>มีคนรับทราบแล้ว (acknowledged) แต่ยังไม่ resolved</summary>
    Acknowledged = 1,

    /// <summary>สภาวะกลับเป็นปกติแล้ว</summary>
    Resolved = 2
}
