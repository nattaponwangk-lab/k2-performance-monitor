using K2PerfMonitor.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// ลงทะเบียน SQL collectors ทั้งหมด + registry เข้า DI
///
/// lifetime:
/// - point-in-time collectors = Transient (ไม่มี state)
/// - delta/stateful collectors (Wait/Io/Deadlock) = Singleton (เก็บ baseline/last-seen ข้ามรอบ)
/// </summary>
public static class CollectorServiceCollectionExtensions
{
    public static IServiceCollection AddSqlCollectors(this IServiceCollection services)
    {
        // point-in-time (stateless)
        services.AddTransient<ICollector, ServerStatsCollector>();
        services.AddTransient<ICollector, SlowQueryCollector>();
        services.AddTransient<ICollector, ExecutionPlanCollector>();
        services.AddTransient<ICollector, BlockingCollector>();
        services.AddTransient<ICollector, IndexCollector>();
        services.AddTransient<ICollector, StoredProcedureCollector>();

        // stateful — must be singleton so delta baseline / last-seen survive between runs
        services.AddSingleton<ICollector, WaitStatisticsCollector>();
        services.AddSingleton<ICollector, IoCollector>();
        services.AddSingleton<ICollector, DeadlockCollector>();

        services.AddSingleton<ICollectorRegistry, CollectorRegistry>();
        return services;
    }
}
