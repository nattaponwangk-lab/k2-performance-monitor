using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Results;

namespace K2PerfMonitor.Core.Interfaces;

/// <summary>
/// Contract สำหรับ collector ทั้ง 12 ตัว
/// แต่ละ collector รับผิดชอบดึงข้อมูลจาก source (SQL DMVs / K2 tables / logs)
/// แล้วคืนผลลัพธ์เป็น <see cref="CollectorResult"/>
/// </summary>
public interface ICollector
{
    /// <summary>ประเภท collector (1-12)</summary>
    CollectorType Type { get; }

    /// <summary>ชื่อที่แสดงใน log/UI</summary>
    string DisplayName { get; }

    /// <summary>
    /// ดึงข้อมูล snapshot ปัจจุบันจาก source
    /// </summary>
    /// <param name="cancellationToken">token ยกเลิก</param>
    /// <returns>ผลลัพธ์ของการเก็บข้อมูล (อาจว่างถ้าไม่มีข้อมูล)</returns>
    Task<CollectorResult> CollectAsync(CancellationToken cancellationToken = default);
}
