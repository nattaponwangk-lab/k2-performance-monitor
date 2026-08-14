using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Interfaces;

/// <summary>
/// การลงทะเบียน collector หนึ่งตัว พร้อมรอบเวลา (สำหรับสร้าง Hangfire recurring job)
/// </summary>
/// <param name="Type">ประเภท collector</param>
/// <param name="DisplayName">ชื่อแสดงผล</param>
/// <param name="JobId">recurring job id (เช่น "collector:ServerStats")</param>
/// <param name="IntervalSeconds">รอบการเก็บ (วินาที)</param>
public readonly record struct CollectorRegistration(
    CollectorType Type,
    string DisplayName,
    string JobId,
    int IntervalSeconds);

/// <summary>
/// รวมรายการ collector ทั้งหมดที่ลงทะเบียนไว้ + รอบเวลาของแต่ละตัว
/// Worker ใช้ผูก recurring job; Web ใช้แสดงสถานะ schedule
/// </summary>
public interface ICollectorRegistry
{
    /// <summary>รายการ collector + schedule (เฉพาะตัวที่ถูก register จริงใน DI)</summary>
    IReadOnlyList<CollectorRegistration> Registrations { get; }
}
