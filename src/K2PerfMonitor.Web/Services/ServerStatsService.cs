using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using K2PerfMonitor.Data.Implementations;
using K2PerfMonitor.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Web.Services;

/// <summary>
/// อ่าน ServerStats จาก Monitoring DB แล้ว map เป็น OverviewVm/CpuRam
/// (แทนที่ MockDataService สำหรับหน้า Overview + CPU/RAM)
/// </summary>
public class ServerStatsService
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly ILogger<ServerStatsService> _logger;

    public ServerStatsService(IDbContextFactory<MonitorDbContext> dbFactory, ILogger<ServerStatsService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>ดึงข้อมูล Overview จาก ServerStats ล่าสุด + history (30 จุด)</summary>
    public async Task<OverviewVm> GetOverviewAsync()
    {
        ServerStatEntity? latest;
        List<ServerStatEntity> history;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            latest = await db.ServerStats
                .AsNoTracking()
                .OrderByDescending(x => x.CollectedAtUtc)
                .FirstOrDefaultAsync();

            history = await db.ServerStats
                .AsNoTracking()
                .OrderByDescending(x => x.CollectedAtUtc)
                .Take(30)
                .ToListAsync();
            history.Reverse(); // เก่า → ใหม่ สำหรับ chart
        }
        catch (Exception ex)
        {
            // Monitor DB เชื่อมต่อไม่ได้ → degrade เป็นหน้าว่าง (ไม่ให้หน้าจอ crash)
            _logger.LogWarning(ex, "Cannot read ServerStats from Monitor DB — showing empty overview");
            return EmptyOverview("— เชื่อมต่อ Monitor DB ไม่ได้ —");
        }

        if (latest == null)
        {
            return EmptyOverview();
        }

        var cpu = latest.CpuPercent;
        var mem = latest.MemoryPercent;
        var health = Math.Round(100 - (cpu * 0.4 + mem * 0.3), 1);
        health = Math.Clamp(health, 0, 100);

        // นับ active alerts (ในรอบนี้ยังไม่มี alert engine — ใส่ 0 ก่อน)
        return new OverviewVm
        {
            HealthScore = health,
            CpuPercent = cpu,
            MemoryPercent = mem,
            AvailableMemoryMb = latest.AvailableMemoryMb,
            UsedMemoryMb = latest.UsedMemoryMb,
            TotalMemoryMb = latest.TotalMemoryMb,
            ConnectionCount = latest.ConnectionCount,
            ActiveRequestCount = latest.ActiveRequestCount,
            BlockedProcessCount = latest.BlockedProcessCount,
            BatchRequestsPerSec = latest.BatchRequestsPerSec,
            UptimeSeconds = latest.UptimeSeconds,
            OnlineSchedulerCount = latest.OnlineSchedulerCount,
            ActiveAlertCount = 0,
            CriticalAlertCount = 0,
            CpuHistory = history.Select(h => new ChartPoint { Time = h.CollectedAtUtc, Value = h.CpuPercent }).ToList(),
            MemoryHistory = history.Select(h => new ChartPoint { Time = h.CollectedAtUtc, Value = h.MemoryPercent }).ToList()
        };
    }

    private static OverviewVm EmptyOverview(string instanceName = "— ยังไม่มีข้อมูล —") => new()
    {
        HealthScore = 0,
        InstanceName = instanceName
    };

    /// <summary>เช็คว่ามีข้อมูลใน DB หรือไม่ (Web ใช้เพื่อแสดงสถานะ mock vs real)</summary>
    public async Task<bool> HasDataAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.ServerStats.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot check ServerStats availability — treating as no data");
            return false;
        }
    }
}
