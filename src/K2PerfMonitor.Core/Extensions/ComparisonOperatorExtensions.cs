using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Extensions;

/// <summary>
/// Helper สำหรับการเปรียบเทียบค่าตาม <see cref="ComparisonOperator"/>
/// ใช้ใน AlertEvaluator
/// </summary>
public static class ComparisonOperatorExtensions
{
    /// <summary>
    /// ตรวจว่า <paramref name="value"/> ผ่านเงื่อนไข <paramref name="op"/> เทียบกับ <paramref name="threshold"/>
    /// </summary>
    public static bool Matches(this ComparisonOperator op, double value, double threshold)
        => op switch
        {
            ComparisonOperator.GreaterThan => value > threshold,
            ComparisonOperator.GreaterThanOrEqual => value >= threshold,
            ComparisonOperator.LessThan => value < threshold,
            ComparisonOperator.LessThanOrEqual => value <= threshold,
            ComparisonOperator.Equals => Math.Abs(value - threshold) < double.Epsilon,
            _ => false
        };

    /// <summary>สัญลักษณ์ที่แสดงใน UI/log (เช่น "&gt;", "&gt;=")</summary>
    public static string ToSymbol(this ComparisonOperator op)
        => op switch
        {
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.GreaterThanOrEqual => ">=",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.LessThanOrEqual => "<=",
            ComparisonOperator.Equals => "=",
            _ => "?"
        };
}
