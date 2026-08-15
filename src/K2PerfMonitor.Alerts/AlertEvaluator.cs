using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Extensions;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Results;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Alerts;

/// <summary>
/// ประเมิน CollectorResult เทียบกับ AlertRules ที่เปิดใช้งานของ collector นั้น
/// - โหลด rules จาก DB (read-only) → เรียก <see cref="Match"/> (pure) → คืน alert ที่ละเมิด
/// - dedup ด้วย key = "collector:metricField:itemKey" และเก็บ severity สูงสุด
///   (เช่น CPU 96% เข้าเงื่อนไขทั้ง rule &gt;80 และ &gt;95 → เหลือ Critical ตัวเดียว)
/// การ persist/cooldown/auto-resolve อยู่ที่ repository + CollectorJob
/// </summary>
public sealed class AlertEvaluator : IAlertEvaluator
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;

    public AlertEvaluator(IDbContextFactory<MonitorDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<Alert>> EvaluateAsync(
        CollectorResult result, CancellationToken cancellationToken = default)
    {
        if (!result.Success || result.Items.Count == 0)
            return Array.Empty<Alert>();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var rules = await db.AlertRules
            .AsNoTracking()
            .Where(r => r.Enabled && r.CollectorType == result.CollectorType)
            .ToListAsync(cancellationToken);

        if (rules.Count == 0) return Array.Empty<Alert>();

        // hysteresis: โหลด dedup key ของ alert ที่ยัง active → กัน flapping รอบ threshold
        var activeKeys = await db.Alerts
            .AsNoTracking()
            .Where(a => a.CollectorType == result.CollectorType && a.Status != Core.Enums.AlertStatus.Resolved)
            .Select(a => a.DedupKey)
            .ToHashSetAsync(cancellationToken);

        return Match(result, rules, activeKeys, HysteresisFraction);
    }

    /// <summary>สัดส่วน hold-band (10%) — alert ที่ active อยู่จะยัง firing จนค่าหลุดออกนอกแบนด์นี้</summary>
    public const double HysteresisFraction = 0.10;

    /// <summary>
    /// แกนประเมินแบบ pure (ไม่แตะ DB) — ใช้ทดสอบได้ตรงๆ
    /// </summary>
    public static IReadOnlyList<Alert> Match(
        CollectorResult result,
        IReadOnlyList<AlertRuleEntity> rules,
        ISet<string>? activeDedupKeys = null,
        double hysteresisFraction = 0)
    {
        var best = new Dictionary<string, Alert>();

        foreach (var rule in rules)
        {
            if (!rule.Enabled || rule.CollectorType != result.CollectorType)
                continue;

            foreach (var item in result.Items)
            {
                if (item.MetricField != rule.MetricField || item.NumericValue is not double value)
                    continue;

                var dedupKey = $"{result.InstanceId}:{result.CollectorType}:{rule.MetricField}:{item.Key}";

                var fires = rule.Operator.Matches(value, rule.Threshold);
                if (!fires)
                {
                    // hysteresis hold: ถ้า alert ยัง active และค่ายังอยู่ในแบนด์ → คง firing (กัน flapping)
                    var held = hysteresisFraction > 0
                               && activeDedupKeys is not null
                               && activeDedupKeys.Contains(dedupKey)
                               && rule.Operator.Matches(value, HoldThreshold(rule.Threshold, rule.Operator, hysteresisFraction));
                    if (!held) continue;
                }
                var candidate = new Alert
                {
                    RuleId = rule.Id,
                    CollectorType = rule.CollectorType,
                    InstanceId = result.InstanceId,
                    InstanceName = result.InstanceName,
                    DedupKey = dedupKey,
                    Severity = rule.Severity,
                    Title = string.IsNullOrWhiteSpace(rule.TitleTemplate) ? rule.Name : rule.TitleTemplate!,
                    Summary = !string.IsNullOrWhiteSpace(item.Summary)
                        ? item.Summary!
                        : $"{rule.MetricField} {rule.Operator.ToSymbol()} {rule.Threshold:0.##} (actual {value:0.##})",
                    MetricValue = value,
                    ThresholdValue = rule.Threshold,
                    Status = Core.Enums.AlertStatus.New,
                    RaisedAtUtc = result.CollectedAtUtc
                };

                // เก็บตัวที่ severity สูงสุดต่อ dedup key (escalation)
                if (!best.TryGetValue(dedupKey, out var existing) || candidate.Severity > existing.Severity)
                    best[dedupKey] = candidate;
            }
        }

        return best.Values.ToList();
    }

    /// <summary>
    /// threshold ของ hold-band: ต้องหลุดออกไป <paramref name="fraction"/> จาก threshold เดิม
    /// ถึงจะ resolve — กัน alert เด้งไปมารอบ threshold
    ///   GreaterThan/OrEqual: hold = threshold*(1-fraction)  (ต้องลงต่ำกว่านี้ถึง clear)
    ///   LessThan/OrEqual:    hold = threshold*(1+fraction)  (ต้องขึ้นสูงกว่านี้ถึง clear)
    /// </summary>
    internal static double HoldThreshold(double threshold, ComparisonOperator op, double fraction) => op switch
    {
        ComparisonOperator.GreaterThan or ComparisonOperator.GreaterThanOrEqual => threshold * (1 - fraction),
        ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual => threshold * (1 + fraction),
        _ => threshold
    };
}
