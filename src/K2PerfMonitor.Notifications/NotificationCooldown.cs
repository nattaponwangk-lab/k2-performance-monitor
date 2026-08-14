namespace K2PerfMonitor.Notifications;

/// <summary>
/// ตรรกะ cooldown แบบ pure — ตัดสินว่าควรแจ้งซ้ำหรือยัง
/// </summary>
public static class NotificationCooldown
{
    /// <summary>
    /// ควรแจ้งเตือนหรือไม่: แจ้งได้ถ้ายังไม่เคยแจ้ง, หรือ cooldown = 0,
    /// หรือเวลาที่ผ่านไปตั้งแต่แจ้งครั้งล่าสุด ≥ cooldown
    /// </summary>
    public static bool ShouldNotify(DateTime? lastNotifiedUtc, int cooldownMinutes, DateTime nowUtc)
    {
        if (lastNotifiedUtc is null) return true;
        if (cooldownMinutes <= 0) return true;
        return nowUtc - lastNotifiedUtc.Value >= TimeSpan.FromMinutes(cooldownMinutes);
    }
}
