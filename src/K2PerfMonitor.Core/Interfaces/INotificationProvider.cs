using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Models;

namespace K2PerfMonitor.Core.Interfaces;

/// <summary>
/// Contract สำหรับช่องทางแจ้งเตือนหนึ่งช่องทาง (LINE / Teams / Email)
/// แต่ละ provider รับผิดชอบการส่งจริง ตาม config ของตัวเอง
/// </summary>
public interface INotificationProvider
{
    /// <summary>ชื่อช่องทาง</summary>
    string Name { get; }

    /// <summary>ช่องทางที่ provider นี้แทน (ใช้จับคู่กับ AlertRule.Channels flags)</summary>
    NotificationChannel Channel { get; }

    /// <summary>เปิดใช้งานอยู่หรือไม่ (อ้างอิงจาก config)</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// ส่งการแจ้งเตือน
    /// </summary>
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
