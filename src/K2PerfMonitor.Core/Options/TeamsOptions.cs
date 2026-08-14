namespace K2PerfMonitor.Core.Options;

/// <summary>
/// การตั้งค่า Microsoft Teams Incoming Webhook
/// </summary>
public sealed class TeamsOptions
{
    public const string SectionName = "Notifications:Teams";

    public bool Enabled { get; set; } = false;

    /// <summary>Teams channel webhook URL</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>URL ของ dashboard สำหรับปุ่ม "View in Dashboard" ใน Adaptive Card</summary>
    public string DashboardUrl { get; set; } = string.Empty;
}
