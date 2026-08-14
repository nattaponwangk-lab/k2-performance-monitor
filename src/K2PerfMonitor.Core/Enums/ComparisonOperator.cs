namespace K2PerfMonitor.Core.Enums;

/// <summary>
/// ตัวเปรียบเทียบสำหรับ alert rule (เทียบค่า metric กับ threshold)
/// </summary>
public enum ComparisonOperator
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Equals
}
