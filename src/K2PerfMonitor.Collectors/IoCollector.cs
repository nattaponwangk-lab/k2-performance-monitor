using K2PerfMonitor.Core.Constants;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// I/O Collector — จาก sys.dm_io_virtual_file_stats (cumulative → delta)
///
/// วัด latency ระดับไฟล์ (stall ms ต่อ operation) ต่อ database file
/// - counters สะสม → เทียบ snapshot ก่อนหน้า (ROADMAP §6)
/// - IoStallMsPerRead  = ΔstallRead  / Δreads   (guard div0)
/// - IoStallMsPerWrite = ΔstallWrite / Δwrites
/// - Singleton (เก็บ baseline), รอบแรกคืน 0 items
/// - SourceKey = "{DatabaseName}|{LogicalFileName}"
/// </summary>
public sealed class IoCollector : SqlCollectorBase
{
    private readonly DeltaBaselineStore _store;

    public override CollectorType Type => CollectorType.Io;
    public override string DisplayName => "I/O Statistics";

    public IoCollector(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        CollectionContext context,
        DeltaBaselineStore store,
        ILogger<IoCollector> logger)
        : base(conn, schedule, context, logger) => _store = store;

    private sealed record IoRaw(
        string DatabaseName, string? LogicalName, string? FileType,
        long Reads, long Writes, long BytesRead, long BytesWritten,
        double StallReadMs, double StallWriteMs);

    protected override async Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct)
    {
        var current = (await reader.QueryAsync("""
            SELECT
                DB_NAME(vfs.database_id)  AS DatabaseName,
                mf.name                   AS LogicalFileName,
                mf.type_desc              AS FileType,
                vfs.database_id, vfs.file_id,
                vfs.num_of_reads          AS NumOfReads,
                vfs.num_of_writes         AS NumOfWrites,
                vfs.num_of_bytes_read     AS BytesRead,
                vfs.num_of_bytes_written  AS BytesWritten,
                vfs.io_stall_read_ms      AS IoStallReadMs,
                vfs.io_stall_write_ms     AS IoStallWriteMs
            FROM sys.dm_io_virtual_file_stats(NULL, NULL) vfs
            JOIN sys.master_files mf
              ON mf.database_id = vfs.database_id AND mf.file_id = vfs.file_id;
            """,
            r =>
            {
                var key = $"{r.GetStr("DatabaseName")}|{r.GetStrOrNull("LogicalFileName")}";
                return new KeyValuePair<string, IoRaw>(key, new IoRaw(
                    r.GetStr("DatabaseName"), r.GetStrOrNull("LogicalFileName"), r.GetStrOrNull("FileType"),
                    r.GetLong("NumOfReads"), r.GetLong("NumOfWrites"),
                    r.GetLong("BytesRead"), r.GetLong("BytesWritten"),
                    r.GetDouble("IoStallReadMs"), r.GetDouble("IoStallWriteMs")));
            }, ct))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var deltas = _store.Get<IoRaw>($"Io:{Context.InstanceId}").Update(current, (p, c) => new IoRaw(
            c.DatabaseName, c.LogicalName, c.FileType,
            DeltaMath.Diff(p.Reads, c.Reads), DeltaMath.Diff(p.Writes, c.Writes),
            DeltaMath.Diff(p.BytesRead, c.BytesRead), DeltaMath.Diff(p.BytesWritten, c.BytesWritten),
            DeltaMath.Diff(p.StallReadMs, c.StallReadMs), DeltaMath.Diff(p.StallWriteMs, c.StallWriteMs)));

        if (deltas.Count == 0)
            return Array.Empty<MetricItem>();

        return deltas.Values
            .Where(d => d.Reads > 0 || d.Writes > 0)
            .Select(d =>
            {
                var stallPerRead = d.Reads > 0 ? Math.Round(d.StallReadMs / d.Reads, 2) : 0;
                var stallPerWrite = d.Writes > 0 ? Math.Round(d.StallWriteMs / d.Writes, 2) : 0;
                var worst = Math.Max(stallPerRead, stallPerWrite);
                return new MetricItem
                {
                    Key = $"{d.DatabaseName}|{d.LogicalName}",
                    MetricField = MetricFields.IoStallMsPerRead,
                    NumericValue = stallPerRead,
                    Severity = worst > 50 ? Severity.Critical : worst > 20 ? Severity.Warning : Severity.Info,
                    Summary = $"{d.DatabaseName}/{d.LogicalName} — {stallPerRead:0.0} ms/read, {stallPerWrite:0.0} ms/write",
                    Payload = new Dictionary<string, object?>
                    {
                        ["DatabaseName"] = d.DatabaseName,
                        ["LogicalFileName"] = d.LogicalName,
                        ["FileType"] = d.FileType,
                        ["NumOfReads"] = d.Reads,
                        ["NumOfWrites"] = d.Writes,
                        ["BytesRead"] = d.BytesRead,
                        ["BytesWritten"] = d.BytesWritten,
                        ["IoStallReadMs"] = d.StallReadMs,
                        ["IoStallWriteMs"] = d.StallWriteMs,
                        ["IoStallMsPerRead"] = stallPerRead,
                        ["IoStallMsPerWrite"] = stallPerWrite,
                        ["SourceKey"] = $"{d.DatabaseName}|{d.LogicalName}"
                    }
                };
            })
            .OrderByDescending(i => i.NumericValue)
            .Take(Schedule.TopN)
            .ToList();
    }
}
