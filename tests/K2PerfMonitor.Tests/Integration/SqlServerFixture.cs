using K2PerfMonitor.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace K2PerfMonitor.Tests.Integration;

/// <summary>
/// Fixture ที่สร้าง Monitoring DB จริงบน SQL Server (LocalDB โดย default) ผ่าน EF migrations
/// - ใช้ตัว SQL Server เดียวกันเป็นทั้ง Monitor DB และ Source (มี DMV ให้ collector อ่านจริง)
/// - override ผ่าน env var MONITOR_TEST_SQL (เช่นให้ CI ชี้ SQL Server service container)
/// - ถ้าเชื่อมต่อไม่ได้ → Available=false → integration tests จะถูก skip (ไม่ทำ CI แดง)
///
/// พิสูจน์ requirement "empty SQL → migration → seed → application ready" แบบ end-to-end จริง
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DefaultBase =
        @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=10";

    private string _baseConn = "";
    private string _dbName = "";

    public bool Available { get; private set; }
    public string? SkipReason { get; private set; }
    public string MonitorConnectionString { get; private set; } = "";
    /// <summary>source สำหรับ collector — ใช้ instance เดียวกัน (มี DMV)</summary>
    public string SourceConnectionString => MonitorConnectionString;
    public TestDbContextFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _baseConn = Environment.GetEnvironmentVariable("MONITOR_TEST_SQL") ?? DefaultBase;
        _dbName = "K2PerfMonitor_IT_" + Guid.NewGuid().ToString("N")[..10];
        MonitorConnectionString = new SqlConnectionStringBuilder(_baseConn)
        {
            InitialCatalog = _dbName
        }.ToString();

        try
        {
            Factory = new TestDbContextFactory(MonitorConnectionString);
            await using var db = Factory.CreateDbContext();
            await db.Database.MigrateAsync();
            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason = $"SQL Server not reachable for integration tests ({ex.GetType().Name}: {ex.Message}). " +
                         "Set MONITOR_TEST_SQL or install SQL Server LocalDB to run them.";
        }
    }

    public async Task DisposeAsync()
    {
        if (!Available) return;
        try
        {
            // drop test DB (SINGLE_USER เพื่อตัด connection ค้าง)
            var masterConn = new SqlConnectionStringBuilder(_baseConn) { InitialCatalog = "master" }.ToString();
            await using var conn = new SqlConnection(masterConn);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"IF DB_ID('{_dbName}') IS NOT NULL BEGIN " +
                $"ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{_dbName}]; END";
            await cmd.ExecuteNonQueryAsync();
        }
        catch { /* best-effort cleanup */ }
    }
}

/// <summary>xUnit collection เพื่อแชร์ fixture (สร้าง DB ครั้งเดียวต่อชุดทดสอบ)</summary>
[CollectionDefinition("sqlserver")]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }

/// <summary>IDbContextFactory เรียบง่ายสำหรับ integration test (ไม่พึ่ง DI container)</summary>
public sealed class TestDbContextFactory : IDbContextFactory<MonitorDbContext>
{
    private readonly DbContextOptions<MonitorDbContext> _options;
    public TestDbContextFactory(string connectionString)
        => _options = new DbContextOptionsBuilder<MonitorDbContext>().UseSqlServer(connectionString).Options;

    public MonitorDbContext CreateDbContext() => new(_options);
}
