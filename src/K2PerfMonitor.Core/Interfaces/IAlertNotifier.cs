using K2PerfMonitor.Core.Models;

namespace K2PerfMonitor.Core.Interfaces;

/// <summary>
/// ส่ง alert หนึ่งตัวออกไปยังช่องทางที่กำหนดใน rule (Line/Teams/Email)
/// โดยเคารพ cooldown (LastNotifiedAtUtc + rule.CooldownMinutes) และบันทึกว่าแจ้งแล้ว
/// </summary>
public interface IAlertNotifier
{
    /// <summary>แจ้งเตือน alert (ข้ามถ้ายังอยู่ในช่วง cooldown หรือไม่มี provider เปิดอยู่)</summary>
    Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default);
}
