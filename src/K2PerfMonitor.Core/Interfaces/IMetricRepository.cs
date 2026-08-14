using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Results;

namespace K2PerfMonitor.Core.Interfaces;

/// <summary>
/// บันทึก/อ่าน metrics และ alerts จาก Monitoring DB
/// ทำหน้าที่เป็น persistence boundary ระหว่าง Worker กับ EF Core layer
/// (Implementation อยู่ใน Data project)
/// </summary>
public interface IMetricRepository
{
    /// <summary>บันทึก snapshot ของ collector result (เขียนลง metric tables)</summary>
    Task SaveResultAsync(CollectorResult result, CancellationToken cancellationToken = default);

    /// <summary>บันทึก alert ใหม่/อัปเดต alert ที่มีอยู่ (dedup by DedupKey + status)</summary>
    Task<Alert> UpsertAlertAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>บันทึกว่า alert ได้รับ notification แล้ว (อัปเดต LastNotifiedAtUtc)</summary>
    Task MarkAlertNotifiedAsync(long alertId, CancellationToken cancellationToken = default);

    /// <summary>ดึง alerts ที่ยัง active (New/Acknowledged) สำหรับตรวจ auto-resolve</summary>
    Task<IReadOnlyList<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// auto-resolve: ปิด alert ที่ยัง active ของ collector นี้ ที่ไม่ได้อยู่ใน <paramref name="stillFiringDedupKeys"/> อีกต่อไป
    /// (เช่น CPU กลับมาปกติ → alert High CPU ถูก resolve)
    /// </summary>
    /// <returns>จำนวน alert ที่ถูก resolve</returns>
    Task<int> ResolveMissingAsync(
        CollectorType collectorType,
        IReadOnlyCollection<string> stillFiringDedupKeys,
        CancellationToken cancellationToken = default);

    /// <summary>ล้างข้อมูลเก่าตาม retention policy</summary>
    Task<int> PurgeOldDataAsync(int retentionDays, CancellationToken cancellationToken = default);
}
