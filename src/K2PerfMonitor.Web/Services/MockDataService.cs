using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Web.Models;

namespace K2PerfMonitor.Web.Services;

/// <summary>
/// Mock data service — สร้างข้อมูลจำลองสำหรับ dashboard
/// Phase ถัดไปจะแทนที่ด้วย service ที่ดึงจาก Monitoring DB (เขียนโดย Worker)
/// </summary>
public class MockDataService
{
    private static readonly string[] SampleDbNames = { "K2", "K2SmartBox", "K2App_HR", "K2App_Finance", "K2App_Leave" };
    private static readonly string[] SampleUsers = { "somchai", "suda.p", "admin", "k2.service", "wichai.k", "naree.t" };
    private static readonly string[] SampleForms = { "Leave Request Form", "Expense Claim Form", "Approval Form", "Onboarding Form", "Purchase Requisition" };
    private static readonly string[] SampleWorkflows = { "Leave Approval Workflow", "Expense Claim Process", "Purchase Order Approval", "Onboarding Process", "Document Review" };
    private static readonly string[] SampleSmartObjects = { "LeaveRequests_SMO", "Employees_SMO", "Approvals_SMO", "Expenses_SMO", "Documents_SMO" };

    /// <summary>สร้างข้อมูล Overview พร้อม history chart</summary>
    public OverviewVm GetOverview()
    {
        var rng = Random.Shared;
        var now = DateTime.UtcNow;
        var cpu = Math.Round(rng.NextDouble() * 70 + 10, 1);       // 10-80%
        var mem = Math.Round(rng.NextDouble() * 60 + 25, 1);        // 25-85%
        var totalMem = 16384.0;                                      // สมมติ 16GB
        var availMem = Math.Round(totalMem - (totalMem * mem / 100), 0);

        return new OverviewVm
        {
            HealthScore = Math.Round(100 - (cpu * 0.3 + mem * 0.3), 1),
            CpuPercent = cpu,
            MemoryPercent = mem,
            AvailableMemoryMb = availMem,
            UsedMemoryMb = Math.Round(totalMem - availMem, 0),
            TotalMemoryMb = totalMem,
            ConnectionCount = rng.Next(40, 180),
            ActiveRequestCount = rng.Next(1, 15),
            BlockedProcessCount = rng.Next(0, 4),
            BatchRequestsPerSec = Math.Round(rng.NextDouble() * 500 + 50, 0),
            UptimeSeconds = rng.Next(86400, 86400 * 60),
            OnlineSchedulerCount = rng.Next(4, 17),
            ActiveAlertCount = rng.Next(2, 9),
            CriticalAlertCount = rng.Next(0, 3),
            CpuHistory = GenSeries(now, 30, 10, 80),
            MemoryHistory = GenSeries(now, 30, 25, 85)
        };
    }

    public List<SlowQueryVm> GetSlowQueries()
    {
        var rng = Random.Shared;
        var list = new List<SlowQueryVm>();
        var samples = new[]
        {
            ("SELECT TOP 1000 * FROM [Server].[ProcInst] pi JOIN [Server].[ActivityInst] ai ON pi.ID = ai.ProcInstID ORDER BY pi.StartDate DESC", "Server.ProcInst", "K2"),
            ("exec sp_LoadLeaveRequests @Folio, @Status", "sp_LoadLeaveRequests", "K2App_Leave"),
            ("SELECT * FROM SmartBox.._LeaveRequest WHERE Status='Pending'", "_LeaveRequest", "K2SmartBox"),
            ("UPDATE Expenses SET Status=@S, ApprovedBy=@U, ApprovedDate=GETUTCDATE() WHERE Id IN (...)", "usp_UpdateExpenseStatus", "K2App_Finance"),
            ("SELECT COUNT(*) FROM [Server].[ProcInst] WHERE StartDate > DATEADD(d,-30,GETUTCDATE())", "Server.ProcInst", "K2")
        };

        foreach (var (text, obj, db) in samples)
        {
            var avg = rng.NextDouble() * 18000 + 800;
            list.Add(new SlowQueryVm
            {
                QueryHash = Guid.NewGuid().ToString("N")[..12],
                QueryText = text,
                DatabaseName = db,
                ObjectName = obj,
                ExecutionCount = rng.Next(5, 2000),
                AvgDurationMs = Math.Round(avg, 1),
                MaxDurationMs = Math.Round(avg * (1.5 + rng.NextDouble() * 2.5), 1),
                TotalDurationMs = Math.Round(avg * rng.Next(50, 500), 0),
                AvgLogicalReads = rng.Next(1000, 500000),
                AvgCpuMs = Math.Round(avg * 0.6, 1),
                LastExecutionUtc = DateTime.UtcNow.AddSeconds(-rng.Next(5, 3600)),
                Severity = avg > 10000 ? Severity.Critical : avg > 5000 ? Severity.Warning : Severity.Info
            });
        }
        return list.OrderByDescending(q => q.AvgDurationMs).ToList();
    }

