using K2PerfMonitor.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// ลงทะเบียน SQL collectors ทั้งหมด + registry + multi-instance infrastructure เข้า DI
///
/// lifetime (multi-instance):
/// - collectors = Scoped (อ่าน CollectionContext ของ instance ปัจจุบัน ต่อ scope)
/// - CollectionContext = Scoped (Worker ตั้งค่าต่อ instance ก่อนเรียก collector)
/// - DeltaBaselineStore / DeadlockCursorStore = Singleton (state ข้ามรอบ แยกตาม instanceId)
/// </summary>
public static class CollectorServiceCollectionExtensions
{
    public static IServiceCollection AddSqlCollectors(this IServiceCollection services)
    {
        // per-instance context + shared stateful stores
        services.AddScoped<CollectionContext>();
        services.AddSingleton<DeltaBaselineStore>();
        services.AddSingleton<DeadlockCursorStore>();

        services.AddScoped<ICollector, ServerStatsCollector>();
        services.AddScoped<ICollector, SlowQueryCollector>();
        services.AddScoped<ICollector, ExecutionPlanCollector>();
        services.AddScoped<ICollector, BlockingCollector>();
        services.AddScoped<ICollector, IndexCollector>();
        services.AddScoped<ICollector, StoredProcedureCollector>();
        services.AddScoped<ICollector, WaitStatisticsCollector>();
        services.AddScoped<ICollector, IoCollector>();
        services.AddScoped<ICollector, DeadlockCollector>();
        services.AddScoped<ICollector, DatabaseStatsCollector>();

        services.AddSingleton<ICollectorRegistry, CollectorRegistry>();
        return services;
    }
}
