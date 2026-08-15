using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace K2PerfMonitor.Data;

/// <summary>
/// Design-time factory สำหรับให้ "dotnet ef migrations add" ทำงานได้
/// โดยไม่ต้องรัน application จริง (EF tools จะเรียก factory นี้เพื่อสร้าง DbContext)
/// Connection string ใช้ placeholder — ตอนรันจริงจะใช้จาก appsettings ของ Worker/Web
/// </summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<MonitorDbContext>
{
    public MonitorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=K2PerfMonitor_DesignTime;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new MonitorDbContext(options);
    }
}
