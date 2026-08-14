namespace K2PerfMonitor.Core.Enums;

/// <summary>
/// ระดับความรุนแรงของ alert และ metric
/// ค่าตัวเลขใช้เปรียบเทียบระดับ (Critical > Warning > Info)
/// </summary>
public enum Severity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
