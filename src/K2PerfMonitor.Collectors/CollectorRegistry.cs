using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// รวบรวม collector ที่ลงทะเบียนใน DI แล้วจับคู่กับรอบเวลาใน <see cref="CollectorScheduleOptions"/>
/// collectors เป็น Scoped → resolve ผ่าน scope ชั่วคราวเพื่ออ่าน Type/DisplayName (registry เป็น singleton)
/// </summary>
public sealed class CollectorRegistry : ICollectorRegistry
{
    public IReadOnlyList<CollectorRegistration> Registrations { get; }

    public CollectorRegistry(IServiceScopeFactory scopeFactory, IOptions<CollectorScheduleOptions> schedule)
    {
        var s = schedule.Value;
        using var scope = scopeFactory.CreateScope();
        var collectors = scope.ServiceProvider.GetServices<ICollector>();
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
        CollectorType.DatabaseStats => s.DatabaseStatsIntervalSeconds,
        CollectorType.K2Workflow => s.K2WorkflowIntervalSeconds,
        CollectorType.K2SmartForm => s.K2SmartFormIntervalSeconds,
        CollectorType.K2SmartObject => s.K2SmartObjectIntervalSeconds,
        _ => 60
    };
}
