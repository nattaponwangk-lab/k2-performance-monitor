using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace K2PerfMonitor.Notifications;

/// <summary>
/// จัดการการแจ้งเตือน alert หนึ่งตัว:
/// โหลด channels + cooldown จาก rule → เช็ค cooldown → fan-out ไป provider ที่เปิดอยู่ + ตรงช่องทาง
/// → ถ้าส่งได้อย่างน้อยหนึ่งช่อง บันทึกว่าแจ้งแล้ว (MarkAlertNotified)
/// </summary>
public sealed class AlertNotificationService : IAlertNotifier
{
    private readonly IEnumerable<INotificationProvider> _providers;
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly IMetricRepository _repo;
    private readonly ILogger<AlertNotificationService> _logger;

    public AlertNotificationService(
        IEnumerable<INotificationProvider> providers,
        IDbContextFactory<MonitorDbContext> dbFactory,
        IMetricRepository repo,
        ILogger<AlertNotificationService> logger)
    {
        _providers = providers;
        _dbFactory = dbFactory;
        _repo = repo;
        _logger = logger;
    }

    public async Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        // ค่าเริ่มต้น (ถ้าไม่มี rule): ส่งทุกช่องทาง, ไม่มี cooldown
        var channels = NotificationChannel.All;
        var cooldownMinutes = 0;

        if (alert.RuleId is long ruleId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var rule = await db.AlertRules.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);
            if (rule is not null)
            {
                channels = rule.Channels;
                cooldownMinutes = rule.CooldownMinutes;
            }
        }

        if (!NotificationCooldown.ShouldNotify(alert.LastNotifiedAtUtc, cooldownMinutes, DateTime.UtcNow))
        {
            _logger.LogDebug("Alert {Id} ({Dedup}) still in cooldown — skip notify", alert.Id, alert.DedupKey);
            return;
        }

        var targets = _providers
            .Where(p => p.IsEnabled && channels.HasFlag(p.Channel))
            .ToList();

        if (targets.Count == 0)
        {
            _logger.LogDebug("No enabled provider for alert {Id} (channels={Channels})", alert.Id, channels);
            return;
        }

        var message = BuildMessage(alert);
        var anySent = false;

        foreach (var provider in targets)
        {
            if (await SendWithRetryAsync(provider, message, alert.Id, cancellationToken))
                anySent = true;
        }

        if (anySent)
            await _repo.MarkAlertNotifiedAsync(alert.Id, cancellationToken);
    }

    /// <summary>retry ต่อ provider — backoff แบบ exponential (0.5s, 1s, 2s) สูงสุด 3 ครั้ง</summary>
    private async Task<bool> SendWithRetryAsync(
        INotificationProvider provider, NotificationMessage message, long alertId, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (await provider.SendAsync(message, ct))
                {
                    _logger.LogInformation("Alert {Id} sent via {Provider} (attempt {Attempt})",
                        alertId, provider.Name, attempt);
                    return true;
                }
                _logger.LogWarning("Provider {Provider} returned false for alert {Id} (attempt {Attempt}/{Max})",
                    provider.Name, alertId, attempt, maxAttempts);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed for alert {Id} (attempt {Attempt}/{Max})",
                    provider.Name, alertId, attempt, maxAttempts);
            }

            if (attempt < maxAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1)), ct);
        }

        _logger.LogError("Alert {Id} could not be delivered via {Provider} after {Max} attempts",
            alertId, provider.Name, maxAttempts);
        return false;
    }

    private static NotificationMessage BuildMessage(Alert alert) => new()
    {
        Title = alert.Title,
        Summary = alert.Summary,
        Detail = alert.Detail,
        Severity = alert.Severity,
        CollectorType = alert.CollectorType,
        Timestamp = alert.RaisedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
    };
}
