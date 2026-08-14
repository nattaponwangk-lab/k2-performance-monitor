using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// ServerStats แบบ aggregate (rollup) สำหรับกราฟย้อนหลังระยะยาว โดยไม่ต้องอ่าน raw ทั้งหมด
/// - BucketMinutes = 5 หรือ 60 (raw 1m/5m → 5m/1h)
/// - upsert แบบ idempotent ด้วย key (BucketStartUtc, BucketMinutes)
/// </summary>
[Index(nameof(BucketMinutes), nameof(BucketStartUtc), IsUnique = true)]
public class ServerStatRollupEntity
{
    [Key] public long Id { get; set; }

    /// <summary>ต้น bucket (UTC, ปัดตาม BucketMinutes)</summary>
    public DateTime BucketStartUtc { get; set; }

    /// <summary>ขนาด bucket (นาที): 5 หรือ 60</summary>
    public int BucketMinutes { get; set; }

    public double AvgCpuPercent { get; set; }
    public double MaxCpuPercent { get; set; }
    public double AvgMemoryPercent { get; set; }
    public double MaxMemoryPercent { get; set; }
    public double AvgConnectionCount { get; set; }
    public int MaxConnectionCount { get; set; }
    public double AvgBatchRequestsPerSec { get; set; }

    /// <summary>จำนวน raw sample ใน bucket</summary>
    public int SampleCount { get; set; }
}
