using Hangfire;
using Hangfire.SqlServer;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Data;
using K2PerfMonitor.Web.Components;
using K2PerfMonitor.Web.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog structured logging ----
builder.Services.AddSerilog(cfg => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/web-.log", rollingInterval: RollingInterval.Day));

// ---- Options binding + validation ----
builder.Services.AddOptions<ConnectionStringsOptions>()
    .Bind(builder.Configuration.GetSection(ConnectionStringsOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.MonitorDb), "ConnectionStrings:MonitorDb is required")
    .ValidateOnStart();

var monitorConn = builder.Configuration.GetConnectionString("MonitorDb")
    ?? throw new InvalidOperationException("ConnectionStrings:MonitorDb is required");

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR — hub ที่ Worker (client) ส่ง snapshot/alert เข้ามา แล้ว relay ให้ browser
builder.Services.AddSignalR();

// EF Core — Monitoring DB
builder.Services.AddDbContextFactory<MonitorDbContext>(options =>
    options.UseSqlServer(monitorConn));

// Services — ข้อมูลจริงจาก Monitoring DB (แทน MockDataService ทั้งหมด — Phase 6)
builder.Services.AddScoped<ServerStatsService>();   // Overview + CPU/RAM
builder.Services.AddScoped<MetricQueryService>();   // SQL metric pages
builder.Services.AddScoped<AlertService>();         // Alerts + acknowledge

// Health checks — liveness (process) + readiness (Monitoring DB)
builder.Services.AddHealthChecks()
    .AddCheck<MonitorDbHealthCheck>("monitor-db", tags: new[] { "ready" });

// ---- Hangfire dashboard (jobs รันโดย Worker; ที่นี่แสดง dashboard อย่างเดียว) ----
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(monitorConn, new SqlServerStorageOptions
    {
        SchemaName = "HangFire",
        PrepareSchemaIfNecessary = true, // idempotent — กัน race ตอน compose start web/worker พร้อมกัน
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

var app = builder.Build();

// ---- Apply EF migrations on startup (idempotent; EF ใช้ migration lock กัน concurrent) ----
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MonitorDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        Log.Information("Monitoring DB migrations applied (Web startup)");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not apply migrations on Web startup — will rely on Worker (dashboard may be degraded until DB ready)");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR hub — real-time metric/alert relay
app.MapHub<K2PerfMonitor.Realtime.MonitorHub>("/hubs/monitor");

// Hangfire dashboard — เข้าถึงได้จากเครื่อง local เท่านั้นตาม default (Phase 8 จะผูก RBAC)
app.UseHangfireDashboard("/hangfire");

// /health = ทุก check · /health/live = process ยังอยู่ (ไม่แตะ DB) · /health/ready = พร้อมรับงาน (DB ต่อได้)
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
