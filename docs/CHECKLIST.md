# K2 Performance Monitor — Checklist งานย่อยตาม Phase

> คู่มืองานลงมือทำต่อจาก [`ROADMAP.md`](ROADMAP.md) · ติ๊ก `[x]` เมื่อเสร็จ
> สถานะ: ☐ ยังไม่ทำ · 🟡 กำลังทำ · ✅ เสร็จ · Baseline 2026-07-04

---

## Phase 0 — Foundation Hardening  ✅ (2026-07-04)
- [x] เพิ่ม EF Core migration แรก (`InitialCreate`) ครอบทุก entity
- [x] สคริปต์ `db/` — `001_InitialCreate.sql` (idempotent) + seed `AlertRule` 15 ข้อ + README
- [x] เพิ่ม Hangfire + SQL storage เข้า Worker (server) + Web (dashboard `/hangfire`)
- [x] ย้าย orchestration loop → Hangfire recurring job (`collector:ServerStats`, cron จาก schedule)
- [x] Serilog structured logging (console + rolling file) + correlation id (`RunId`) ต่อ run
- [x] เขียน `CollectorRun` audit จริงทุกรอบ (start/end/success/error/elapsed) ใน `CollectorJob`
- [x] Options validation `ValidateOnStart` (Worker + Web) + `/health` endpoint (ตรวจ Monitor DB)
- [x] ตั้ง test project (25 unit tests ผ่าน) + CI build pipeline (`.github/workflows/ci.yml`)
- [x] EF migrate-on-startup ใน Worker · refactor `MetricRepository` → `IDbContextFactory`

> **หมายเหตุ deploy:** โฟลเดอร์นี้ยังไม่ใช่ git repo — CI workflow พร้อมใช้เมื่อ `git init` (หรือย้ายเข้า repo หลัก)

## Phase 1 — SQL Core Collectors  ✅ (2026-08-14)
- [x] `ICollectorRegistry` + per-collector schedule จาก `CollectorSchedule` (Worker ผูก recurring job อัตโนมัติจาก registry)
- [x] **แก้ ServerStats CPU%** จาก heuristic → ค่าจริง (`sys.dm_os_ring_buffers` RING_BUFFER_SCHEDULER_MONITOR — documented source/formula/limits)
- [x] SlowQueryCollector (`sys.dm_exec_query_stats` + threshold, parameterized)
- [x] ExecutionPlanCollector (plan XML สำหรับ top slow queries → ตาราง `ExecutionPlans` ใหม่ + migration)
- [x] WaitStatisticsCollector (`sys.dm_os_wait_stats` — delta + กรอง benign waits)
- [x] BlockingCollector (`sys.dm_exec_requests` blocking chain + blocked/blocking SQL)
- [x] DeadlockCollector (system_health XE ring buffer → parse deadlock graph + dedup)
- [x] IndexCollector (missing + unused index + CREATE/DROP script)
- [x] IoCollector (`sys.dm_io_virtual_file_stats` — delta, stall/op)
- [x] StoredProcedureCollector (`sys.dm_exec_procedure_stats`)
- [x] Delta/baseline snapshot handling สำหรับ DMV สะสม (`DeltaBaseline<T>` + `DeltaMath`, singleton collectors)
- [x] `SqlCollectorBase` — resilience: source ล่ม → Success=false, ไม่ crash worker · SQL injection-safe (parameterized)
- [x] Verify: 60 tests (unit + integration รันจริงบน SQL Server LocalDB — collect→persist→alert)

## Phase 2 — Persistence, Retention & Rollups
- [ ] Index/optimize ตาราง metric เพื่อ dashboard query
- [ ] Retention job (`RetentionDays`) ลบข้อมูลเก่าอัตโนมัติ
- [ ] Rollup job (raw → 5m/1h aggregate)
- [ ] Load test 24–48h — ตรวจขนาด DB + query latency

