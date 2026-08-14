using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Results;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace K2PerfMonitor.Worker.Jobs;

/// <summary>
/// งานเก็บ metric หนึ่งรอบ ที่ Hangfire เรียกตาม schedule (recurring job ต่อ collector)
/// - resolve collector ตาม type → รัน → บันทึกผล (ผ่าน repo) + audit ลง CollectorRuns เสมอ
/// - ผูก correlation id (RunId) เข้า Serilog log context ทุกครั้ง เพื่อไล่ log ต่อรอบได้
/// - ถ้า collector โยน exception จะบันทึก audit เป็น fail แล้ว rethrow ให้ Hangfire retry
/// </summary>
public sealed class CollectorJob
{
    private readonly IEnumerable<ICollector> _collectors;
    private readonly IMetricRepository _repo;
    private readonly IAlertEvaluator _alertEvaluator;
    private readonly IAlertNotifier _alertNotifier;
    private readonly IRealtimePublisher _realtime;
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly ILogger<CollectorJob> _logger;

    public CollectorJob(
        IEnumerable<ICollector> collectors,
        IMetricRepository repo,
        IAlertEvaluator alertEvaluator,
        IAlertNotifier alertNotifier,
        IRealtimePublisher realtime,
        IDbContextFactory<MonitorDbContext> dbFactory,
        ILogger<CollectorJob> logger)
    {
        _collectors = collectors;
        _repo = repo;
        _alertEvaluator = alertEvaluator;
        _alertNotifier = alertNotifier;
        _realtime = realtime;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task RunAsync(CollectorType type, CancellationToken ct = default)
    {
        var runId = Guid.NewGuid().ToString("N")[..8];
        using var _ = LogContext.PushProperty("RunId", runId);
        using var __ = LogContext.PushProperty("Collector", type);

        var collector = _collectors.FirstOrDefault(c => c.Type == type);
        if (collector is null)
        {
            _logger.LogWarning("No collector registered for {Type} — skipping", type);
            return;
        }

        var audit = new CollectorRunEntity
        {
            CollectorType = type,
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
                await _repo.SaveResultAsync(result, ct);
                _logger.LogInformation(
                    "{Collector} collected {Items} items in {Ms:0}ms",
                    collector.DisplayName, result.Items.Count, result.Elapsed.TotalMilliseconds);

                await _realtime.PublishSnapshotAsync(result, ct); // best-effort live push
                await EvaluateAlertsAsync(result, ct);
            }
            else
            {
                _logger.LogWarning("{Collector} failed: {Error}", collector.DisplayName, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            audit.Success = false;
            audit.ErrorMessage = ex.Message;
            _logger.LogError(ex, "{Collector} threw an exception", collector.DisplayName);
            throw; // ให้ Hangfire retry ตาม policy
        }
        finally
        {
            audit.FinishedAtUtc = DateTime.UtcNow;
            if (audit.ElapsedMs <= 0)
                audit.ElapsedMs = (audit.FinishedAtUtc.Value - audit.StartedAtUtc).TotalMilliseconds;

            // เขียน audit เสมอ (แม้ fail) — ใช้ CancellationToken.None กัน audit หายเมื่อถูก cancel
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(CancellationToken.None);
                db.CollectorRuns.Add(audit);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist CollectorRun audit for {Collector}", collector.DisplayName);
            }
        }
    }

    /// <summary>
    /// ประเมิน alert rules กับผลลัพธ์ → upsert alert ที่ละเมิด + auto-resolve ตัวที่กลับปกติ
    /// (แยก try/catch: ถ้า alert engine พังต้องไม่ทำให้ collection รอบนี้ล้ม)
    /// </summary>
    private async Task EvaluateAlertsAsync(CollectorResult result, CancellationToken ct)
    {
        try
        {
            var firing = await _alertEvaluator.EvaluateAsync(result, ct);
            foreach (var alert in firing)
            {
                var persisted = await _repo.UpsertAlertAsync(alert, ct);
                await _alertNotifier.NotifyAsync(persisted, ct);
                await _realtime.PublishAlertAsync(persisted, ct); // live toast/banner
            }

            var firingKeys = firing.Select(a => a.DedupKey).ToArray();
            var resolved = await _repo.ResolveMissingAsync(result.CollectorType, firingKeys, ct);

            if (firing.Count > 0 || resolved > 0)
                _logger.LogInformation("Alerts — {Firing} firing, {Resolved} auto-resolved", firing.Count, resolved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert evaluation failed for {Collector}", result.CollectorType);
        }
    }
}
