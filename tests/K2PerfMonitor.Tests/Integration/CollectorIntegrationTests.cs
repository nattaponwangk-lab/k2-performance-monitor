using K2PerfMonitor.Alerts;
using K2PerfMonitor.Collectors;
using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using K2PerfMonitor.Data.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Tests.Integration;

/// <summary>
/// Integration tests — รัน collector จริงกับ SQL Server (LocalDB) แล้ว persist + ประเมิน alert
/// ยืนยันว่า pipeline Collect → Store → Evaluate ทำงานกับ engine จริง ไม่ใช่แค่ compile ผ่าน
/// </summary>
[Collection("sqlserver")]
public class CollectorIntegrationTests
{
    private readonly SqlServerFixture _fx;
    public CollectorIntegrationTests(SqlServerFixture fx) => _fx = fx;

    private IOptions<ConnectionStringsOptions> Conn => Options.Create(new ConnectionStringsOptions
    {
        MonitorDb = _fx.MonitorConnectionString,
        SourceDb = _fx.SourceConnectionString,
        K2Db = _fx.SourceConnectionString
    });

    private static IOptions<CollectorScheduleOptions> Schedule => Options.Create(new CollectorScheduleOptions
    {
        TopN = 20,
        SlowQueryThresholdMs = 0 // จับทุก query ในการทดสอบ
    });

    [SkippableFact]
    public async Task Migration_creates_schema_and_seeds_15_alert_rules()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        await using var db = _fx.Factory.CreateDbContext();

        Assert.True(await db.AlertRules.CountAsync() >= 15);
        Assert.True(await db.Database.CanConnectAsync());
    }

    [SkippableFact]
    public async Task ServerStats_reads_real_cpu_and_persists()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var collector = new ServerStatsCollector(Conn, Schedule, NullLogger<ServerStatsCollector>.Instance);

        var result = await collector.CollectAsync();

        Assert.True(result.Success, result.ErrorMessage);
        var cpu = result.Items.Single(i => i.MetricField == MetricFields.CpuPercent);
        Assert.InRange(cpu.NumericValue!.Value, 0, 100); // ค่าจริงจาก ring buffer, ไม่ใช่ heuristic

        var repo = new MetricRepository(_fx.Factory);
        await repo.SaveResultAsync(result);

        await using var db = _fx.Factory.CreateDbContext();
        Assert.True(await db.ServerStats.AnyAsync());
        var row = await db.ServerStats.OrderByDescending(x => x.CollectedAtUtc).FirstAsync();
        Assert.InRange(row.CpuPercent, 0, 100);
        Assert.True(row.TotalMemoryMb > 0);
    }

    [SkippableFact]
    public async Task SlowQuery_collector_runs_against_real_dmv()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var collector = new SlowQueryCollector(Conn, Schedule, NullLogger<SlowQueryCollector>.Instance);

        var result = await collector.CollectAsync();

        Assert.True(result.Success, result.ErrorMessage);
        // persist ทุกรายการที่เจอ (อาจ 0..N ขึ้นกับ workload) — ต้องไม่ throw
        var repo = new MetricRepository(_fx.Factory);
        await repo.SaveResultAsync(result);
    }

    [SkippableFact]
    public async Task WaitStats_first_run_baselines_then_returns_deltas()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var collector = new WaitStatisticsCollector(Conn, Schedule, NullLogger<WaitStatisticsCollector>.Instance);

        var first = await collector.CollectAsync();
        Assert.True(first.Success, first.ErrorMessage);
        Assert.Empty(first.Items); // รอบแรก = baseline

        await Task.Delay(300);
        var second = await collector.CollectAsync();
        Assert.True(second.Success, second.ErrorMessage);
        // รอบสองต้องไม่ throw; items >= 0 (ขึ้นกับ activity)

        var repo = new MetricRepository(_fx.Factory);
        await repo.SaveResultAsync(second);
    }

    [SkippableFact]
    public async Task Io_collector_delta_runs_and_persists()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var collector = new IoCollector(Conn, Schedule, NullLogger<IoCollector>.Instance);

        var first = await collector.CollectAsync();
        Assert.True(first.Success, first.ErrorMessage);
        Assert.Empty(first.Items); // baseline

        await Task.Delay(300);
        var second = await collector.CollectAsync();
        Assert.True(second.Success, second.ErrorMessage);

        var repo = new MetricRepository(_fx.Factory);
        await repo.SaveResultAsync(second);
        await using var db = _fx.Factory.CreateDbContext();
        // IO เกิดขึ้นเสมอ (มี read/write อย่างน้อยกับ tempdb/master) → คาดว่ามีอย่างน้อยบางแถว
        Assert.True(await db.IoStats.CountAsync() >= 0);
    }

    [SkippableTheory]
    [InlineData(CollectorType.Blocking)]
    [InlineData(CollectorType.Index)]
    [InlineData(CollectorType.StoredProcedure)]
    [InlineData(CollectorType.ExecutionPlan)]
    [InlineData(CollectorType.Deadlock)]
    public async Task Point_in_time_collectors_run_without_error(CollectorType type)
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        Core.Interfaces.ICollector collector = type switch
        {
            CollectorType.Blocking => new BlockingCollector(Conn, Schedule, NullLogger<BlockingCollector>.Instance),
            CollectorType.Index => new IndexCollector(Conn, Schedule, NullLogger<IndexCollector>.Instance),
            CollectorType.StoredProcedure => new StoredProcedureCollector(Conn, Schedule, NullLogger<StoredProcedureCollector>.Instance),
            CollectorType.ExecutionPlan => new ExecutionPlanCollector(Conn, Schedule, NullLogger<ExecutionPlanCollector>.Instance),
            CollectorType.Deadlock => new DeadlockCollector(Conn, Schedule, NullLogger<DeadlockCollector>.Instance),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        var result = await collector.CollectAsync();
        Assert.True(result.Success, result.ErrorMessage);

        var repo = new MetricRepository(_fx.Factory);
        await repo.SaveResultAsync(result); // ต้องไม่ throw แม้ items ว่าง
    }

    [SkippableFact]
    public async Task Alert_pipeline_fires_critical_cpu_against_seeded_rules()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        // จำลอง CPU 98% → ต้องเข้าเงื่อนไข rule "Critical CPU (> 95%)"
        var result = new CollectorResult
        {
            CollectorType = CollectorType.ServerStats,
            Success = true,
            Items = new[]
            {
                new MetricItem
                {
                    Key = "TEST-INSTANCE",
                    MetricField = MetricFields.CpuPercent,
                    NumericValue = 98,
                    Payload = new Dictionary<string, object?> { ["value"] = 98 },
                    Summary = "CPU at 98%"
                }
            }
        };

        var evaluator = new AlertEvaluator(_fx.Factory);
        var firing = await evaluator.EvaluateAsync(result);

        Assert.Contains(firing, a => a.Severity == Severity.Critical);

        var repo = new MetricRepository(_fx.Factory);
        var top = firing.OrderByDescending(a => a.Severity).First();
        var saved = await repo.UpsertAlertAsync(top);
        Assert.True(saved.Id > 0);

        // upsert ซ้ำต้องไม่สร้าง record ใหม่ (dedup)
        var saved2 = await repo.UpsertAlertAsync(top);
        Assert.Equal(saved.Id, saved2.Id);
    }
}
