using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// Wait statistics — จาก sys.dm_os_wait_stats (delta ระหว่าง snapshot)
/// SourceKey = wait type (เช่น PAGEIOLATCH_EX, LCK_M_S)
/// </summary>
public class WaitStatEntity : MetricEntityBase
{
    /// <summary>wait type</summary>
    [Column(TypeName = "nvarchar(128)")]
    public string WaitType { get; set; } = string.Empty;

    /// <summary>จำนวน waiting tasks (delta ในช่วง snapshot)</summary>
    public long WaitingTasksCount { get; set; }

    /// <summary>total wait time ms (delta)</summary>
    public double WaitTimeMs { get; set; }

    /// <summary>signal wait time ms (เวลารอ CPU หลังได้ resource)</summary>
    public double SignalWaitTimeMs { get; set; }

    /// <summary>max wait time ms</summary>
    public double MaxWaitTimeMs { get; set; }

    /// <summary>wait % (สัดส่วนของ wait type นี้ต่อทั้งหมด)</summary>
    public double WaitPercent { get; set; }

    /// <summary>เป็น benign wait หรือไม่ (เช่น SLEEP_TASK — ไม่น่ากังวล)</summary>
    public bool IsBenign { get; set; }
}
