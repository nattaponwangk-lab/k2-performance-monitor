namespace K2PerfMonitor.Core.Options;

/// <summary>
/// การตั้งค่า SignalR Hub
/// Worker ใช้เป็น client ส่ง metrics แบบ real-time ไปยัง Hub ของ Web
/// </summary>
public sealed class SignalROptions
{
    public const string SectionName = "SignalR";

    /// <summary>URL ของ SignalR Hub บน Web (เช่น http://localhost:5000/hubs/monitor)</summary>
    public string HubUrl { get; set; } = string.Empty;

    /// <summary>เปิดใช้งานการ push แบบ real-time (ถ้าปิด จะเก็บข้อมูล DB เท่านั้น)</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>หน่วงเวลาลอง reconnect ครั้งแรก (ms)</summary>
    public int ReconnectInitialDelayMs { get; set; } = 2000;
}
