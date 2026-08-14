namespace K2PerfMonitor.Core.Options;

/// <summary>
/// การตั้งค่า LINE Notify
/// หมายเหตุ: LINE Notify กำลังถูกปลดระวาง ในอนาคตให้ย้ายไป LINE Messaging API
/// (ดู docs/notifications-setup.md)
/// </summary>
public sealed class LineOptions
{
    public const string SectionName = "Notifications:Line";

    public bool Enabled { get; set; } = false;

    /// <summary>LINE Notify token (1 token = 1 กลุ่ม/ห้อง)</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>API endpoint (เปลี่ยนได้ถ้าใช้ proxy)</summary>
    public string ApiUrl { get; set; } = "https://notify-api.line.me/api/notify";
}
