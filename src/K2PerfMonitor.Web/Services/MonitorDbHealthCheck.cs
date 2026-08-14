using K2PerfMonitor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace K2PerfMonitor.Web.Services;

/// <summary>
/// Health check — ตรวจว่าต่อ Monitoring DB ได้จริง (ใช้กับ /health)
/// </summary>
public sealed class MonitorDbHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;

    public MonitorDbHealthCheck(IDbContextFactory<MonitorDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var ok = await db.Database.CanConnectAsync(cancellationToken);
            return ok
                ? HealthCheckResult.Healthy("Monitoring DB reachable")
                : HealthCheckResult.Unhealthy("Monitoring DB not reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Monitoring DB check failed", ex);
        }
    }
}
