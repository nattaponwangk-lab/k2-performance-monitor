namespace K2PerfMonitor.Core.Enums;

/// <summary>
/// ประเภทของ collector ทั้ง 12 ตัว ใช้ระบุว่า metric/alert/collector นี้เกี่ยวกับอะไร
/// </summary>
public enum CollectorType
{
    SlowQuery = 1,
    ExecutionPlan = 2,
    WaitStatistics = 3,
    Blocking = 4,
    Deadlock = 5,
    Index = 6,
    Io = 7,
    ServerStats = 8,
    StoredProcedure = 9,
    K2Workflow = 10,
    K2SmartForm = 11,
    K2SmartObject = 12
}
