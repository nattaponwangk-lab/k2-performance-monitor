using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// K2 SmartObject call ที่ช้า — จาก K2 Logger / SmartBox / Workflow Reports SmartObjects
/// SourceKey = "{SmartObjectName}|{Method}"
/// </summary>
public class K2SmartObjectStatEntity : MetricEntityBase
{
    /// <summary>ชื่อ SmartObject</summary>
    [MaxLength(256)]
    public string? SmartObjectName { get; set; }

    /// <summary>เมธอดที่เรียก (List/Read/Save/Delete/Execute)</summary>
    [MaxLength(64)]
    public string? Method { get; set; }

    /// <summary>service broker / service type (เช่น SQL, CRM, SharePoint)</summary>
    [MaxLength(128)]
    public string? ServiceType { get; set; }

    /// <summary>เวลาที่ call ใช้ (ms)</summary>
    public double DurationMs { get; set; }

    /// <summary>จำนวน call ในช่วง snapshot</summary>
    public long CallCount { get; set; }

    /// <summary>avg call time</summary>
    public double AvgDurationMs { get; set; }

    /// <summary>max call time</summary>
    public double MaxDurationMs { get; set; }

    /// <summary>จำนวน rows ที่ return (ถ้าเป็น List)</summary>
    public long? RowsReturned { get; set; }

    /// <summary>error message ถ้า call ล้มเหลว</summary>
    [MaxLength(512)]
    public string? ErrorMessage { get; set; }
}
