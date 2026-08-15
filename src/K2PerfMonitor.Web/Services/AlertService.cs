using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Web.Services;

/// <summary>
/// อ่าน/จัดการ alert จริงจาก Monitoring DB (Phase 6 + Phase 3 acknowledge UI)
/// </summary>
public sealed class AlertService
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly InstanceFilterState _filter;
    private readonly ILogger<AlertService> _logger;

    public AlertService(IDbContextFactory<MonitorDbContext> dbFactory, InstanceFilterState filter, ILogger<AlertService> logger)
    {
        _dbFactory = dbFactory;
        _filter = filter;
        _logger = logger;
    }

    public async Task<QueryResult<Alert>> GetAlertsAsync(AlertStatus? status = null, int take = 500)
    {
        try
        {
            var instanceId = _filter.SelectedInstanceId;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var q = db.Alerts.AsNoTracking().Where(a => a.InstanceId == instanceId);
            if (status is not null)
                q = q.Where(a => a.Status == status);

            var rows = await q
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.RaisedAtUtc)
                .Take(take)
                .Select(e => new Alert
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
                })
                .ToListAsync();

            return rows.Count == 0 ? QueryResult<Alert>.Empty() : QueryResult<Alert>.Ok(rows, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot read alerts from Monitor DB");
            return QueryResult<Alert>.Error(ex.Message);
        }
    }

    /// <summary>นับ active alerts + critical (สำหรับ Overview KPI)</summary>
    public async Task<(int active, int critical)> GetActiveCountsAsync()
    {
        try
        {
            var instanceId = _filter.SelectedInstanceId;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var active = await db.Alerts.CountAsync(a => a.InstanceId == instanceId && a.Status != AlertStatus.Resolved);
            var critical = await db.Alerts.CountAsync(a => a.InstanceId == instanceId && a.Status != AlertStatus.Resolved && a.Severity == Severity.Critical);
            return (active, critical);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot count alerts");
            return (0, 0);
        }
    }

    /// <summary>Acknowledge alert (state machine: New → Acknowledged)</summary>
    public async Task AcknowledgeAsync(long alertId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var a = await db.Alerts.FirstOrDefaultAsync(x => x.Id == alertId);
        if (a is null || a.Status != AlertStatus.New) return;
        a.Status = AlertStatus.Acknowledged;
        a.AcknowledgedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
