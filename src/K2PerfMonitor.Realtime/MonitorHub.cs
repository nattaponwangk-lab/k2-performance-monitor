using K2PerfMonitor.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace K2PerfMonitor.Realtime;

/// <summary>
/// SignalR Hub บน Web — ทำหน้าที่ relay:
///   Worker (SignalR client) → PublishSnapshot/PublishAlert → hub → broadcast ให้ browser (Clients.Others)
/// browser subscribe event ReceiveSnapshot / ReceiveAlert เพื่ออัปเดต dashboard สด
/// </summary>
public sealed class MonitorHub : Hub
{
    /// <summary>Worker เรียกเพื่อส่ง snapshot → relay ให้ browser ทุกตัว (ยกเว้นผู้ส่ง)</summary>
    public Task PublishSnapshot(MetricSnapshotDto snapshot)
        => Clients.Others.SendAsync(RealtimeMessages.ReceiveSnapshot, snapshot);

    /// <summary>Worker เรียกเพื่อส่ง alert ใหม่ → relay ให้ browser</summary>
    public Task PublishAlert(AlertDto alert)
        => Clients.Others.SendAsync(RealtimeMessages.ReceiveAlert, alert);
}
