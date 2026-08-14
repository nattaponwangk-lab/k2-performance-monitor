using System.Diagnostics;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// Base class ของ SQL collector ทุกตัว — รวม cross-cutting logic ไว้ที่เดียว:
/// - เปิด/ปิด <see cref="SqlDmvReader"/> ต่อรอบ (connection ไม่ค้าง)
/// - จับเวลา (Elapsed) + ครอบ try/catch → source ล่มก็คืน <see cref="CollectorResult"/> Success=false
///   (ไม่ทำให้ Worker crash — ตาม ROADMAP §20 resilience)
/// - รักษา contract <see cref="ICollector"/> เดิม (subclass เขียนแค่ CollectItemsAsync)
///
/// การจัดการ cancellation: ถ้า token ถูกยกเลิก (worker shutdown) จะโยน
/// <see cref="OperationCanceledException"/> ออกไป (ไม่นับเป็น collector failure)
/// ส่วน command timeout/connection error จะถูกจับเป็น Success=false
/// </summary>
public abstract class SqlCollectorBase : ICollector
{
    private readonly ConnectionStringsOptions _conn;
    protected CollectorScheduleOptions Schedule { get; }
    protected ILogger Logger { get; }

    protected SqlCollectorBase(
        IOptions<ConnectionStringsOptions> conn,
        IOptions<CollectorScheduleOptions> schedule,
        ILogger logger)
    {
        _conn = conn.Value;
        Schedule = schedule.Value;
        Logger = logger;
    }

    public abstract CollectorType Type { get; }
    public abstract string DisplayName { get; }

    /// <summary>connection string ของ source ที่ collector นี้ใช้ (default = SourceDb)</summary>
    protected virtual string ConnectionString => _conn.SourceDb;

    /// <summary>command timeout (วินาที) — override ได้ต่อ collector</summary>
    protected virtual int CommandTimeoutSeconds => 30;

    /// <summary>
    /// แกนการเก็บข้อมูลจริง — subclass ดึงจาก DMV ผ่าน reader แล้วคืน MetricItems
    /// (base จัดการ connection/timing/error ให้)
    /// </summary>
    protected abstract Task<IReadOnlyList<MetricItem>> CollectItemsAsync(SqlDmvReader reader, CancellationToken ct);

    public async Task<CollectorResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();
        try
        {
            await using var reader = new SqlDmvReader(ConnectionString) { CommandTimeoutSeconds = CommandTimeoutSeconds };
            await reader.OpenAsync(cancellationToken);

            var items = await CollectItemsAsync(reader, cancellationToken);
            sw.Stop();

            return new CollectorResult
            {
                CollectorType = Type,
                CollectedAtUtc = started,
                Success = true,
                Elapsed = sw.Elapsed,
                Items = items
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // worker shutdown — ปล่อยให้ Hangfire จัดการ ไม่นับเป็น failure
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Logger.LogWarning(ex,
                "{Collector} could not collect from source (source may be unavailable) — recording failed run",
                DisplayName);

            return new CollectorResult
            {
                CollectorType = Type,
                CollectedAtUtc = started,
                Success = false,
                ErrorMessage = ex.Message,
                Elapsed = sw.Elapsed
            };
        }
    }
}
