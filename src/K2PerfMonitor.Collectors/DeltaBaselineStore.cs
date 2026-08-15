using System.Collections.Concurrent;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// เก็บ <see cref="DeltaBaseline{TRaw}"/> แยกตาม key (เช่น "Wait:{instanceId}") แบบ singleton
/// → delta collector เป็น scoped ได้ แต่ baseline คงอยู่ข้ามรอบ และ **แยกตาม instance** (ไม่ปน)
/// </summary>
public sealed class DeltaBaselineStore
{
    private readonly ConcurrentDictionary<string, object> _baselines = new();

    public DeltaBaseline<TRaw> Get<TRaw>(string key)
        => (DeltaBaseline<TRaw>)_baselines.GetOrAdd(key, _ => new DeltaBaseline<TRaw>());
}

/// <summary>
/// เก็บ "เวลา deadlock ล่าสุดที่เคยเห็น" แยกตาม instance (singleton) — ให้ DeadlockCollector เป็น scoped ได้
/// </summary>
public sealed class DeadlockCursorStore
{
    private readonly ConcurrentDictionary<long, DateTime> _lastSeen = new();

    public DateTime Get(long instanceId) => _lastSeen.TryGetValue(instanceId, out var v) ? v : DateTime.MinValue;

    public void Advance(long instanceId, DateTime seen)
        => _lastSeen.AddOrUpdate(instanceId, seen, (_, cur) => seen > cur ? seen : cur);
}
