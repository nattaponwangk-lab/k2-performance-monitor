using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Web.Security;

public sealed record InstanceView(long Id, string Name, InstanceType Type, string? Host, bool Enabled, DateTime CreatedAtUtc);

/// <summary>
/// จัดการ MonitoredInstance (multi-instance) — connection string เข้ารหัสด้วย Data Protection
/// ไม่คืน/แสดง connection string plaintext ออกไปยัง UI (มีเฉพาะ decrypt ตอน collector ใช้จริง)
/// </summary>
public sealed class InstanceService
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly IDataProtector _protector;
    private readonly ILogger<InstanceService> _logger;

    public InstanceService(
        IDbContextFactory<MonitorDbContext> dbFactory,
        IDataProtectionProvider dp,
        ILogger<InstanceService> logger)
    {
        _dbFactory = dbFactory;
        _protector = dp.CreateProtector("K2PerfMonitor.MonitoredInstance.ConnectionString.v1");
        _logger = logger;
    }

    public async Task<List<InstanceView>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.MonitoredInstances.AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new InstanceView(i.Id, i.Name, i.InstanceType, i.Host, i.Enabled, i.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task AddAsync(string name, InstanceType type, string? host, string connectionString)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.MonitoredInstances.AnyAsync(i => i.Name == name))
            throw new InvalidOperationException($"Instance '{name}' already exists");

        db.MonitoredInstances.Add(new MonitoredInstanceEntity
        {
            Name = name,
            InstanceType = type,
            Host = host,
            EncryptedConnectionString = _protector.Protect(connectionString), // เข้ารหัสก่อนเก็บ
            Enabled = true
        });
        await db.SaveChangesAsync();
        _logger.LogInformation("Monitored instance '{Name}' ({Type}) added", name, type); // ไม่ log connection string
    }

    public async Task SetEnabledAsync(long id, bool enabled)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var i = await db.MonitoredInstances.FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return;
        i.Enabled = enabled;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var i = await db.MonitoredInstances.FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return;
        db.MonitoredInstances.Remove(i);
        await db.SaveChangesAsync();
    }

    /// <summary>decrypt connection string (ใช้เฉพาะฝั่ง collector/server — ไม่ผ่าน UI)</summary>
    public string Decrypt(string encrypted) => _protector.Unprotect(encrypted);
}
