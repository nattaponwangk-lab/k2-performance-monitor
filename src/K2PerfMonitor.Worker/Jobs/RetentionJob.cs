using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Worker.Jobs;

/// <summary>
/// งานบำรุงรักษา: ล้างข้อมูล metric/alert/run ที่เก่ากว่า RetentionDays (Phase 2)
/// Hangfire เรียกวันละครั้ง — idempotent, ปลอดภัยถ้ารันซ้ำ
/// </summary>
public sealed class RetentionJob
{
    private readonly IMetricRepository _repo;
    private readonly CollectorScheduleOptions _schedule;
    private readonly ILogger<RetentionJob> _logger;

    public RetentionJob(
        IMetricRepository repo,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<RetentionJob> logger)
    {
        _repo = repo;
        _schedule = schedule.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var days = _schedule.RetentionDays;
        _logger.LogInformation("Retention job starting — purging data older than {Days} days", days);
        var deleted = await _repo.PurgeOldDataAsync(days, ct);
        _logger.LogInformation("Retention job finished — {Deleted} rows purged", deleted);
    }
}
