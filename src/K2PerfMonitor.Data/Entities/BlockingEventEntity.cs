using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// เหตุการณ์ blocking — จาก sys.dm_tran_locks + sys.dm_os_waiting_tasks
/// SourceKey = "{blocked_session_id}|{blocking_session_id}"
/// </summary>
public class BlockingEventEntity : MetricEntityBase
{
    /// <summary>session id ที่ถูก block</summary>
    public int BlockedSessionId { get; set; }

    /// <summary>session id ที่เป็นตัว block</summary>
    public int BlockingSessionId { get; set; }

    /// <summary>เวลาที่ blocked อยู่ (ms)</summary>
    public double WaitDurationMs { get; set; }

    /// <summary>wait type (เช่น LCK_M_S)</summary>
    [Column(TypeName = "nvarchar(128)")]
    public string WaitType { get; set; } = string.Empty;

    /// <summary>resource ที่ block (เช่น KEY: 5:720575940... )</summary>
    [MaxLength(512)]
    public string? Resource { get; set; }

    /// <summary>lock mode ที่รอ (เช่น S, X)</summary>
    [MaxLength(16)]
    public string? RequestedLockMode { get; set; }

    /// <summary>SQL ของ blocked session</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? BlockedQueryText { get; set; }

    /// <summary>SQL ของ blocking session</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? BlockingQueryText { get; set; }

    /// <summary>ชื่อ host/login ของ blocked session</summary>
    [MaxLength(256)]
    public string? BlockedLoginName { get; set; }

    /// <summary>ชื่อ host/login ของ blocking session</summary>
    [MaxLength(256)]
    public string? BlockingLoginName { get; set; }
}
