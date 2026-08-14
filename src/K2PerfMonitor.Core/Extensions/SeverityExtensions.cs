using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Extensions;

/// <summary>
/// Helper สำหรับ <see cref="Severity"/>
/// </summary>
public static class SeverityExtensions
{
    /// <summary>สี hex สำหรับ Teams/Email (Critical=แดง, Warning=ส้ม, Info=เทา)</summary>
    public static string ToHexColor(this Severity severity)
        => severity switch
        {
            Severity.Critical => "#D32F2F",   // red
            Severity.Warning => "#F57C00",     // orange
            Severity.Info => "#607D8B",        // blue-grey
            _ => "#607D8B"
        };

    /// <summary>emoji สำหรับ notification</summary>
    public static string ToEmoji(this Severity severity)
        => severity switch
        {
            Severity.Critical => "🔴",
            Severity.Warning => "🟠",
            Severity.Info => "🔵",
            _ => "⚪"
        };

    /// <summary>ป้ายข้อความ</summary>
    public static string ToLabel(this Severity severity)
        => severity switch
        {
            Severity.Critical => "CRITICAL",
            Severity.Warning => "WARNING",
            Severity.Info => "INFO",
            _ => severity.ToString().ToUpperInvariant()
        };
}
