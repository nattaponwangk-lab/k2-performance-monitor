namespace K2PerfMonitor.Worker.Jobs;

/// <summary>
/// สร้าง cron expression สำหรับ Hangfire recurring job จากช่วงเวลาเป็น "วินาที"
/// - &lt; 60s : ใช้ cron 6-field (มีวินาที) เช่น 15s → "*/15 * * * * *"
///   (ต้องตั้ง SchedulePollingInterval ของ Hangfire server ให้ต่ำกว่า interval มิฉะนั้นจะไม่ยิงตามจริง)
/// - &gt;= 60s: ใช้ cron ระดับนาที เช่น 300s → "*/5 * * * *"
/// - &gt;= 3600s: ใช้ cron ระดับชั่วโมง
///
/// ค่า step จะถูกปรับให้ "หารลงตัว" กับ 60 เสมอ (Cronos ต้องการ step ที่ลงตัวเพื่อกระจายสม่ำเสมอ)
/// </summary>
public static class CronExpr
{
    public static string FromSeconds(int seconds)
    {
        if (seconds < 1) seconds = 1;

        if (seconds < 60)
        {
            var step = NearestDivisor(seconds, 60);
            return $"*/{step} * * * * *";
        }

        var minutes = seconds / 60;
        if (minutes < 60)
        {
            var step = NearestDivisor(minutes, 60);
            return $"*/{step} * * * *";
        }

        var hours = Math.Clamp(minutes / 60, 1, 23);
        return $"0 0 */{hours} * * *";
    }

    /// <summary>หา divisor ของ mod ที่ใกล้ค่า value ที่สุด (เพื่อให้ step กระจายสม่ำเสมอ)</summary>
    private static int NearestDivisor(int value, int mod)
    {
        value = Math.Clamp(value, 1, mod);
        for (var delta = 0; delta < mod; delta++)
        {
            var up = value + delta;
            if (up <= mod && mod % up == 0) return up;
            var down = value - delta;
            if (down >= 1 && mod % down == 0) return down;
        }
        return 1;
    }
}
