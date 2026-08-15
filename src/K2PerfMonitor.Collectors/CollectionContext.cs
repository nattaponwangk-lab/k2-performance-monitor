namespace K2PerfMonitor.Collectors;

/// <summary>
/// บริบทการเก็บข้อมูลของ "instance ปัจจุบัน" ที่ collector กำลังทำงานด้วย (scoped)
/// Worker ตั้งค่าต่อ instance ก่อนเรียก collector → collector อ่าน connection + stamp InstanceId
/// ทำให้ metric แยกตาม instance ได้ (multi-instance data isolation)
/// </summary>
public sealed class CollectionContext
{
    public long InstanceId { get; set; }
    public string InstanceName { get; set; } = "Default";
    public string ConnectionString { get; set; } = string.Empty;
}
