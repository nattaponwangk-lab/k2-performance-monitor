using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Web.Security;

/// <summary>
/// จัดการผู้ใช้ + ตรวจ credential (auth ของระบบเอง)
/// </summary>
public sealed class UserService
{
    private readonly IDbContextFactory<MonitorDbContext> _dbFactory;
    private readonly ILogger<UserService> _logger;

    public UserService(IDbContextFactory<MonitorDbContext> dbFactory, ILogger<UserService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>ตรวจ username/password → คืน user ถ้าถูกต้องและ active (ไม่ log password)</summary>
    public async Task<AppUserEntity?> ValidateAsync(string username, string password)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Username}", username);
            return null;
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _logger.LogInformation("User {Username} ({Role}) logged in", user.Username, user.Role);
        return user;
    }

    public async Task<List<AppUserEntity>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync();
    }

    public async Task CreateAsync(string username, string password, UserRole role)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException($"User '{username}' already exists");

        db.Users.Add(new AppUserEntity
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            Role = role,
            IsActive = true
        });
        await db.SaveChangesAsync();
        _logger.LogInformation("User {Username} ({Role}) created", username, role);
    }

    public async Task SetActiveAsync(long id, bool active)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return;
        u.IsActive = active;
        await db.SaveChangesAsync();
    }

    /// <summary>seed admin คนแรกจาก config (ครั้งแรกที่ยังไม่มีผู้ใช้) — ไม่ใช้ default password ตายตัว</summary>
    public async Task SeedAdminAsync(string? initialPassword)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync()) return;

        if (string.IsNullOrWhiteSpace(initialPassword))
        {
            _logger.LogWarning(
                "No users exist and Auth:InitialAdminPassword is not set — set it (env Auth__InitialAdminPassword) to create the initial admin account");
            return;
        }

        db.Users.Add(new AppUserEntity
        {
            Username = "admin",
            PasswordHash = PasswordHasher.Hash(initialPassword),
            Role = UserRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();
        _logger.LogInformation("Initial admin user created (username 'admin') — please change the password after first login");
    }
}
