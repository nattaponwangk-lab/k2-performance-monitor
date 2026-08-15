using K2PerfMonitor.Collectors;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Results;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;

namespace K2PerfMonitor.Worker.Jobs;

/// <summary>
/// งานเก็บ metric หนึ่งรอบ ที่ Hangfire เรียกตาม schedule (recurring job ต่อ collector)
///
/// Multi-instance: รันต่อ "target" ทุกตัว (Default SourceDb + instance ที่ enabled ใน registry)
/// - แต่ละ target สร้าง DI scope ของตัวเอง → ตั้ง CollectionContext (connection + InstanceId) → resolve collector
/// - บันทึกผล + audit + alert แยกตาม InstanceId (data isolation)
/// - collector/instance หนึ่งล้ม ไม่กระทบตัวอื่น (แยก try/catch ต่อ target)
/// </summary>
public sealed class CollectorJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICollectionTargetProvider _targets;
    private readonly ILogger<CollectorJob> _logger;

    public CollectorJob(
        IServiceScopeFactory scopeFactory,
        ICollectionTargetProvider targets,
        ILogger<CollectorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _targets = targets;
        _logger = logger;
    }

    public async Task RunAsync(CollectorType type, CancellationToken ct = default)
    {
        var targets = await _targets.GetTargetsAsync(ct);
        foreach (var target in targets)
            await RunForTargetAsync(type, target, ct);
    }

    private async Task RunForTargetAsync(CollectorType type, CollectionTarget target, CancellationToken ct)
    {
        var runId = Guid.NewGuid().ToString("N")[..8];
        using var _ = LogContext.PushProperty("RunId", runId);
        using var __ = LogContext.PushProperty("Collector", type);
        using var ___ = LogContext.PushProperty("Instance", target.InstanceName);

        // scope ต่อ instance → CollectionContext แยกกัน (delta baseline ก็แยกตาม InstanceId ผ่าน store)
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<CollectionContext>();
        context.InstanceId = target.InstanceId;
        context.InstanceName = target.InstanceName;
        context.ConnectionString = target.ConnectionString;

        var collector = sp.GetServices<ICollector>().FirstOrDefault(c => c.Type == type);
        if (collector is null)
        {
            _logger.LogWarning("No collector registered for {Type} — skipping", type);
            return;
        }

        var repo = sp.GetRequiredService<IMetricRepository>();
        var dbFactory = sp.GetRequiredService<IDbContextFactory<MonitorDbContext>>();

        var audit = new CollectorRunEntity
        {
            CollectorType = type,
            InstanceId = target.InstanceId,
            InstanceName = target.InstanceName,
            DisplayName = collector.DisplayName,
            StartedAtUtc = DateTime.UtcNow
        };

        try
        {
            var result = await collector.CollectAsync(ct);
            audit.Success = result.Success;
            audit.ItemsCollected = result.Items.Count;
            audit.ElapsedMs = result.Elapsed.TotalMilliseconds;
            audit.ErrorMessage = result.ErrorMessage;

            if (result.Success)
            {
                await repo.SaveResultAsync(result, ct);
                _logger.LogInformation(
                    "{Collector}@{Instance} collected {Items} items in {Ms:0}ms",
                    collector.DisplayName, target.InstanceName, result.Items.Count, result.Elapsed.TotalMilliseconds);

                await sp.GetRequiredService<IRealtimePublisher>().PublishSnapshotAsync(result, ct);
                await EvaluateAlertsAsync(sp, result, ct);
            }
            else
            {
                _logger.LogWarning("{Collector}@{Instance} failed: {Error}", collector.DisplayName, target.InstanceName, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            audit.Success = false;
            audit.ErrorMessage = ex.Message;
            _logger.LogError(ex, "{Collector}@{Instance} threw an exception", collector.DisplayName, target.InstanceName);
            // ไม่ rethrow — instance อื่นต้องเก็บต่อได้ (audit บันทึก fail แล้ว)
        }
        finally
        {
            audit.FinishedAtUtc = DateTime.UtcNow;
            if (audit.ElapsedMs <= 0)
                audit.ElapsedMs = (audit.FinishedAtUtc.Value - audit.StartedAtUtc).TotalMilliseconds;

            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(CancellationToken.None);
                db.CollectorRuns.Add(audit);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist CollectorRun audit for {Collector}@{Instance}", collector.DisplayName, target.InstanceName);
            }
        }
    }

    /// <summary>ประเมิน alert (แยกตาม instance) — upsert + notify + auto-resolve เฉพาะ instance นี้</summary>
    private async Task EvaluateAlertsAsync(IServiceProvider sp, CollectorResult result, CancellationToken ct)
    {
        try
        {
            var evaluator = sp.GetRequiredService<IAlertEvaluator>();
            var repo = sp.GetRequiredService<IMetricRepository>();
            var notifier = sp.GetRequiredService<IAlertNotifier>();
            var realtime = sp.GetRequiredService<IRealtimePublisher>();

            var firing = await evaluator.EvaluateAsync(result, ct);
            foreach (var alert in firing)
            {
                var persisted = await repo.UpsertAlertAsync(alert, ct);
                await notifier.NotifyAsync(persisted, ct);
                await realtime.PublishAlertAsync(persisted, ct);
            }

            var firingKeys = firing.Select(a => a.DedupKey).ToArray();
            var resolved = await repo.ResolveMissingAsync(result.CollectorType, result.InstanceId, firingKeys, ct);

            if (firing.Count > 0 || resolved > 0)
                _logger.LogInformation("Alerts@{Instance} — {Firing} firing, {Resolved} auto-resolved",
                    result.InstanceName, firing.Count, resolved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert evaluation failed for {Collector}@{Instance}", result.CollectorType, result.InstanceName);
        }
    }
}
