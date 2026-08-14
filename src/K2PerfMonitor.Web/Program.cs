using Hangfire;
using Hangfire.SqlServer;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Data;
using K2PerfMonitor.Web.Components;
using K2PerfMonitor.Web.Security;
using K2PerfMonitor.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
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

// bind options for read-only display ในหน้า Settings (schedule + notification channel status)
builder.Services.Configure<CollectorScheduleOptions>(builder.Configuration.GetSection(CollectorScheduleOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<TeamsOptions>(builder.Configuration.GetSection(TeamsOptions.SectionName));
builder.Services.Configure<LineOptions>(builder.Configuration.GetSection(LineOptions.SectionName));

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

// ---- Auth / RBAC (Phase 8) ----
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<InstanceService>();
builder.Services.AddScoped<AlertRuleService>();
// Data Protection — persist keys ให้ cookie/credential ถอดรหัสได้ข้าม restart/instance
var keyPath = builder.Configuration["DataProtection:KeyPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
    .SetApplicationName("K2PerfMonitor");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "K2PM.Auth";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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

    // seed admin คนแรกจาก config (ถ้ายังไม่มีผู้ใช้)
    try
    {
        var userSvc = scope.ServiceProvider.GetRequiredService<UserService>();
        await userSvc.SeedAdminAsync(builder.Configuration["Auth:InitialAdminPassword"]);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not seed initial admin user");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAuthEndpoints();

// SignalR hub — real-time metric/alert relay
app.MapHub<K2PerfMonitor.Realtime.MonitorHub>("/hubs/monitor");

// Hangfire dashboard — เฉพาะ Admin (RBAC)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminDashboardAuthorizationFilter() }
});

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
