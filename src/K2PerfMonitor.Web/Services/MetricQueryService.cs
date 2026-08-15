using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using K2PerfMonitor.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Web.Services;

/// <summary>
/// อ่าน metric จริงจาก Monitoring DB แล้ว map เป็น ViewModel สำหรับหน้า dashboard
/// (แทน MockDataService — Phase 6)
///
/// หลักการ:
/// - แต่ละหน้าดึง "รอบล่าสุด" (rows ที่ CollectedAtUtc ตรงกับ snapshot ล่าสุดของตารางนั้น)
///   ยกเว้น deadlock ที่ดึงประวัติย้อนหลัง
/// - ครอบ try/catch เสมอ → DB ล่ม/ยังไม่มีข้อมูล คืน <see cref="QueryResult{T}"/> ที่บอกสถานะ
///   (หน้า UI แสดง Loading/Empty/Error/Unavailable ได้ครบ ไม่ crash)
/// </summary>
public sealed class MetricQueryService
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly InstanceFilterState _filter;
    private readonly ILogger<MetricQueryService> _logger;

    public MetricQueryService(
        IDbContextFactory<MonitorDbContext> dbFactory,
        InstanceFilterState filter,
        ILogger<MetricQueryService> logger)
    {
        _dbFactory = dbFactory;
        _filter = filter;
        _logger = logger;
    }

    /// <summary>รายการ instance ที่มีข้อมูลจริง (จาก CollectorRuns) — สำหรับ instance selector</summary>
    public async Task<IReadOnlyList<InstanceOption>> GetInstancesAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.CollectorRuns.AsNoTracking()
                .Select(r => new { r.InstanceId, r.InstanceName })
                .Distinct()
                .OrderBy(x => x.InstanceId)
                .Select(x => new InstanceOption(x.InstanceId, x.InstanceName))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot list instances");
            return new List<InstanceOption> { new(0, "Default") };
        }
    }

    // ---- generic latest-cycle loader (filter ตาม instance ที่เลือก) ----
    private async Task<QueryResult<T>> LoadLatestAsync<TEntity, T>(
        Func<MonitorDbContext, IQueryable<TEntity>> set,
        Func<TEntity, T> map,
        string what)
        where TEntity : MetricEntityBase
    {
        try
        {
            var instanceId = _filter.SelectedInstanceId;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var q = set(db).AsNoTracking().Where(e => e.InstanceId == instanceId);
            if (!await q.AnyAsync())
                return QueryResult<T>.Empty();

            var latest = await q.MaxAsync(e => e.CollectedAtUtc);
            var rows = await q.Where(e => e.CollectedAtUtc == latest).ToListAsync();
            return QueryResult<T>.Ok(rows.Select(map).ToList(), latest);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot read {What} from Monitor DB", what);
            return QueryResult<T>.Error(ex.Message);
        }
    }

    public Task<QueryResult<DatabaseStatVm>> GetDatabaseStatsAsync()
        => LoadLatestAsync(db => db.DatabaseStats, e => new DatabaseStatVm
        {
            DatabaseId = e.DatabaseId,
            DatabaseName = e.DatabaseName,
            State = e.State,
            RecoveryModel = e.RecoveryModel,
            CompatibilityLevel = e.CompatibilityLevel,
            IsSystemDatabase = e.IsSystemDatabase,
            DataSizeMb = e.DataSizeMb,
            LogSizeMb = e.LogSizeMb,
            TotalSizeMb = e.TotalSizeMb
        }, "database stats");

    public Task<QueryResult<SlowQueryVm>> GetSlowQueriesAsync()
        => LoadLatestAsync(db => db.SlowQueries, e => new SlowQueryVm
        {
            QueryHash = e.SourceKey,
            QueryText = e.QueryText,
            DatabaseName = e.DatabaseName,
            ObjectName = e.ObjectName,
            ExecutionCount = e.ExecutionCount,
            AvgDurationMs = e.AvgDurationMs,
            MaxDurationMs = e.MaxDurationMs,
            TotalDurationMs = e.TotalDurationMs,
            AvgLogicalReads = e.AvgLogicalReads,
            AvgCpuMs = e.AvgCpuMs,
            LastExecutionUtc = e.LastExecutionUtc,
            Severity = e.AvgDurationMs > 15000 ? Severity.Critical : e.AvgDurationMs > 5000 ? Severity.Warning : Severity.Info
        }, "slow queries");

    public Task<QueryResult<WaitStatVm>> GetWaitStatsAsync()
        => LoadLatestAsync(db => db.WaitStats, e => new WaitStatVm
        {
            WaitType = e.WaitType,
            Category = WaitCategory(e.WaitType),
            WaitingTasksCount = e.WaitingTasksCount,
            WaitTimeMs = e.WaitTimeMs,
            SignalWaitTimeMs = e.SignalWaitTimeMs,
            MaxWaitTimeMs = e.MaxWaitTimeMs,
            WaitPercent = e.WaitPercent,
            IsBenign = e.IsBenign
        }, "wait stats");

    public Task<QueryResult<BlockingVm>> GetBlockingAsync()
        => LoadLatestAsync(db => db.BlockingEvents, e => new BlockingVm
        {
            BlockedSessionId = e.BlockedSessionId,
            BlockingSessionId = e.BlockingSessionId,
            WaitDurationMs = e.WaitDurationMs,
            WaitType = e.WaitType,
            Resource = e.Resource,
            RequestedLockMode = e.RequestedLockMode,
            BlockedQueryText = e.BlockedQueryText,
            BlockingQueryText = e.BlockingQueryText,
            BlockedLoginName = e.BlockedLoginName,
            BlockingLoginName = e.BlockingLoginName,
            Severity = e.WaitDurationMs > 120000 ? Severity.Critical : e.WaitDurationMs > 30000 ? Severity.Warning : Severity.Info
        }, "blocking");

    public Task<QueryResult<StoredProcedureVm>> GetStoredProceduresAsync()
        => LoadLatestAsync(db => db.StoredProcedureStats, e => new StoredProcedureVm
        {
            DatabaseName = e.DatabaseName,
            ObjectName = $"{e.SchemaName}.{e.ObjectName}",
            ExecutionCount = e.ExecutionCount,
            AvgElapsedMs = e.AvgElapsedMs,
            MaxElapsedMs = e.MaxElapsedMs,
            AvgLogicalReads = e.AvgLogicalReads,
            LastExecutionUtc = e.LastExecutionUtc,
            Severity = e.AvgElapsedMs > 15000 ? Severity.Critical : e.AvgElapsedMs > 5000 ? Severity.Warning : Severity.Info
        }, "stored procedures");

    public Task<QueryResult<IoStatVm>> GetIoStatsAsync()
        => LoadLatestAsync(db => db.IoStats, e => new IoStatVm
        {
            DatabaseName = e.DatabaseName,
            LogicalFileName = e.LogicalFileName,
            FileType = e.FileType,
            NumOfReads = e.NumOfReads,
            NumOfWrites = e.NumOfWrites,
            IoStallMsPerRead = e.IoStallMsPerRead,
            IoStallMsPerWrite = e.IoStallMsPerWrite,
            Severity = e.IoStallMsPerRead > 50 || e.IoStallMsPerWrite > 50 ? Severity.Critical
                     : e.IoStallMsPerRead > 20 || e.IoStallMsPerWrite > 20 ? Severity.Warning : Severity.Info
        }, "io stats");

    public Task<QueryResult<IndexRecommendationVm>> GetIndexRecommendationsAsync()
        => LoadLatestAsync(db => db.IndexRecommendations, e => new IndexRecommendationVm
        {
            RecommendationType = e.RecommendationType,
            DatabaseName = e.DatabaseName,
            TableName = $"{e.SchemaName}.{e.TableName}",
            EqualityColumns = e.EqualityColumns,
            InequalityColumns = e.InequalityColumns,
            IncludedColumns = e.IncludedColumns,
            Impact = e.Impact,
            UserSeeks = e.UserSeeks,
            UserScans = e.UserScans,
            IndexName = e.IndexName,
            RecommendationScript = e.RecommendationScript
        }, "index recommendations");

    public Task<QueryResult<ExecutionPlanVm>> GetExecutionPlansAsync()
        => LoadLatestAsync(db => db.ExecutionPlans, e => new ExecutionPlanVm
        {
            QueryHash = e.QueryHash,
            DatabaseName = e.DatabaseName,
            ObjectName = e.ObjectName,
            ExecutionCount = e.ExecutionCount,
            AvgDurationMs = e.AvgDurationMs,
            AvgCpuMs = e.AvgCpuMs,
            AvgLogicalReads = e.AvgLogicalReads,
            QueryText = e.QueryText,
            PlanXml = e.PlanXml,
            Severity = e.AvgDurationMs > 15000 ? Severity.Critical : e.AvgDurationMs > 5000 ? Severity.Warning : Severity.Info
        }, "execution plans");

    /// <summary>Deadlocks — ประวัติย้อนหลัง (ไม่ใช่รอบล่าสุด) เรียงใหม่สุดก่อน</summary>
    public async Task<QueryResult<DeadlockVm>> GetDeadlocksAsync(int take = 100)
    {
        try
        {
            var instanceId = _filter.SelectedInstanceId;
            await using var db = await _dbFactory.CreateDbContextAsync();
            if (!await db.DeadlockEvents.AnyAsync(x => x.InstanceId == instanceId))
                return QueryResult<DeadlockVm>.Empty();

            var rows = await db.DeadlockEvents.AsNoTracking()
                .Where(x => x.InstanceId == instanceId)
                .OrderByDescending(x => x.DeadlockAtUtc)
                .Take(take)
                .Select(e => new DeadlockVm
                {
                    DeadlockAtUtc = e.DeadlockAtUtc,
                    VictimProcessId = e.VictimProcessId,
                    VictimQueryText = e.VictimQueryText,
                    VictimLoginName = e.VictimLoginName,
                    SurvivorQueryText = e.SurvivorQueryText,
                    SurvivorLoginName = e.SurvivorLoginName
                })
                .ToListAsync();
            return QueryResult<DeadlockVm>.Ok(rows, rows.FirstOrDefault()?.DeadlockAtUtc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot read deadlocks from Monitor DB");
            return QueryResult<DeadlockVm>.Error(ex.Message);
        }
    }

    private static string WaitCategory(string waitType) => waitType switch
    {
        var w when w.StartsWith("PAGEIOLATCH") => "I/O",
        var w when w.StartsWith("WRITELOG") => "Transaction Log",
        var w when w.StartsWith("LCK_") => "Lock",
        var w when w.StartsWith("CXPACKET") || w.StartsWith("CXCONSUMER") => "Parallelism",
        var w when w.StartsWith("ASYNC_NETWORK_IO") => "Network/Client",
        var w when w.StartsWith("SOS_SCHEDULER_YIELD") => "CPU",
        var w when w.StartsWith("RESOURCE_SEMAPHORE") => "Memory",
        var w when w.StartsWith("BACKUP") => "Backup",
        _ => "Other"
    };
}
