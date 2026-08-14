using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Core.Models;

/// <summary>
/// ชื่อ event/method บน SignalR hub (ใช้ร่วมกัน Worker client ↔ Hub ↔ browser)
/// </summary>
public static class RealtimeMessages
{
    // hub methods (Worker client → hub)
    public const string PublishSnapshot = nameof(PublishSnapshot);
    public const string PublishAlert = nameof(PublishAlert);

    // client events (hub → browser)
    public const string ReceiveSnapshot = nameof(ReceiveSnapshot);
    public const string ReceiveAlert = nameof(ReceiveAlert);
}

/// <summary>snapshot metric ที่ push แบบ real-time (เบา — เฉพาะค่าที่ dashboard ใช้)</summary>
public sealed class MetricSnapshotDto
{
    public CollectorType CollectorType { get; set; }
    public DateTime CollectedAtUtc { get; set; }
    /// <summary>ค่า metric หลัก key ตาม MetricField (เช่น CpuPercent, MemoryPercent)</summary>
    public Dictionary<string, double> Metrics { get; set; } = new();
}

/// <summary>alert ใหม่ที่ push แบบ real-time (แสดง toast/banner)</summary>
public sealed class AlertDto
{
    public long Id { get; set; }
    public CollectorType CollectorType { get; set; }
    public Severity Severity { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTime RaisedAtUtc { get; set; }

    public static AlertDto From(Alert a) => new()
    {
        Id = a.Id,
        CollectorType = a.CollectorType,
        Severity = a.Severity,
        Title = a.Title,
        Summary = a.Summary,
        RaisedAtUtc = a.RaisedAtUtc
    };
}