## Phase 3 — Alert Engine  🟡 (narrow: ServerStats — 2026-07-04)
- [x] Implement `IAlertEvaluator` ใน `K2PerfMonitor.Alerts` (pure `Match` + rule load)
- [x] ประเมิน `AlertRule` ทุกรอบ collect (ผูกใน `CollectorJob.EvaluateAlertsAsync`)
- [x] Dedup (`collector:field:key`) + escalate severity สูงสุด + auto-resolve เมื่อกลับปกติ
- [x] บันทึก `Alert` ถาวร (`UpsertAlertAsync`/`ResolveMissingAsync`/`PurgeOldDataAsync`) + 6 unit tests
- [ ] State machine Acknowledged (UI) + hysteresis กัน flapping (ค้างไว้ Phase 3 เต็ม)
- [ ] ขยายให้ครอบ collector อื่นอัตโนมัติเมื่อเพิ่มใน Phase 1

## Phase 4 — Notifications  🟡 (narrow: ServerStats — 2026-07-04)
- [x] EmailProvider (SMTP, HTML body)
- [x] TeamsProvider (Incoming Webhook / MessageCard + ปุ่ม dashboard)
- [x] LineProvider (LINE Notify Bearer token)
- [x] Routing ตาม `AlertRule.Channels` flags + cooldown (`LastNotifiedAtUtc` + `CooldownMinutes`) + 5 unit tests
- [x] `AlertNotificationService` fan-out + `MarkAlertNotified` · wire ใน `CollectorJob` · config disabled-by-default
- [x] Setup guide: [notifications-setup.md](notifications-setup.md)
- [ ] Notification retry/queue (ตอนนี้ log อย่างเดียวเมื่อ fail) — ค้าง Phase 4 เต็ม
- [ ] E2E จริง 3 ช่องทาง (ต้องตั้ง credential + verify เอง)

## Phase 5 — Real-time (SignalR)
- [ ] Implement `IRealtimePublisher` + SignalR hub
- [ ] push metric ล่าสุด + alert ใหม่ (throttle + backpressure)
- [ ] Web subscribe live — Overview/CPU-RAM auto-update

## Phase 6 — Web: แทน Mock ด้วยข้อมูลจริง
- [ ] SlowQueries  - [ ] ExecutionPlans  - [ ] WaitStats  - [ ] Blockings
- [ ] Deadlocks  - [ ] Indexes  - [ ] Io  - [ ] StoredProcedures  - [ ] Alerts
- [ ] Filter/sort/paging + drill-down + export CSV
- [ ] ลบ `MockDataService` ออก

## Phase 7 — K2-Specific Monitoring
- [ ] **Spike/PoC:** ยืนยันแหล่งข้อมูล K2 (host DB / runtime / API)
- [ ] K2WorkflowCollector (duration, stuck/errored)
- [ ] K2SmartFormCollector (form load)
- [ ] K2SmartObjectCollector (SMO call time)
- [ ] แทน mock 3 หน้า K2 (Workflows/SmartForms/SmartObjects)

## Phase 8 — Auth, RBAC & Multi-Instance
- [ ] Login (user/password) + role Admin/Operator/Viewer
- [ ] จำกัดสิทธิ์ Settings + Alert-rule (Admin only)
- [ ] รองรับหลาย SQL/K2 instance (เลือกดูรายเครื่อง)
- [ ] เข้ารหัส credential (Data Protection) — ห้าม plaintext/log

## Phase 9 — Packaging, Deploy & Observability
- [ ] Dockerfile (web + worker)
- [ ] docker-compose (+ SQL Server)
- [ ] Config ผ่าน env + migrate-on-startup ที่ปลอดภัย
- [ ] Health/readiness probe + self-monitoring
- [ ] เอกสารติดตั้ง/ใช้งาน + backup/restore

## Phase 10 — Hardening & Release
- [ ] Security review (secret, SQL injection surface, XSS)
- [ ] Performance tuning + resilience (source DB ล่ม → ไม่ crash)
- [ ] UAT + release checklist + versioning
