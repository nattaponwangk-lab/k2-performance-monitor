using Hangfire;
using Hangfire.SqlServer;
using K2PerfMonitor.Alerts;
using K2PerfMonitor.Collectors;
using K2PerfMonitor.Notifications;
using K2PerfMonitor.Notifications.Providers;
using K2PerfMonitor.Realtime;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Data;
using K2PerfMonitor.Data.Implementations;
using K2PerfMonitor.Worker.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder(args);

// ---- Serilog structured logging (console + rolling file) ----
builder.Services.AddSerilog(cfg => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {RunId} {Collector} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day,
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {RunId} {Collector} {Message:lj}{NewLine}{Exception}"));

// ---- Options binding + validation (fail fast on startup) ----
builder.Services.AddOptions<ConnectionStringsOptions>()
    .Bind(builder.Configuration.GetSection(ConnectionStringsOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.MonitorDb), "ConnectionStrings:MonitorDb is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.SourceDb), "ConnectionStrings:SourceDb is required")
    .ValidateOnStart();

builder.Services.AddOptions<CollectorScheduleOptions>()
    .Bind(builder.Configuration.GetSection(CollectorScheduleOptions.SectionName))
    .Validate(o => o.ServerStatsIntervalSeconds is > 0 and <= 3600,
        "CollectorSchedule:ServerStatsIntervalSeconds must be 1..3600")
    .ValidateOnStart();

var monitorConn = builder.Configuration.GetConnectionString("MonitorDb")
    ?? throw new InvalidOperationException("ConnectionStrings:MonitorDb is required");

// ---- EF Core — Monitoring DB (factory; collectors/jobs สร้าง context เอง) ----
builder.Services.AddDbContextFactory<MonitorDbContext>(options =>
    options.UseSqlServer(monitorConn));

// ---- Repository + Collectors + Alert engine + Job ----
builder.Services.AddScoped<IMetricRepository, MetricRepository>();
builder.Services.AddSqlCollectors();
builder.Services.AddScoped<IAlertEvaluator, AlertEvaluator>();
builder.Services.AddScoped<CollectorJob>();
builder.Services.AddScoped<RetentionJob>();

// ---- Notifications (Email/Teams/LINE) — ปิดทุกช่องทางโดย default จนกว่าจะตั้ง config ----
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<TeamsOptions>(builder.Configuration.GetSection(TeamsOptions.SectionName));
builder.Services.Configure<LineOptions>(builder.Configuration.GetSection(LineOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<INotificationProvider, EmailNotificationProvider>();
builder.Services.AddSingleton<INotificationProvider, TeamsNotificationProvider>();
builder.Services.AddSingleton<INotificationProvider, LineNotificationProvider>();
builder.Services.AddScoped<IAlertNotifier, AlertNotificationService>();

// ---- Realtime publisher (SignalR client → Web hub) — Null ถ้า SignalR:Enabled=false ----
builder.Services.AddRealtimePublisher(builder.Configuration);

// ---- Hangfire (SQL Server storage) ----
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(monitorConn, new SqlServerStorageOptions
    {
        SchemaName = "HangFire",
        PrepareSchemaIfNecessary = true,
        QueuePollInterval = TimeSpan.FromSeconds(5),
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

// polling ต่ำ เพื่อรองรับ recurring job แบบ sub-minute (เช่น ServerStats 15s)
builder.Services.AddHangfireServer(o =>
{
    o.SchedulePollingInterval = TimeSpan.FromSeconds(5);
    o.WorkerCount = Math.Max(2, Environment.ProcessorCount);
});

var host = builder.Build();

// ---- Apply pending EF migrations on startup ----
using (var scope = host.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MonitorDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    Log.Information("Monitoring DB migrations applied");
}

// ---- Register recurring collector jobs (driven by ICollectorRegistry + CollectorSchedule) ----
using (var scope = host.Services.CreateScope())
{
    var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var registry = scope.ServiceProvider.GetRequiredService<ICollectorRegistry>();

    foreach (var reg in registry.Registrations)
    {
        var type = reg.Type;
        recurring.AddOrUpdate<CollectorJob>(
            reg.JobId,
            job => job.RunAsync(type, CancellationToken.None),
            CronExpr.FromSeconds(reg.IntervalSeconds));
        Log.Information("Registered {JobId} @ every {Interval}s", reg.JobId, reg.IntervalSeconds);
    }

    // ---- Retention job (Phase 2) — ล้างข้อมูลเก่าตาม RetentionDays วันละครั้ง ----
    var schedule = scope.ServiceProvider.GetRequiredService<IOptions<CollectorScheduleOptions>>().Value;
    recurring.AddOrUpdate<RetentionJob>(
        "maintenance:retention",
        job => job.RunAsync(CancellationToken.None),
        Cron.Daily(3)); // 03:00 UTC

    Log.Information("Recurring jobs registered: {Count} collectors + retention (retention {Days}d)",
        registry.Registrations.Count, schedule.RetentionDays);
}

host.Run();
