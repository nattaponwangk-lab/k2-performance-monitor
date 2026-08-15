namespace K2PerfMonitor.Core.Interfaces;

/// <summary>เป้าหมายการเก็บข้อมูลหนึ่ง instance (InstanceId + ชื่อ + connection string ที่ถอดรหัสแล้ว)</summary>
/// <param name="InstanceId">0 = Default (SourceDb ที่ config)</param>
public readonly record struct CollectionTarget(long InstanceId, string InstanceName, string ConnectionString);

/// <summary>
/// คืนรายการ instance ที่ต้อง collect ในรอบนี้ — Default (SourceDb ที่ config) + instance ที่ enabled ใน registry
/// (decrypt connection string ด้วย Data Protection)
/// </summary>
public interface ICollectionTargetProvider
{
    Task<IReadOnlyList<CollectionTarget>> GetTargetsAsync(CancellationToken ct = default);
}
