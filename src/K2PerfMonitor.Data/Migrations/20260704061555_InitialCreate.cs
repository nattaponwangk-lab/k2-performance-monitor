using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace K2PerfMonitor.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CollectorType = table.Column<int>(type: "int", nullable: false),
                    MetricField = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Operator = table.Column<int>(type: "int", nullable: false),
                    Threshold = table.Column<double>(type: "float", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CooldownMinutes = table.Column<int>(type: "int", nullable: false),
                    Channels = table.Column<int>(type: "int", nullable: false),
                    TitleTemplate = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<long>(type: "bigint", nullable: true),
                    CollectorType = table.Column<int>(type: "int", nullable: false),
                    DedupKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetricValue = table.Column<double>(type: "float", nullable: true),
                    ThresholdValue = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastNotifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotifyCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlockingEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlockedSessionId = table.Column<int>(type: "int", nullable: false),
                    BlockingSessionId = table.Column<int>(type: "int", nullable: false),
                    WaitDurationMs = table.Column<double>(type: "float", nullable: false),
                    WaitType = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RequestedLockMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    BlockedQueryText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockingQueryText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockedLoginName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BlockingLoginName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockingEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectorRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectorType = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ElapsedMs = table.Column<double>(type: "float", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ItemsCollected = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectorRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeadlockEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeadlockAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VictimProcessId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VictimQueryText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VictimLoginName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SurvivorQueryText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SurvivorLoginName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeadlockGraphXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlockEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndexRecommendations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SchemaName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EqualityColumns = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InequalityColumns = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IncludedColumns = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Impact = table.Column<double>(type: "float", nullable: false),
                    UserSeeks = table.Column<long>(type: "bigint", nullable: false),
                    UserScans = table.Column<long>(type: "bigint", nullable: false),
                    UserLookups = table.Column<long>(type: "bigint", nullable: false),
                    IndexName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RecommendationScript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IoStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LogicalFileName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    NumOfReads = table.Column<long>(type: "bigint", nullable: false),
                    NumOfWrites = table.Column<long>(type: "bigint", nullable: false),
                    BytesRead = table.Column<long>(type: "bigint", nullable: false),
                    BytesWritten = table.Column<long>(type: "bigint", nullable: false),
                    IoStallReadMs = table.Column<double>(type: "float", nullable: false),
                    IoStallWriteMs = table.Column<double>(type: "float", nullable: false),
                    IoStallMsPerRead = table.Column<double>(type: "float", nullable: false),
                    IoStallMsPerWrite = table.Column<double>(type: "float", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IoStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "K2SmartFormStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FormId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FormLoadMs = table.Column<double>(type: "float", nullable: false),
                    InitializeRuleMs = table.Column<double>(type: "float", nullable: true),
                    LoadCount = table.Column<long>(type: "bigint", nullable: false),
                    AvgLoadMs = table.Column<double>(type: "float", nullable: false),
                    MaxLoadMs = table.Column<double>(type: "float", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FormUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_K2SmartFormStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "K2SmartObjectStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmartObjectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    CallCount = table.Column<long>(type: "bigint", nullable: false),
                    AvgDurationMs = table.Column<double>(type: "float", nullable: false),
                    MaxDurationMs = table.Column<double>(type: "float", nullable: false),
                    RowsReturned = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_K2SmartObjectStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "K2WorkflowStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcSetId = table.Column<long>(type: "bigint", nullable: false),
                    ProcInstId = table.Column<long>(type: "bigint", nullable: true),
                    WorkflowName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Folio = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    CurrentActivityWaitMs = table.Column<double>(type: "float", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Originator = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsStuck = table.Column<bool>(type: "bit", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_K2WorkflowStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstanceName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UptimeSeconds = table.Column<long>(type: "bigint", nullable: false),
                    CpuPercent = table.Column<double>(type: "float", nullable: false),
                    MemoryPercent = table.Column<double>(type: "float", nullable: false),
                    UsedMemoryMb = table.Column<double>(type: "float", nullable: false),
                    AvailableMemoryMb = table.Column<double>(type: "float", nullable: false),
                    TotalMemoryMb = table.Column<double>(type: "float", nullable: false),
                    ConnectionCount = table.Column<int>(type: "int", nullable: false),
                    ActiveRequestCount = table.Column<int>(type: "int", nullable: false),
                    BatchRequestsPerSec = table.Column<double>(type: "float", nullable: false),
                    OnlineSchedulerCount = table.Column<int>(type: "int", nullable: false),
                    BlockedProcessCount = table.Column<int>(type: "int", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlowQueries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ObjectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    TotalDurationMs = table.Column<double>(type: "float", nullable: false),
                    AvgDurationMs = table.Column<double>(type: "float", nullable: false),
                    MaxDurationMs = table.Column<double>(type: "float", nullable: false),
                    TotalLogicalReads = table.Column<double>(type: "float", nullable: false),
                    AvgLogicalReads = table.Column<double>(type: "float", nullable: false),
                    AvgCpuMs = table.Column<double>(type: "float", nullable: false),
                    AvgPhysicalReads = table.Column<double>(type: "float", nullable: false),
                    LastExecutionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlanHandle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredProcedureStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SchemaName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ObjectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ObjectId = table.Column<long>(type: "bigint", nullable: false),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    TotalElapsedMs = table.Column<double>(type: "float", nullable: false),
                    AvgElapsedMs = table.Column<double>(type: "float", nullable: false),
                    MaxElapsedMs = table.Column<double>(type: "float", nullable: false),
                    TotalWorkerMs = table.Column<double>(type: "float", nullable: false),
                    AvgWorkerMs = table.Column<double>(type: "float", nullable: false),
                    TotalLogicalReads = table.Column<double>(type: "float", nullable: false),
                    AvgLogicalReads = table.Column<double>(type: "float", nullable: false),
                    TotalPhysicalReads = table.Column<double>(type: "float", nullable: false),
                    AvgPhysicalReads = table.Column<double>(type: "float", nullable: false),
                    LastExecutionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredProcedureStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaitStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WaitType = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    WaitingTasksCount = table.Column<long>(type: "bigint", nullable: false),
                    WaitTimeMs = table.Column<double>(type: "float", nullable: false),
                    SignalWaitTimeMs = table.Column<double>(type: "float", nullable: false),
                    MaxWaitTimeMs = table.Column<double>(type: "float", nullable: false),
                    WaitPercent = table.Column<double>(type: "float", nullable: false),
                    IsBenign = table.Column<bool>(type: "bit", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitStats", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AlertRules",
                columns: new[] { "Id", "Channels", "CollectorType", "CooldownMinutes", "CreatedAtUtc", "Enabled", "MetricField", "Name", "Operator", "Severity", "Threshold", "TitleTemplate" },
                values: new object[,]
                {
                    { 1L, 7, 1, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AvgDurationMs", "Slow Query (avg > 5s)", 0, 1, 5000.0, null },
                    { 2L, 7, 1, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AvgDurationMs", "Slow Query (avg > 15s)", 0, 2, 15000.0, null },
                    { 3L, 7, 9, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AvgDurationMs", "Slow Stored Proc (avg > 5s)", 0, 1, 5000.0, null },
                    { 4L, 7, 3, 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WaitTimeMs", "High Wait Time", 0, 1, 30000.0, null },
                    { 5L, 7, 4, 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "BlockingDurationMs", "Long Blocking (> 30s)", 0, 1, 30000.0, null },
                    { 6L, 7, 4, 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "BlockingDurationMs", "Severe Blocking (> 120s)", 0, 2, 120000.0, null },
                    { 7L, 7, 8, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "CpuPercent", "High CPU (> 80%)", 0, 1, 80.0, null },
                    { 8L, 7, 8, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "CpuPercent", "Critical CPU (> 95%)", 0, 2, 95.0, null },
                    { 9L, 7, 8, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AvailableMemoryMb", "Low Memory (< 512MB free)", 2, 1, 512.0, null },
                    { 10L, 7, 8, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "AvailableMemoryMb", "Critical Memory (< 128MB free)", 2, 2, 128.0, null },
                    { 11L, 7, 7, 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "IoStallMsPerRead", "Slow I/O Read (> 20ms/op)", 0, 1, 20.0, null },
                    { 12L, 7, 6, 360, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MissingIndexImpact", "Missing Index (high impact)", 0, 0, 80.0, null },
                    { 13L, 7, 10, 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WorkflowDurationMs", "Stuck Workflow (> 24h)", 0, 1, 86400000.0, null },
                    { 14L, 7, 11, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "FormLoadMs", "Slow Form Load (> 8s)", 0, 1, 8000.0, null },
                    { 15L, 7, 12, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SmartObjectCallMs", "Slow SmartObject Call (> 5s)", 0, 1, 5000.0, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_CollectorType_Enabled",
                table: "AlertRules",
                columns: new[] { "CollectorType", "Enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DedupKey_Status",
                table: "Alerts",
                columns: new[] { "DedupKey", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_RaisedAtUtc",
                table: "Alerts",
                column: "RaisedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Status",
                table: "Alerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BlockingEvents_CollectedAtUtc",
                table: "BlockingEvents",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BlockingEvents_CollectedAtUtc_BlockedSessionId",
                table: "BlockingEvents",
                columns: new[] { "CollectedAtUtc", "BlockedSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectorRuns_StartedAtUtc",
                table: "CollectorRuns",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlockEvents_CollectedAtUtc",
                table: "DeadlockEvents",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlockEvents_DeadlockAtUtc",
                table: "DeadlockEvents",
                column: "DeadlockAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IndexRecommendations_CollectedAtUtc",
                table: "IndexRecommendations",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IndexRecommendations_CollectedAtUtc_RecommendationType",
                table: "IndexRecommendations",
                columns: new[] { "CollectedAtUtc", "RecommendationType" });

            migrationBuilder.CreateIndex(
                name: "IX_IoStats_CollectedAtUtc",
                table: "IoStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IoStats_CollectedAtUtc_DatabaseName",
                table: "IoStats",
                columns: new[] { "CollectedAtUtc", "DatabaseName" });

            migrationBuilder.CreateIndex(
                name: "IX_K2SmartFormStats_CollectedAtUtc",
                table: "K2SmartFormStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_K2SmartFormStats_CollectedAtUtc_FormName",
                table: "K2SmartFormStats",
                columns: new[] { "CollectedAtUtc", "FormName" });

            migrationBuilder.CreateIndex(
                name: "IX_K2SmartObjectStats_CollectedAtUtc",
                table: "K2SmartObjectStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_K2SmartObjectStats_CollectedAtUtc_SmartObjectName",
                table: "K2SmartObjectStats",
                columns: new[] { "CollectedAtUtc", "SmartObjectName" });

            migrationBuilder.CreateIndex(
                name: "IX_K2WorkflowStats_CollectedAtUtc",
                table: "K2WorkflowStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_K2WorkflowStats_CollectedAtUtc_Status",
                table: "K2WorkflowStats",
                columns: new[] { "CollectedAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServerStats_CollectedAtUtc",
                table: "ServerStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SlowQueries_CollectedAtUtc",
                table: "SlowQueries",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SlowQueries_CollectedAtUtc_SourceKey",
                table: "SlowQueries",
                columns: new[] { "CollectedAtUtc", "SourceKey" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredProcedureStats_CollectedAtUtc",
                table: "StoredProcedureStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StoredProcedureStats_CollectedAtUtc_ObjectName",
                table: "StoredProcedureStats",
                columns: new[] { "CollectedAtUtc", "ObjectName" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitStats_CollectedAtUtc",
                table: "WaitStats",
                column: "CollectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WaitStats_CollectedAtUtc_WaitType",
                table: "WaitStats",
                columns: new[] { "CollectedAtUtc", "WaitType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "BlockingEvents");

            migrationBuilder.DropTable(
                name: "CollectorRuns");

            migrationBuilder.DropTable(
                name: "DeadlockEvents");

            migrationBuilder.DropTable(
                name: "IndexRecommendations");

            migrationBuilder.DropTable(
                name: "IoStats");

            migrationBuilder.DropTable(
                name: "K2SmartFormStats");

            migrationBuilder.DropTable(
                name: "K2SmartObjectStats");

            migrationBuilder.DropTable(
                name: "K2WorkflowStats");

            migrationBuilder.DropTable(
                name: "ServerStats");

            migrationBuilder.DropTable(
                name: "SlowQueries");

            migrationBuilder.DropTable(
                name: "StoredProcedureStats");

            migrationBuilder.DropTable(
                name: "WaitStats");
        }
    }
}
