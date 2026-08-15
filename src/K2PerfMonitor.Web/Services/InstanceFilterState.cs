namespace K2PerfMonitor.Web.Services;

/// <summary>
/// สถานะ "instance ที่เลือกดู" (scoped ต่อ circuit) — query service อ่านค่านี้เพื่อ filter metric ตาม InstanceId
/// ทำให้ dashboard เลือกดูรายเครื่องได้ (multi-instance data isolation) โดยไม่ปนข้อมูลข้าม server
/// </summary>
public sealed class InstanceFilterState
{
    /// <summary>InstanceId ที่เลือก (0 = Default)</summary>
    public long SelectedInstanceId { get; set; } = 0;

    /// <summary>รายการ instance ที่มีข้อมูล (โหลดครั้งแรกใน layout)</summary>
    public IReadOnlyList<InstanceOption> Instances { get; set; } = new List<InstanceOption>();
}

public readonly record struct InstanceOption(long InstanceId, string InstanceName);