    public List<WaitStatVm> GetWaitStats()
    {
        var rng = Random.Shared;
        var waits = new[]
        {
            ("PAGEIOLATCH_SH", "I/O", false),
            ("PAGEIOLATCH_EX", "I/O", false),
            ("LCK_M_S", "Lock", false),
            ("LCK_M_IS", "Lock", false),
            ("CXPACKET", "Parallelism", false),
            ("ASYNC_NETWORK_IO", "Network/Client", false),
            ("SOS_SCHEDULER_YIELD", "CPU", false),
            ("WRITELOG", "Transaction Log", false),
            ("SLEEP_TASK", "Idle", true),
            ("SQLTRACE_BUFFER_FLUSH", "Trace", true)
        };

        var total = rng.Next(50000, 200000);
        var list = new List<WaitStatVm>();
        // แจกสัดส่วนให้ wait type แรกใหญ่สุด
        var weights = new[] { 0.28, 0.15, 0.12, 0.06, 0.10, 0.08, 0.07, 0.05, 0.05, 0.04 };
        for (var i = 0; i < waits.Length; i++)
        {
            var (name, cat, benign) = waits[i];
            var wt = total * weights[i];
            list.Add(new WaitStatVm
            {
                WaitType = name,
                Category = cat,
                WaitingTasksCount = rng.Next(100, 5000),
                WaitTimeMs = Math.Round(wt, 0),
                SignalWaitTimeMs = Math.Round(wt * (0.05 + rng.NextDouble() * 0.35), 0),
                MaxWaitTimeMs = Math.Round(wt / 100 * (2 + rng.NextDouble() * 6), 0),
                WaitPercent = Math.Round(weights[i] * 100, 1),
                IsBenign = benign
            });
        }
        return list.OrderByDescending(w => w.WaitTimeMs).ToList();
    }

    public List<BlockingVm> GetBlocking()
    {
        var rng = Random.Shared;
        var count = rng.Next(0, 4);
        var list = new List<BlockingVm>();
        for (var i = 0; i < count; i++)
        {
            var dur = rng.NextDouble() * 150000 + 5000;
            list.Add(new BlockingVm
            {
                BlockedSessionId = 50 + i * 3,
                BlockingSessionId = 70 + i,
                WaitDurationMs = Math.Round(dur, 0),
                WaitType = rng.Next(2) == 0 ? "LCK_M_S" : "LCK_M_X",
                Resource = $"KEY: {rng.Next(5, 15)}:{rng.Next(720575940, 720575950)}",
                RequestedLockMode = rng.Next(2) == 0 ? "S" : "X",
                BlockedQueryText = "UPDATE [Server].[ProcInst] SET Status=@S WHERE ID=@ID",
                BlockingQueryText = "SELECT * FROM [Server].[ActivityInst] WHERE ProcInstID IN (...)",
                BlockedLoginName = SampleUsers[rng.Next(SampleUsers.Length)],
                BlockingLoginName = SampleUsers[rng.Next(SampleUsers.Length)],
                Severity = dur > 60000 ? Severity.Critical : dur > 30000 ? Severity.Warning : Severity.Info
            });
        }
        return list;
    }

