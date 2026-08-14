namespace K2PerfMonitor.Data.Implementations;

/// <summary>
/// อ่านค่าจาก MetricItem.Payload (dictionary ของ object? ที่ collector สร้าง) แบบ null-safe + แปลงชนิด
/// ค่าใน payload เป็น typed object จริง (long/double/DateTime/string/bool) — helper นี้กันกรณี null/ชนิดไม่ตรง
/// </summary>
internal static class P
{
    public static string Str(IReadOnlyDictionary<string, object?> p, string key)
        => p.TryGetValue(key, out var v) && v is not null ? v.ToString() ?? "" : "";

    public static string? StrOrNull(IReadOnlyDictionary<string, object?> p, string key)
        => p.TryGetValue(key, out var v) && v is not null ? v.ToString() : null;

    public static double Dbl(IReadOnlyDictionary<string, object?> p, string key)
        => p.TryGetValue(key, out var v) && v is not null && TryDbl(v, out var d) ? d : 0;

    public static long Long(IReadOnlyDictionary<string, object?> p, string key)
        => p.TryGetValue(key, out var v) && v is not null && TryLong(v, out var l) ? l : 0;

    public static bool Bool(IReadOnlyDictionary<string, object?> p, string key)
        => p.TryGetValue(key, out var v) && v is bool b && b;

    public static DateTime? DateOrNull(IReadOnlyDictionary<string, object?> p, string key)
    {
        if (!p.TryGetValue(key, out var v) || v is null) return null;
        return v switch
        {
            DateTime dt => dt,
            _ => DateTime.TryParse(v.ToString(), out var parsed) ? parsed : null
        };
    }

    private static bool TryDbl(object v, out double d)
    {
        try { d = Convert.ToDouble(v); return true; }
        catch { d = 0; return false; }
    }

    private static bool TryLong(object v, out long l)
    {
        try { l = Convert.ToInt64(v); return true; }
        catch { l = 0; return false; }
    }
}
