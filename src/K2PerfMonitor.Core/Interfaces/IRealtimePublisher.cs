using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Results;

namespace K2PerfMonitor.Core.Interfaces;

/// <summary>
/// ส่ง snapshot ขบ real-time ไปยัง SignalR Hub (เพื่อ dashboard อัปเดตทันที)
/// Worker ใช้ implementation ที่เป็น SignalR client (ใน Realtime project)
/// ถ้า disabled จะเป็น NullRealtimePublisher (no-op)
/// </summary>
public interface IRealtimePublisher
{
    /// <summary>เปิดใช้งานหรือไม่</summary>
    bool IsEnabled { get; }

    /// <summary>ส่ง collector snapshot แบบ real-time ไป dashboard</summary>
    Task PublishSnapshotAsync(CollectorResult result, CancellationToken cancellationToken = default);

    /// <summary>ส่ง alert ใหม่ไป dashboard (แสดง toast/banner)</summary>
    Task PublishAlertAsync(Alert alert, CancellationToken cancellationToken = default);
}