    public List<DeadlockVm> GetDeadlocks()
    {
        var rng = Random.Shared;
        var count = rng.Next(0, 3);
        var list = new List<DeadlockVm>();
        for (var i = 0; i < count; i++)
        {
            list.Add(new DeadlockVm
            {
                DeadlockAtUtc = DateTime.UtcNow.AddMinutes(-rng.Next(2, 1440)),
                VictimProcessId = $"process{rng.Next(1000, 9999)}",
                VictimQueryText = "UPDATE Expenses SET Status='Pending' WHERE BatchId=@B",
                SurvivorQueryText = "DELETE FROM Expenses WHERE Status='Cancelled' AND CreatedDate < @D",
                VictimLoginName = SampleUsers[rng.Next(SampleUsers.Length)],
                SurvivorLoginName = SampleUsers[rng.Next(SampleUsers.Length)]
            });
        }
        return list.OrderByDescending(d => d.DeadlockAtUtc).ToList();
    }

    public List<IndexRecommendationVm> GetIndexRecommendations()
    {
        var rng = Random.Shared;
        var list = new List<IndexRecommendationVm>();
        // Missing indexes
        var tables = new[] { ("Server", "ProcInst"), ("Server", "ActivityInst"), ("dbo", "Expenses"), ("dbo", "LeaveRequest"), ("dbo", "ApprovalHistory") };
        foreach (var (schema, tbl) in tables)
        {
            if (rng.Next(2) == 0) continue;
            var impact = rng.NextDouble() * 40 + 55;
            list.Add(new IndexRecommendationVm
            {
                RecommendationType = "Missing",
                DatabaseName = SampleDbNames[rng.Next(SampleDbNames.Length)],
                TableName = $"{schema}.{tbl}",
                EqualityColumns = rng.Next(2) == 0 ? "[Status], [ProcSetId]" : "[Folio]",
                IncludedColumns = "[StartDate], [Originator], [Folio]",
                Impact = Math.Round(impact, 1),
                UserSeeks = rng.Next(100, 50000),
                UserScans = rng.Next(0, 500),
                RecommendationScript = $"CREATE NONCLUSTERED INDEX IX_{tbl}_Status ON [{schema}].[{tbl}] ([Status]) INCLUDE ([Folio], [StartDate]);"
            });
        }
        // Unused indexes
        for (var i = 0; i < 2; i++)
        {
            var (schema, tbl) = tables[rng.Next(tables.Length)];
            list.Add(new IndexRecommendationVm
            {
                RecommendationType = "Unused",
                DatabaseName = SampleDbNames[rng.Next(SampleDbNames.Length)],
                TableName = $"{schema}.{tbl}",
                IndexName = $"IX_OldBackup_{rng.Next(1, 9)}",
                UserSeeks = 0,
                UserScans = 0,
                Impact = 0,
                RecommendationScript = $"-- DROP INDEX [IX_OldBackup_{rng.Next(1, 9)}] ON [{schema}].[{tbl}]"
            });
        }
        return list;
    }

    public List<IoStatVm> GetIoStats()
    {
        var rng = Random.Shared;
        var list = new List<IoStatVm>();
        foreach (var db in SampleDbNames)
        {
            var readStall = rng.NextDouble() * 35;
            list.Add(new IoStatVm
            {
                DatabaseName = db,
                LogicalFileName = db + "_Data",
                FileType = "ROWS",
                NumOfReads = rng.Next(1000, 500000),
                NumOfWrites = rng.Next(500, 200000),
                IoStallMsPerRead = Math.Round(readStall, 2),
                IoStallMsPerWrite = Math.Round(rng.NextDouble() * 20, 2),
                Severity = readStall > 20 ? Severity.Warning : Severity.Info
            });
        }
        return list.OrderByDescending(x => x.IoStallMsPerRead).ToList();
    }

