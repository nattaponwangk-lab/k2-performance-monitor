using System.Xml.Linq;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// Deadlock Collector — อ่าน deadlock graph จาก system_health Extended Events ring buffer
///
/// - ดึง target_data XML ของ session 'system_health' → shred หา event 'xml_deadlock_report'
/// - track timestamp ล่าสุดที่เคยเห็น (Singleton) → emit เฉพาะ deadlock ใหม่ (กัน re-insert)
/// - dedup ชั้นสองที่ repository ด้วย SourceKey (timestamp|victim)
/// - Informational (ไม่มี numeric alert) — เก็บ graph ไว้แสดงใน viewer
/// </summary>
public sealed class DeadlockCollector : SqlCollectorBase
{
    private DateTime _lastSeenUtc = DateTime.MinValue;
    private readonly object _gate = new();

    public override CollectorType Type => CollectorType.Deadlock;
    public override string DisplayName => "Deadlocks";

    public DeadlockCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger<DeadlockCollector> logger)
        : base(conn, schedule, logger) { }

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        var xml = await reader.ExecuteScalarAsync<string>("""
            SELECT CAST(xet.target_data AS nvarchar(max))
            FROM sys.dm_xe_session_targets xet
            JOIN sys.dm_xe_sessions xe ON xe.address = xet.event_session_address
            WHERE xe.name = 'system_health' AND xet.target_name = 'ring_buffer';
            """, ct);

        if (string.IsNullOrWhiteSpace(xml))
            return Array.Empty<MetricItem>();

        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not parse system_health ring buffer XML");
            return Array.Empty<MetricItem>();
        }

        DateTime cutoff;
        lock (_gate) cutoff = _lastSeenUtc;

        var items = new List<MetricItem>();
        var maxSeen = cutoff;

        foreach (var evt in doc.Descendants("event")
                     .Where(e => (string?)e.Attribute("name") == "xml_deadlock_report"))
        {
            var tsAttr = (string?)evt.Attribute("timestamp");
            if (!DateTime.TryParse(tsAttr, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var ts))
                ts = DateTime.UtcNow;
            if (ts > maxSeen) maxSeen = ts;
            if (ts <= cutoff) continue; // เคยเห็นแล้ว

            var deadlock = evt.Descendants("deadlock").FirstOrDefault();
            if (deadlock is null) continue;

            var victimId = deadlock.Element("victim-list")?.Element("victimProcess")?.Attribute("id")?.Value ?? "";
            var processes = deadlock.Element("process-list")?.Elements("process").ToList() ?? new List<XElement>();

            var victim = processes.FirstOrDefault(p => p.Attribute("id")?.Value == victimId);
            var survivor = processes.FirstOrDefault(p => p.Attribute("id")?.Value != victimId);

            var key = $"{ts:O}|{victimId}";
            items.Add(new MetricItem
            {
                Key = key,
                MetricField = null,
                NumericValue = 1,
                Severity = Severity.Warning,
                Summary = $"Deadlock at {ts:u} — victim {InputBuf(victim)?.Trim().Split('\n').FirstOrDefault()}",
                Payload = new Dictionary<string, object?>
                {
                    ["DeadlockAtUtc"] = ts,
                    ["VictimProcessId"] = victimId,
                    ["VictimQueryText"] = InputBuf(victim) ?? "",
                    ["VictimLoginName"] = victim?.Attribute("loginname")?.Value,
                    ["SurvivorQueryText"] = InputBuf(survivor) ?? "",
                    ["SurvivorLoginName"] = survivor?.Attribute("loginname")?.Value,
                    ["DeadlockGraphXml"] = deadlock.ToString(),
                    ["SourceKey"] = key
                }
            });
        }

        lock (_gate)
            if (maxSeen > _lastSeenUtc) _lastSeenUtc = maxSeen;

        return items;
    }

    private static string? InputBuf(XElement? process)
        => process?.Element("inputbuf")?.Value?.Trim();
}
