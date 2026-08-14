namespace K2PerfMonitor.Core.Options;

/// <summary>
/// การตั้งค่า Email (SMTP)
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Notifications:Email";

    public bool Enabled { get; set; } = false;

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;

    /// <summary>ใช้ SSL/TLS</summary>
    public bool UseSsl { get; set; } = false;

    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>ที่อยู่ผู้ส่ง</summary>
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "K2 Performance Monitor";

    /// <summary>ผู้รับ (คั่นด้วย ; หรือ ,)</summary>
    public string ToAddresses { get; set; } = string.Empty;
}
