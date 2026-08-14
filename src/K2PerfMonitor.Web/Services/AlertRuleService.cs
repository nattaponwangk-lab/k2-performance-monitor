using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Web.Services;

/// <summary>อ่าน/จัดการ AlertRule จริงจาก DB (หน้า Settings — Admin)</summary>
public sealed class AlertRuleService
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    public AlertRuleService(IDbContextFactory<MonitorDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<AlertRuleEntity>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AlertRules.AsNoTracking().OrderBy(r => r.CollectorType).ThenBy(r => r.Threshold).ToListAsync();
    }

    public async Task SetEnabledAsync(long id, bool enabled)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var r = await db.AlertRules.FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return;
        r.Enabled = enabled;
        await db.SaveChangesAsync();
    }

    public async Task UpdateThresholdAsync(long id, double threshold)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var r = await db.AlertRules.FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return;
        r.Threshold = threshold;
        await db.SaveChangesAsync();
    }
}