    public List<StoredProcedureVm> GetStoredProcedures()
    {
        var rng = Random.Shared;
        var sps = new[] { "usp_LoadLeaveRequests", "usp_ProcessExpenseBatch", "usp_GetPendingApprovals", "usp_UpdateWorkflowStatus", "usp_ArchiveOldInstances", "usp_GetDashboardStats" };
        return sps.Select(sp =>
        {
            var avg = rng.NextDouble() * 12000 + 1000;
            return new StoredProcedureVm
            {
                DatabaseName = SampleDbNames[rng.Next(SampleDbNames.Length)],
                ObjectName = sp,
                ExecutionCount = rng.Next(100, 50000),
                AvgElapsedMs = Math.Round(avg, 1),
                MaxElapsedMs = Math.Round(avg * (1.5 + rng.NextDouble() * 2.5), 1),
                AvgLogicalReads = rng.Next(500, 200000),
                LastExecutionUtc = DateTime.UtcNow.AddSeconds(-rng.Next(5, 7200)),
                Severity = avg > 10000 ? Severity.Critical : avg > 5000 ? Severity.Warning : Severity.Info
            };
        }).OrderByDescending(x => x.AvgElapsedMs).ToList();
    }

    public List<K2WorkflowVm> GetK2Workflows()
    {
        var rng = Random.Shared;
        var list = new List<K2WorkflowVm>();
        // ที่ค้าง/stuck
        for (var i = 0; i < 3; i++)
        {
            var durHours = rng.NextDouble() * 30 + 20;
            list.Add(new K2WorkflowVm
            {
                ProcSetId = rng.Next(100, 999),
                ProcInstId = rng.Next(10000, 99999),
                WorkflowName = SampleWorkflows[rng.Next(SampleWorkflows.Length)],
                Folio = $"LVR_{rng.Next(10000, 99999)}",
                Status = "Running",
                DurationMs = durHours * 3600 * 1000,
                CurrentActivityWaitMs = durHours * 3600 * 1000 * 0.8,
                StartedAtUtc = DateTime.UtcNow.AddHours(-durHours),
                Originator = SampleUsers[rng.Next(SampleUsers.Length)],
                IsStuck = true,
                Severity = durHours > 24 ? Severity.Critical : Severity.Warning
            });
        }
        // ที่เสร็จช้า
        for (var i = 0; i < 5; i++)
        {
            var durMin = rng.NextDouble() * 120 + 5;
            list.Add(new K2WorkflowVm
            {
                ProcSetId = rng.Next(100, 999),
                ProcInstId = rng.Next(10000, 99999),
                WorkflowName = SampleWorkflows[rng.Next(SampleWorkflows.Length)],
                Folio = $"EXP_{rng.Next(10000, 99999)}",
                Status = "Completed",
                DurationMs = durMin * 60 * 1000,
                StartedAtUtc = DateTime.UtcNow.AddHours(-rng.NextDouble() * 48),
                FinishedAtUtc = DateTime.UtcNow.AddMinutes(-rng.Next(1, 300)),
                Originator = SampleUsers[rng.Next(SampleUsers.Length)],
                IsStuck = false,
                Severity = durMin > 60 ? Severity.Warning : Severity.Info
            });
        }
        return list.OrderByDescending(w => w.DurationMs).ToList();
    }

