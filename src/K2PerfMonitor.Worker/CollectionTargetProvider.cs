using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Worker;

/// <summary>
/// คืนเป้าหมายการเก็บ = Default (SourceDb ที่ config, InstanceId=0) + instance ที่ enabled ใน registry
/// - decrypt connection string ของ registry instance ด้วย Data Protection (key ring เดียวกับ Web)
/// - instance ที่ decrypt ไม่ได้ (key หาย) จะถูกข้าม + log (ไม่ทำให้ทั้งรอบล้ม)
/// </summary>
public sealed class CollectionTargetProvider : ICollectionTargetProvider
{
    // ต้องตรงกับ Web InstanceService.CreateProtector(...)
    private const string ProtectorPurpose = "K2PerfMonitor.MonitoredInstance.ConnectionString.v1";

    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly ConnectionStringsOptions _conn;
    private readonly IDataProtector _protector;
    private readonly ILogger<CollectionTargetProvider> _logger;

    public CollectionTargetProvider(
        IDbContextFactory<MonitorDbContext> dbFactory,
        IOptions<ConnectionStringsOptions> conn,
        IDataProtectionProvider dp,
        ILogger<CollectionTargetProvider> logger)
    {
        _dbFactory = dbFactory;
        _conn = conn.Value;
        _protector = dp.CreateProtector(ProtectorPurpose);
        _logger = logger;
    }

    public async Task<IReadOnlyList<CollectionTarget>> GetTargetsAsync(CancellationToken ct = default)
    {
        var targets = new List<CollectionTarget>
        {
            // Default instance = SourceDb ที่ config (เก็บได้เสมอแม้ไม่มี registry)
            new(0, "Default", _conn.SourceDb)
        };

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var instances = await db.MonitoredInstances.AsNoTracking()
                .Where(i => i.Enabled)
                .ToListAsync(ct);

            foreach (var i in instances)
            {
                try
                {
                    var conn = _protector.Unprotect(i.EncryptedConnectionString);
                    targets.Add(new CollectionTarget(i.Id, i.Name, conn));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Skipping instance '{Name}' — cannot decrypt connection string ({Err})", i.Name, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot load monitored instances — collecting Default only");
        }

        return targets;
    }
}
