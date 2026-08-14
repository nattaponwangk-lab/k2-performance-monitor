using System.ComponentModel.DataAnnotations;
using K2PerfMonitor.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// ผู้ใช้ระบบ (auth ของระบบเอง) — password เก็บเป็น hash เท่านั้น (PBKDF2) ห้าม plaintext
/// </summary>
[Index(nameof(Username), IsUnique = true)]
public class AppUserEntity
{
    [Key] public long Id { get; set; }

    [MaxLength(64)] public string Username { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash (format: iterations.salt_b64.hash_b64) — ไม่ใช่ plaintext</summary>
    [MaxLength(256)] public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Viewer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}
