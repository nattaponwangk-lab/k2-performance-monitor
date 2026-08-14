IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [AlertRules] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(128) NOT NULL,
        [Enabled] bit NOT NULL,
        [CollectorType] int NOT NULL,
        [MetricField] nvarchar(64) NOT NULL,
        [Operator] int NOT NULL,
        [Threshold] float NOT NULL,
        [Severity] int NOT NULL,
        [CooldownMinutes] int NOT NULL,
        [Channels] int NOT NULL,
        [TitleTemplate] nvarchar(256) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AlertRules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [Alerts] (
        [Id] bigint NOT NULL IDENTITY,
        [RuleId] bigint NULL,
        [CollectorType] int NOT NULL,
        [DedupKey] nvarchar(256) NOT NULL,
        [Severity] int NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Summary] nvarchar(max) NOT NULL,
        [Detail] nvarchar(max) NULL,
        [MetricValue] float NULL,
        [ThresholdValue] float NULL,
        [Status] int NOT NULL,
        [RaisedAtUtc] datetime2 NOT NULL,
        [AcknowledgedAtUtc] datetime2 NULL,
        [ResolvedAtUtc] datetime2 NULL,
        [LastNotifiedAtUtc] datetime2 NULL,
        [NotifyCount] int NOT NULL,
        CONSTRAINT [PK_Alerts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [BlockingEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [BlockedSessionId] int NOT NULL,
        [BlockingSessionId] int NOT NULL,
        [WaitDurationMs] float NOT NULL,
        [WaitType] nvarchar(128) NOT NULL,
        [Resource] nvarchar(512) NULL,
        [RequestedLockMode] nvarchar(16) NULL,
        [BlockedQueryText] nvarchar(max) NULL,
        [BlockingQueryText] nvarchar(max) NULL,
        [BlockedLoginName] nvarchar(256) NULL,
        [BlockingLoginName] nvarchar(256) NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_BlockingEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [CollectorRuns] (
        [Id] bigint NOT NULL IDENTITY,
        [CollectorType] int NOT NULL,
        [DisplayName] nvarchar(128) NOT NULL,
        [StartedAtUtc] datetime2 NOT NULL,
        [FinishedAtUtc] datetime2 NULL,
        [ElapsedMs] float NOT NULL,
        [Success] bit NOT NULL,
        [ItemsCollected] int NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        CONSTRAINT [PK_CollectorRuns] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [DeadlockEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [DeadlockAtUtc] datetime2 NOT NULL,
        [VictimProcessId] nvarchar(128) NOT NULL,
        [VictimQueryText] nvarchar(max) NOT NULL,
        [VictimLoginName] nvarchar(256) NULL,
        [SurvivorQueryText] nvarchar(max) NOT NULL,
        [SurvivorLoginName] nvarchar(256) NULL,
        [DeadlockGraphXml] nvarchar(max) NOT NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_DeadlockEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [IndexRecommendations] (
        [Id] bigint NOT NULL IDENTITY,
        [RecommendationType] nvarchar(16) NOT NULL,
        [DatabaseName] nvarchar(128) NULL,
        [SchemaName] nvarchar(128) NULL,
        [TableName] nvarchar(256) NULL,
        [EqualityColumns] nvarchar(512) NULL,
        [InequalityColumns] nvarchar(512) NULL,
        [IncludedColumns] nvarchar(512) NULL,
        [Impact] float NOT NULL,
        [UserSeeks] bigint NOT NULL,
        [UserScans] bigint NOT NULL,
        [UserLookups] bigint NOT NULL,
        [IndexName] nvarchar(256) NULL,
        [RecommendationScript] nvarchar(max) NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_IndexRecommendations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [IoStats] (
        [Id] bigint NOT NULL IDENTITY,
        [DatabaseName] nvarchar(128) NOT NULL,
        [LogicalFileName] nvarchar(128) NULL,
        [FileType] nvarchar(16) NULL,
        [NumOfReads] bigint NOT NULL,
        [NumOfWrites] bigint NOT NULL,
        [BytesRead] bigint NOT NULL,
        [BytesWritten] bigint NOT NULL,
        [IoStallReadMs] float NOT NULL,
        [IoStallWriteMs] float NOT NULL,
        [IoStallMsPerRead] float NOT NULL,
        [IoStallMsPerWrite] float NOT NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_IoStats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [K2SmartFormStats] (
        [Id] bigint NOT NULL IDENTITY,
        [FormName] nvarchar(256) NULL,
        [FormId] nvarchar(128) NULL,
        [FormLoadMs] float NOT NULL,
        [InitializeRuleMs] float NULL,
        [LoadCount] bigint NOT NULL,
        [AvgLoadMs] float NOT NULL,
        [MaxLoadMs] float NOT NULL,
        [UserName] nvarchar(256) NULL,
        [FormUrl] nvarchar(512) NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_K2SmartFormStats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [K2SmartObjectStats] (
        [Id] bigint NOT NULL IDENTITY,
        [SmartObjectName] nvarchar(256) NULL,
        [Method] nvarchar(64) NULL,
        [ServiceType] nvarchar(128) NULL,
        [DurationMs] float NOT NULL,
        [CallCount] bigint NOT NULL,
        [AvgDurationMs] float NOT NULL,
        [MaxDurationMs] float NOT NULL,
        [RowsReturned] bigint NULL,
        [ErrorMessage] nvarchar(512) NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_K2SmartObjectStats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [K2WorkflowStats] (
        [Id] bigint NOT NULL IDENTITY,
        [ProcSetId] bigint NOT NULL,
        [ProcInstId] bigint NULL,
        [WorkflowName] nvarchar(256) NULL,
        [Folio] nvarchar(256) NULL,
        [Status] nvarchar(32) NOT NULL,
        [DurationMs] float NOT NULL,
        [CurrentActivityWaitMs] float NULL,
        [StartedAtUtc] datetime2 NULL,
        [FinishedAtUtc] datetime2 NULL,
        [Originator] nvarchar(256) NULL,
        [IsStuck] bit NOT NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_K2WorkflowStats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [ServerStats] (
        [Id] bigint NOT NULL IDENTITY,
        [InstanceName] nvarchar(128) NOT NULL,
        [UptimeSeconds] bigint NOT NULL,
        [CpuPercent] float NOT NULL,
        [MemoryPercent] float NOT NULL,
        [UsedMemoryMb] float NOT NULL,
        [AvailableMemoryMb] float NOT NULL,
        [TotalMemoryMb] float NOT NULL,
        [ConnectionCount] int NOT NULL,
        [ActiveRequestCount] int NOT NULL,
        [BatchRequestsPerSec] float NOT NULL,
        [OnlineSchedulerCount] int NOT NULL,
        [BlockedProcessCount] int NOT NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ServerStats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [SlowQueries] (
        [Id] bigint NOT NULL IDENTITY,
        [QueryText] nvarchar(max) NOT NULL,
        [DatabaseName] nvarchar(128) NULL,
        [ObjectName] nvarchar(256) NULL,
        [ExecutionCount] bigint NOT NULL,
        [TotalDurationMs] float NOT NULL,
        [AvgDurationMs] float NOT NULL,
        [MaxDurationMs] float NOT NULL,
        [TotalLogicalReads] float NOT NULL,
        [AvgLogicalReads] float NOT NULL,
        [AvgCpuMs] float NOT NULL,
        [AvgPhysicalReads] float NOT NULL,
        [LastExecutionUtc] datetime2 NULL,
        [PlanHandle] nvarchar(256) NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SlowQueries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [StoredProcedureStats] (
        [Id] bigint NOT NULL IDENTITY,
        [DatabaseName] nvarchar(128) NULL,
        [SchemaName] nvarchar(128) NULL,
        [ObjectName] nvarchar(256) NULL,
        [ObjectId] bigint NOT NULL,
        [ExecutionCount] bigint NOT NULL,
        [TotalElapsedMs] float NOT NULL,
        [AvgElapsedMs] float NOT NULL,
        [MaxElapsedMs] float NOT NULL,
        [TotalWorkerMs] float NOT NULL,
        [AvgWorkerMs] float NOT NULL,
        [TotalLogicalReads] float NOT NULL,
        [AvgLogicalReads] float NOT NULL,
        [TotalPhysicalReads] float NOT NULL,
        [AvgPhysicalReads] float NOT NULL,
        [LastExecutionUtc] datetime2 NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_StoredProcedureStats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE TABLE [WaitStats] (
        [Id] bigint NOT NULL IDENTITY,
        [WaitType] nvarchar(128) NOT NULL,
        [WaitingTasksCount] bigint NOT NULL,
        [WaitTimeMs] float NOT NULL,
        [SignalWaitTimeMs] float NOT NULL,
        [MaxWaitTimeMs] float NOT NULL,
        [WaitPercent] float NOT NULL,
        [IsBenign] bit NOT NULL,
        [CollectedAtUtc] datetime2 NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WaitStats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Channels', N'CollectorType', N'CooldownMinutes', N'CreatedAtUtc', N'Enabled', N'MetricField', N'Name', N'Operator', N'Severity', N'Threshold', N'TitleTemplate') AND [object_id] = OBJECT_ID(N'[AlertRules]'))
        SET IDENTITY_INSERT [AlertRules] ON;
    EXEC(N'INSERT INTO [AlertRules] ([Id], [Channels], [CollectorType], [CooldownMinutes], [CreatedAtUtc], [Enabled], [MetricField], [Name], [Operator], [Severity], [Threshold], [TitleTemplate])
    VALUES (CAST(1 AS bigint), 7, 1, 30, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''AvgDurationMs'', N''Slow Query (avg > 5s)'', 0, 1, 5000.0E0, NULL),
    (CAST(2 AS bigint), 7, 1, 30, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''AvgDurationMs'', N''Slow Query (avg > 15s)'', 0, 2, 15000.0E0, NULL),
    (CAST(3 AS bigint), 7, 9, 30, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''AvgDurationMs'', N''Slow Stored Proc (avg > 5s)'', 0, 1, 5000.0E0, NULL),
    (CAST(4 AS bigint), 7, 3, 60, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''WaitTimeMs'', N''High Wait Time'', 0, 1, 30000.0E0, NULL),
    (CAST(5 AS bigint), 7, 4, 15, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''BlockingDurationMs'', N''Long Blocking (> 30s)'', 0, 1, 30000.0E0, NULL),
    (CAST(6 AS bigint), 7, 4, 15, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''BlockingDurationMs'', N''Severe Blocking (> 120s)'', 0, 2, 120000.0E0, NULL),
    (CAST(7 AS bigint), 7, 8, 5, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''CpuPercent'', N''High CPU (> 80%)'', 0, 1, 80.0E0, NULL),
    (CAST(8 AS bigint), 7, 8, 5, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''CpuPercent'', N''Critical CPU (> 95%)'', 0, 2, 95.0E0, NULL),
    (CAST(9 AS bigint), 7, 8, 5, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''AvailableMemoryMb'', N''Low Memory (< 512MB free)'', 2, 1, 512.0E0, NULL),
    (CAST(10 AS bigint), 7, 8, 5, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''AvailableMemoryMb'', N''Critical Memory (< 128MB free)'', 2, 2, 128.0E0, NULL),
    (CAST(11 AS bigint), 7, 7, 60, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''IoStallMsPerRead'', N''Slow I/O Read (> 20ms/op)'', 0, 1, 20.0E0, NULL),
    (CAST(12 AS bigint), 7, 6, 360, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''MissingIndexImpact'', N''Missing Index (high impact)'', 0, 0, 80.0E0, NULL),
    (CAST(13 AS bigint), 7, 10, 60, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''WorkflowDurationMs'', N''Stuck Workflow (> 24h)'', 0, 1, 86400000.0E0, NULL),
    (CAST(14 AS bigint), 7, 11, 30, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''FormLoadMs'', N''Slow Form Load (> 8s)'', 0, 1, 8000.0E0, NULL),
    (CAST(15 AS bigint), 7, 12, 30, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''SmartObjectCallMs'', N''Slow SmartObject Call (> 5s)'', 0, 1, 5000.0E0, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Channels', N'CollectorType', N'CooldownMinutes', N'CreatedAtUtc', N'Enabled', N'MetricField', N'Name', N'Operator', N'Severity', N'Threshold', N'TitleTemplate') AND [object_id] = OBJECT_ID(N'[AlertRules]'))
        SET IDENTITY_INSERT [AlertRules] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AlertRules_CollectorType_Enabled] ON [AlertRules] ([CollectorType], [Enabled]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_DedupKey_Status] ON [Alerts] ([DedupKey], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_RaisedAtUtc] ON [Alerts] ([RaisedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_Status] ON [Alerts] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BlockingEvents_CollectedAtUtc] ON [BlockingEvents] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BlockingEvents_CollectedAtUtc_BlockedSessionId] ON [BlockingEvents] ([CollectedAtUtc], [BlockedSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CollectorRuns_StartedAtUtc] ON [CollectorRuns] ([StartedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlockEvents_CollectedAtUtc] ON [DeadlockEvents] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlockEvents_DeadlockAtUtc] ON [DeadlockEvents] ([DeadlockAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IndexRecommendations_CollectedAtUtc] ON [IndexRecommendations] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IndexRecommendations_CollectedAtUtc_RecommendationType] ON [IndexRecommendations] ([CollectedAtUtc], [RecommendationType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IoStats_CollectedAtUtc] ON [IoStats] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IoStats_CollectedAtUtc_DatabaseName] ON [IoStats] ([CollectedAtUtc], [DatabaseName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_K2SmartFormStats_CollectedAtUtc] ON [K2SmartFormStats] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_K2SmartFormStats_CollectedAtUtc_FormName] ON [K2SmartFormStats] ([CollectedAtUtc], [FormName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_K2SmartObjectStats_CollectedAtUtc] ON [K2SmartObjectStats] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_K2SmartObjectStats_CollectedAtUtc_SmartObjectName] ON [K2SmartObjectStats] ([CollectedAtUtc], [SmartObjectName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_K2WorkflowStats_CollectedAtUtc] ON [K2WorkflowStats] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_K2WorkflowStats_CollectedAtUtc_Status] ON [K2WorkflowStats] ([CollectedAtUtc], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServerStats_CollectedAtUtc] ON [ServerStats] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SlowQueries_CollectedAtUtc] ON [SlowQueries] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SlowQueries_CollectedAtUtc_SourceKey] ON [SlowQueries] ([CollectedAtUtc], [SourceKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StoredProcedureStats_CollectedAtUtc] ON [StoredProcedureStats] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StoredProcedureStats_CollectedAtUtc_ObjectName] ON [StoredProcedureStats] ([CollectedAtUtc], [ObjectName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WaitStats_CollectedAtUtc] ON [WaitStats] ([CollectedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WaitStats_CollectedAtUtc_WaitType] ON [WaitStats] ([CollectedAtUtc], [WaitType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704061555_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260704061555_InitialCreate', N'9.0.17');
END;

COMMIT;
GO

