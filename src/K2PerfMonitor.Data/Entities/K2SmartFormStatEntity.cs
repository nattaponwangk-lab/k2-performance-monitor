using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// K2 SmartForm performance — จาก SmartForms profiler/trace logs + IIS logs
/// SourceKey = "{FormName}|{FormId}"
/// </summary>
public class K2SmartFormStatEntity : MetricEntityBase
{
    /// <summary>ชื่อ form</summary>
    [MaxLength(256)]
    public string? FormName { get; set; }

    /// <summary>form id / guid</summary>
    [MaxLength(128)]
    public string? FormId { get; set; }

    /// <summary>เวลา load ทั้ง form (ms)</summary>
    public double FormLoadMs { get; set; }

    /// <summary>เวลาที่ Initialize rule ใช้ (ms)</summary>
    public double? InitializeRuleMs { get; set; }

    /// <summary>จำนวนครั้งที่โหลดในช่วง snapshot</summary>
    public long LoadCount { get; set; }

    /// <summary>avg load time</summary>
    public double AvgLoadMs { get; set; }

    /// <summary>max load time</summary>
    public double MaxLoadMs { get; set; }

    /// <summary>ผู้ใช้ที่โหลด (สำหรับ trace)</summary>
    [MaxLength(256)]
    public string? UserName { get; set; }

    /// <summary>url ของ form</summary>
    [MaxLength(512)]
    public string? FormUrl { get; set; }
}
