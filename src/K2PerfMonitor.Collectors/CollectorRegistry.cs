using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// รวบรวม collector ทั้งหมดที่ลงทะเบียนใน DI แล้วจับคู่กับรอบเวลาใน <see cref="CollectorScheduleOptions"/>
/// - ตัวไหนไม่ได้ register (เช่น K2 ยังไม่ทำ) ก็จะไม่มี job → ไม่ต้องแก้ Worker ทุกครั้ง
/// </summary>
public sealed class CollectorRegistry : ICollectorRegistry
{
    public IReadOnlyList<CollectorRegistration> Registrations { get; }

    public CollectorRegistry(IEnumerable<ICollector> collectors, IOptions<CollectorScheduleOptions> schedule)
    {
        var s = schedule.Value;
        Registrations = collectors
            .Select(c => new CollectorRegistration(
                c.Type, c.DisplayName, $"collector:{c.Type}", IntervalFor(c.Type, s)))
            .OrderBy(r => r.Type)
            .ToList();
    }

    /// <summary>map collector type → interval (วินาที) จาก options</summary>
    public static int IntervalFor(CollectorType type, CollectorScheduleOptions s) => type switch
    {
        CollectorType.ServerStats => s.ServerStatsIntervalSeconds,
        CollectorType.SlowQuery => s.SlowQueryIntervalSeconds,
        CollectorType.ExecutionPlan => s.SlowQueryIntervalSeconds,
        CollectorType.WaitStatistics => s.WaitStatsIntervalSeconds,
        CollectorType.Blocking => s.BlockingIntervalSeconds,
        CollectorType.Deadlock => s.DeadlockIntervalSeconds,
        CollectorType.Index => s.IndexIntervalSeconds,
        CollectorType.Io => s.IoIntervalSeconds,
        CollectorType.StoredProcedure => s.StoredProcedureIntervalSeconds,
        CollectorType.K2Workflow => s.K2WorkflowIntervalSeconds,
        CollectorType.K2SmartForm => s.K2SmartFormIntervalSeconds,
        CollectorType.K2SmartObject => s.K2SmartObjectIntervalSeconds,
        _ => 60
    };
}
