using System.Collections.Concurrent;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// จัดการ baseline สำหรับ DMV ที่เป็นค่า "สะสม" (cumulative ตั้งแต่ server/plan start)
/// เช่น sys.dm_os_wait_stats, sys.dm_io_virtual_file_stats
///
/// หลักการ (ROADMAP §6 "DMV cumulative vs interval"):
///   Snapshot N  -  Snapshot N-1  =  Delta (ค่าในช่วง interval)
///
/// - รอบแรกหลัง worker start = เก็บ baseline เฉย ๆ (ยังไม่มี delta ให้คืน)
/// - รอบถัดไป = คืน delta เทียบกับรอบก่อน
/// - รองรับ counter reset (server restart → current &lt; previous) โดยถือ current เป็น delta
///
/// เก็บ state ใน memory ต่อ instance → collector ที่ใช้ delta ต้องลงทะเบียนเป็น Singleton
/// (restart worker = re-baseline หนึ่งรอบ ซึ่งยอมรับได้และ document ไว้)
/// </summary>
public sealed class DeltaBaseline<TRaw>
{
    private readonly ConcurrentDictionary<string, TRaw> _previous = new();
    private volatile bool _hasBaseline;

    /// <summary>เคยเก็บ baseline อย่างน้อยหนึ่งรอบแล้วหรือยัง</summary>
    public bool HasBaseline => _hasBaseline;

    /// <summary>
    /// อัปเดต snapshot ปัจจุบัน แล้วคืน delta เทียบรอบก่อนหน้า
    /// </summary>
    /// <param name="current">ค่า raw ปัจจุบัน key ตาม dedup key (เช่น wait type)</param>
    /// <param name="subtract">ฟังก์ชันคำนวณ delta = f(previous, current); ต้อง handle reset เอง</param>
    /// <returns>
    /// delta ต่อ key — เฉพาะ key ที่มี baseline รอบก่อน
    /// (รอบแรก/ key ใหม่ = ไม่รวมใน result)
    /// </returns>
    public IReadOnlyDictionary<string, TRaw> Update(
        IReadOnlyDictionary<string, TRaw> current,
        Func<TRaw, TRaw, TRaw> subtract)
    {
        var deltas = new Dictionary<string, TRaw>(current.Count);

        foreach (var (key, cur) in current)
        {
            if (_previous.TryGetValue(key, out var prev))
                deltas[key] = subtract(prev, cur);

            _previous[key] = cur;
        }

        // ล้าง key ที่หายไป (เช่น wait type ไม่โผล่แล้ว) เพื่อไม่ให้ dictionary โต
        foreach (var stale in _previous.Keys.Where(k => !current.ContainsKey(k)).ToList())
            _previous.TryRemove(stale, out _);

        _hasBaseline = true;
        return deltas;
    }
}

/// <summary>
/// Helper คำนวณ delta ของ counter สะสมแบบ pure — จัดการ reset (current &lt; previous)
/// โดยถือว่าเกิด restart/wrap แล้วคืน current เป็น delta
/// </summary>
public static class DeltaMath
{
    /// <summary>delta ของ long counter (จัดการ reset)</summary>
    public static long Diff(long previous, long current)
        => current >= previous ? current - previous : current;

    /// <summary>delta ของ double counter (จัดการ reset)</summary>
    public static double Diff(double previous, double current)
        => current >= previous ? current - previous : current;
}
