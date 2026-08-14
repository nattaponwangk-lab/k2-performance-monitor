using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Results;
using K2PerfMonitor.Data.Entities;

namespace K2PerfMonitor.Data.Implementations;

/// <summary>
/// EF Core implementation ของ IMetricRepository
/// - บันทึก CollectorResult ลงตาราง metric ที่เกี่ยวข้อง (dispatch ตาม CollectorType)
/// - มี query helpers สำหรับ Web อ่านข้อมูลล่าสุด + history
///
/// ใช้ IDbContextFactory เพื่อคุม lifetime ของ DbContext เอง — ปลอดภัยเมื่อถูกเรียก
/// จาก Hangfire job scope หรือ singleton (ไม่พึ่ง scoped DbContext ที่ AddDbContextFactory ไม่ได้ลงทะเบียนให้)
/// </summary>
public class MetricRepository : IMetricRepository
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;

    public MetricRepository(IDbContextFactory<MonitorDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task SaveResultAsync(CollectorResult result, CancellationToken cancellationToken = default)
    {
        if (!result.Success) return;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        switch (result.CollectorType)
        {
            case CollectorType.ServerStats:
                SaveServerStats(db, result);
                break;
            // Collectors อื่นๆ จะเพิ่มในรอบถัดไป
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// บันทึก ServerStats snapshot — ในรอบนี้เก็บ 1 แถวต่อ snapshot (สรุประดับ server)
    /// </summary>
    private static void SaveServerStats(MonitorDbContext db, CollectorResult result)
    {
        // อ่าน metric values จาก items (key by MetricField)
        var items = result.Items.ToDictionary(i => i.MetricField ?? "", i => i);
        var payload = result.Items.FirstOrDefault()?.Payload ?? new Dictionary<string, object?>();

        var entity = new ServerStatEntity
        {
            CollectedAtUtc = result.CollectedAtUtc,
            SourceKey = "Server",
            InstanceName = GetStr(payload, "InstanceName"),
            UptimeSeconds = GetLong(payload, "UptimeSeconds"),
            CpuPercent = GetDouble(items, MetricFields.CpuPercent),
            MemoryPercent = GetDouble(items, MetricFields.MemoryPercent),
            UsedMemoryMb = GetDouble(items, MetricFields.AvailableMemoryMb) > 0
                ? GetDouble(payload, "TotalMemoryMb") - GetDouble(items, MetricFields.AvailableMemoryMb)
                : 0,
            AvailableMemoryMb = GetDouble(items, MetricFields.AvailableMemoryMb),
            TotalMemoryMb = GetDouble(payload, "TotalMemoryMb"),
            ConnectionCount = (int)GetDouble(items, MetricFields.ConnectionCount),
            ActiveRequestCount = (int)GetDouble(payload, "ActiveRequestCount"),
            BatchRequestsPerSec = GetDouble(items, MetricFields.BatchRequestsPerSec),
            OnlineSchedulerCount = (int)GetDouble(payload, "OnlineSchedulerCount"),
            BlockedProcessCount = (int)GetDouble(items, MetricFields.BlockedProcessCount),
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(payload)
        };

        db.ServerStats.Add(entity);
    }

    // ============== Query helpers สำหรับ Web ==============

    /// <summary>ดึง ServerStats ล่าสุด 1 แถว</summary>
    public async Task<ServerStatEntity?> GetLatestServerStatsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ServerStats
            .AsNoTracking()
            .OrderByDescending(x => x.CollectedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>ดึง history ย้อนหลัง N จุด (สำหรับ trend chart)</summary>
    public async Task<List<ServerStatEntity>> GetServerStatsHistoryAsync(int points, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ServerStats
            .AsNoTracking()
            .OrderByDescending(x => x.CollectedAtUtc)
            .Take(points)
            .ToListAsync(ct);
    }

    // ============== Alerts ==============

    /// <summary>
    /// บันทึก alert — dedup ด้วย DedupKey: ถ้ามี alert ที่ยัง active (ไม่ Resolved) อยู่แล้ว
    /// จะอัปเดตค่าล่าสุด + escalate severity (ไม่สร้างซ้ำ) มิฉะนั้น insert ใหม่เป็น New
    /// </summary>
    public async Task<Alert> UpsertAlertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Alerts
            .Where(a => a.DedupKey == alert.DedupKey && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.RaisedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            var entity = ToEntity(alert);
            db.Alerts.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return ToModel(entity);
        }

        // ยังละเมิดอยู่ — อัปเดตค่าล่าสุด + escalate severity ถ้าสูงขึ้น
        existing.MetricValue = alert.MetricValue;
        existing.ThresholdValue = alert.ThresholdValue;
        existing.Summary = alert.Summary;
        existing.Detail = alert.Detail;
        if (alert.Severity > existing.Severity)
            existing.Severity = alert.Severity;
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(existing);
    }

    public async Task MarkAlertNotifiedAsync(long alertId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Alerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (entity is null) return;
        entity.LastNotifiedAtUtc = DateTime.UtcNow;
        entity.NotifyCount++;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Alerts
            .AsNoTracking()
            .Where(a => a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.RaisedAtUtc)
            .ToListAsync(cancellationToken);
        return rows.Select(ToModel).ToList();
    }

    public async Task<int> ResolveMissingAsync(
        CollectorType collectorType,
        IReadOnlyCollection<string> stillFiringDedupKeys,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var keys = stillFiringDedupKeys as ISet<string> ?? stillFiringDedupKeys.ToHashSet();

        var stale = await db.Alerts
            .Where(a => a.CollectorType == collectorType
                        && a.Status != AlertStatus.Resolved
                        && !keys.Contains(a.DedupKey))
            .ToListAsync(cancellationToken);

        foreach (var a in stale)
        {
            a.Status = AlertStatus.Resolved;
            a.ResolvedAtUtc = now;
        }

        if (stale.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    public async Task<int> PurgeOldDataAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        if (retentionDays <= 0) return 0;
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var total = 0;
        total += await db.ServerStats.Where(x => x.CollectedAtUtc < cutoff).ExecuteDeleteAsync(cancellationToken);
        total += await db.CollectorRuns.Where(x => x.StartedAtUtc < cutoff).ExecuteDeleteAsync(cancellationToken);
        total += await db.Alerts
            .Where(x => x.Status == AlertStatus.Resolved && x.ResolvedAtUtc != null && x.ResolvedAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        return total;
    }

    // ============== Alert entity <-> model mapping ==============
    private static AlertEntity ToEntity(Alert a) => new()
    {
        RuleId = a.RuleId,
        CollectorType = a.CollectorType,
        DedupKey = a.DedupKey,
        Severity = a.Severity,
        Title = a.Title,
        Summary = a.Summary,
        Detail = a.Detail,
        MetricValue = a.MetricValue,
        ThresholdValue = a.ThresholdValue,
        Status = a.Status,
        RaisedAtUtc = a.RaisedAtUtc,
        AcknowledgedAtUtc = a.AcknowledgedAtUtc,
        ResolvedAtUtc = a.ResolvedAtUtc,
        LastNotifiedAtUtc = a.LastNotifiedAtUtc,
        NotifyCount = a.NotifyCount
    };

    private static Alert ToModel(AlertEntity e) => new()
    {
        Id = e.Id,
        RuleId = e.RuleId,
        CollectorType = e.CollectorType,
        DedupKey = e.DedupKey,
        Severity = e.Severity,
        Title = e.Title,
        Summary = e.Summary,
        Detail = e.Detail,
        MetricValue = e.MetricValue,
        ThresholdValue = e.ThresholdValue,
        Status = e.Status,
        RaisedAtUtc = e.RaisedAtUtc,
        AcknowledgedAtUtc = e.AcknowledgedAtUtc,
        ResolvedAtUtc = e.ResolvedAtUtc,
        LastNotifiedAtUtc = e.LastNotifiedAtUtc,
        NotifyCount = e.NotifyCount
    };

    // ============== payload helpers ==============
    private static double GetDouble(IReadOnlyDictionary<string, MetricItem> items, string field)
        => items.TryGetValue(field, out var it) && it.NumericValue.HasValue ? it.NumericValue.Value : 0;
    private static double GetDouble(IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var v) && v != null && double.TryParse(v.ToString(), out var d) ? d : 0;
    private static long GetLong(IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var v) && v != null && long.TryParse(v.ToString(), out var l) ? l : 0;
    private static string GetStr(IReadOnlyDictionary<string, object?> payload, string key)
        => payload.TryGetValue(key, out var v) && v != null ? v.ToString() ?? "" : "";
}
