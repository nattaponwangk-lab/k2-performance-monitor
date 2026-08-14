namespace K2PerfMonitor.Core.Enums;

/// <summary>
/// บทบาทผู้ใช้ (RBAC)
/// - Admin: จัดการ Settings / Alert rules / Instances / Users
/// - Operator: ดู monitoring + ทำ operational action (acknowledge alert)
/// - Viewer: อ่านอย่างเดียว
/// </summary>
public enum UserRole
{
    Viewer = 0,
    Operator = 1,
    Admin = 2
}

/// <summary>ประเภท instance ที่ monitor</summary>
public enum InstanceType
{
    Sql = 0,
    K2 = 1
}
