using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Data;

/// <summary>
/// EF Core DbContext สำหรับ Monitoring database (K2PerfMonitor)
/// - metric tables: ดึงตาม collector type, มี index บน CollectedAtUtc (retention + trend query)
/// - alerts/rules: lifecycle + dedup
/// </summary>
public class MonitorDbContext : DbContext
{
    public MonitorDbContext(DbContextOptions<MonitorDbContext> options) : base(options) { }

    // Metric tables (per collector)
    public DbSet<SlowQueryEntity> SlowQueries => Set<SlowQueryEntity>();
    public DbSet<ExecutionPlanEntity> ExecutionPlans => Set<ExecutionPlanEntity>();
    public DbSet<WaitStatEntity> WaitStats => Set<WaitStatEntity>();
    public DbSet<BlockingEventEntity> BlockingEvents => Set<BlockingEventEntity>();
    public DbSet<DeadlockEventEntity> DeadlockEvents => Set<DeadlockEventEntity>();
    public DbSet<IndexRecommendationEntity> IndexRecommendations => Set<IndexRecommendationEntity>();
    public DbSet<IoStatEntity> IoStats => Set<IoStatEntity>();
    public DbSet<ServerStatEntity> ServerStats => Set<ServerStatEntity>();
    public DbSet<ServerStatRollupEntity> ServerStatRollups => Set<ServerStatRollupEntity>();
    public DbSet<StoredProcedureStatEntity> StoredProcedureStats => Set<StoredProcedureStatEntity>();
    public DbSet<K2WorkflowStatEntity> K2WorkflowStats => Set<K2WorkflowStatEntity>();
    public DbSet<K2SmartFormStatEntity> K2SmartFormStats => Set<K2SmartFormStatEntity>();
    public DbSet<K2SmartObjectStatEntity> K2SmartObjectStats => Set<K2SmartObjectStatEntity>();

    // Alerting + system
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();
    public DbSet<AlertRuleEntity> AlertRules => Set<AlertRuleEntity>();
    public DbSet<CollectorRunEntity> CollectorRuns => Set<CollectorRunEntity>();

    // Auth + multi-instance
    public DbSet<AppUserEntity> Users => Set<AppUserEntity>();
    public DbSet<MonitoredInstanceEntity> MonitoredInstances => Set<MonitoredInstanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Metric tables: index บน CollectedAtUtc (สำหรับ retention + trend) ----
        ConfigureMetricTable(modelBuilder.Entity<SlowQueryEntity>(), "SlowQueries", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.SourceKey }));

        ConfigureMetricTable(modelBuilder.Entity<ExecutionPlanEntity>(), "ExecutionPlans", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.QueryHash }));

        ConfigureMetricTable(modelBuilder.Entity<WaitStatEntity>(), "WaitStats", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.WaitType }));

        ConfigureMetricTable(modelBuilder.Entity<BlockingEventEntity>(), "BlockingEvents", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.BlockedSessionId }));

        ConfigureMetricTable(modelBuilder.Entity<DeadlockEventEntity>(), "DeadlockEvents", e => e
            .HasIndex(x => x.DeadlockAtUtc));

        ConfigureMetricTable(modelBuilder.Entity<IndexRecommendationEntity>(), "IndexRecommendations", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.RecommendationType }));

        ConfigureMetricTable(modelBuilder.Entity<IoStatEntity>(), "IoStats", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.DatabaseName }));

        ConfigureMetricTable(modelBuilder.Entity<ServerStatEntity>(), "ServerStats", e => e
            .HasIndex(x => x.CollectedAtUtc));

        ConfigureMetricTable(modelBuilder.Entity<StoredProcedureStatEntity>(), "StoredProcedureStats", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.ObjectName }));

        ConfigureMetricTable(modelBuilder.Entity<K2WorkflowStatEntity>(), "K2WorkflowStats", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.Status }));

        ConfigureMetricTable(modelBuilder.Entity<K2SmartFormStatEntity>(), "K2SmartFormStats", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.FormName }));

        ConfigureMetricTable(modelBuilder.Entity<K2SmartObjectStatEntity>(), "K2SmartObjectStats", e => e
            .HasIndex(x => new { x.CollectedAtUtc, x.SmartObjectName }));

        // ---- Alerts ----
        modelBuilder.Entity<AlertEntity>(e =>
        {
            e.ToTable("Alerts");
            e.HasIndex(x => new { x.DedupKey, x.Status });
            e.HasIndex(x => x.RaisedAtUtc);
            e.HasIndex(x => x.Status);
            // enum stored as int
            e.Property(x => x.Severity).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CollectorType).HasConversion<int>();
        });

        // ---- Alert rules ----
        modelBuilder.Entity<AlertRuleEntity>(e =>
        {
            e.ToTable("AlertRules");
            e.HasIndex(x => new { x.CollectorType, x.Enabled });
            e.Property(x => x.Severity).HasConversion<int>();
            e.Property(x => x.Operator).HasConversion<int>();
            e.Property(x => x.CollectorType).HasConversion<int>();
            e.Property(x => x.Channels).HasConversion<int>();
        });

        // ---- Collector runs ----
        modelBuilder.Entity<CollectorRunEntity>(e =>
        {
            e.ToTable("CollectorRuns");
            e.HasIndex(x => x.StartedAtUtc);
            e.Property(x => x.CollectorType).HasConversion<int>();
        });

        // Seed default alert rules (Phase 4 จะเพิ่มเติม)
        AlertRuleSeed.Apply(modelBuilder);
    }

    /// <summary>
    /// Helper ตั้งค่า metric table มาตรฐาน: table name + enum conversions + callback สำหรับ index เพิ่ม
    /// </summary>
    private static void ConfigureMetricTable<TEntity>(
        EntityTypeBuilder<TEntity> entity,
        string tableName,
        Action<EntityTypeBuilder<TEntity>> extra)
        where TEntity : MetricEntityBase
    {
        entity.ToTable(tableName);
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.CollectedAtUtc); // สำหรับ retention purge + trend query
        extra(entity);
    }
}