    public List<K2SmartFormVm> GetK2SmartForms()
    {
        var rng = Random.Shared;
        return SampleForms.Select(f =>
        {
            var load = rng.NextDouble() * 12000 + 1500;
            return new K2SmartFormVm
            {
                FormName = f,
                FormId = Guid.NewGuid().ToString(),
                FormLoadMs = Math.Round(load, 0),
                InitializeRuleMs = Math.Round(load * 0.7, 0),
                LoadCount = rng.Next(10, 2000),
                AvgLoadMs = Math.Round(load * 0.6, 0),
                MaxLoadMs = Math.Round(load * 1.8, 0),
                FormUrl = $"https://k2app/forms/{f.Replace(' ', '_')}",
                Severity = load > 8000 ? Severity.Warning : Severity.Info
            };
        }).OrderByDescending(x => x.FormLoadMs).ToList();
    }

    public List<K2SmartObjectVm> GetK2SmartObjects()
    {
        var rng = Random.Shared;
        var methods = new[] { "List", "Read", "Save", "Delete", "Execute" };
        var services = new[] { "SQL Server", "SharePoint", "Active Directory", "CRM", "SmartBox" };
        return SampleSmartObjects.SelectMany(smo =>
        {
            var m = methods[rng.Next(methods.Length)];
            var dur = rng.NextDouble() * 8000 + 500;
            return new[] { new K2SmartObjectVm
            {
                SmartObjectName = smo,
                Method = m,
                ServiceType = services[rng.Next(services.Length)],
                DurationMs = Math.Round(dur, 0),
                CallCount = rng.Next(50, 10000),
                AvgDurationMs = Math.Round(dur * 0.5, 0),
                MaxDurationMs = Math.Round(dur * 3, 0),
                RowsReturned = m == "List" ? rng.Next(0, 5000) : null,
                Severity = dur > 5000 ? Severity.Warning : Severity.Info
            }};
        }).OrderByDescending(x => x.DurationMs).ToList();
    }

    public List<Alert> GetAlerts()
    {
        var rng = Random.Shared;
        var list = new List<Alert>();
        var types = new[]
        {
            (CollectorType.SlowQuery, "Slow Query Detected", "Query usp_ProcessExpenseBatch avg 18.2s", Severity.Critical, 5),
            (CollectorType.ServerStats, "High CPU Usage", "CPU at 94% for 3 min", Severity.Critical, 8),
            (CollectorType.Blocking, "Blocking Chain", "Session 53 blocked 92s by session 71", Severity.Warning, 15),
            (CollectorType.K2Workflow, "Stuck Workflow", "Leave Approval stuck 28h on activity 'Manager Approval'", Severity.Warning, 25),
            (CollectorType.K2SmartForm, "Slow Form Load", "Expense Claim Form load 11.5s", Severity.Warning, 35),
            (CollectorType.WaitStatistics, "High Lock Waits", "LCK_M_S waits 45% of total wait time", Severity.Warning, 40),
            (CollectorType.Io, "Slow I/O", "K2 database read stall 24ms/op", Severity.Info, 55)
        };
        foreach (var (col, title, summary, sev, minAgo) in types)
        {
            if (rng.Next(3) == 0) continue; // สุ่มซ่อนบางตัว
            list.Add(new Alert
            {
                Id = rng.Next(1000, 9999),
                CollectorType = col,
                DedupKey = $"mock-{col}-{rng.Next(100)}",
                Severity = sev,
                Title = title,
                Summary = summary,
                Status = AlertStatus.New,
                RaisedAtUtc = DateTime.UtcNow.AddMinutes(-minAgo),
                NotifyCount = rng.Next(0, 3)
            });
        }
        return list.OrderByDescending(a => a.Severity).ThenByDescending(a => a.RaisedAtUtc).ToList();
    }

    private static List<ChartPoint> GenSeries(DateTime now, int points, double min, double max)
    {
        var rng = Random.Shared;
        var list = new List<ChartPoint>();
        var val = (min + max) / 2;
        for (var i = points; i >= 0; i--)
        {
            val = Math.Clamp(val + (rng.NextDouble() - 0.5) * (max - min) * 0.2, min, max);
            list.Add(new ChartPoint { Time = now.AddMinutes(-i), Value = Math.Round(val, 1) });
        }
        return list;
    }
}
