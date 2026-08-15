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
- [x] DatabaseStatsCollector — **database discovery** (sys.databases + master_files: state/recovery/size/system-vs-user, ไม่ hard-code ชื่อ DB) + หน้า Databases
- [x] Verify: 80 tests (unit + integration รันจริงบน SQL Server — collect→persist→alert, multi-instance isolation, database discovery)

## Phase 2 — Persistence, Retention & Rollups  ✅ (2026-08-14)
- [x] Index/optimize ตาราง metric (index บน CollectedAtUtc + composite ทุกตาราง — มีตั้งแต่ Phase 0/1)
- [x] Retention job (`RetentionDays`) ลบข้อมูลเก่าอัตโนมัติ (ครอบทุกตาราง metric + audit + resolved alerts)
- [x] Rollup job (ServerStats raw → 5m/1h aggregate, idempotent, ทุก 5 นาที + ตาราง `ServerStatRollups` + migration)
- [~] Load test 24–48h — verified pipeline เขียนต่อเนื่องได้ (worker รันจริงบน LocalDB); การทดสอบ 24–48h เต็มต้องรันในสภาพแวดล้อม deploy จริง (ดู docs/troubleshooting)

## Phase 3 — Alert Engine  🟡 (narrow: ServerStats — 2026-07-04)
- [x] Implement `IAlertEvaluator` ใน `K2PerfMonitor.Alerts` (pure `Match` + rule load)
- [x] ประเมิน `AlertRule` ทุกรอบ collect (ผูกใน `CollectorJob.EvaluateAlertsAsync`)
- [x] Dedup (`collector:field:key`) + escalate severity สูงสุด + auto-resolve เมื่อกลับปกติ
- [x] บันทึก `Alert` ถาวร (`UpsertAlertAsync`/`ResolveMissingAsync`/`PurgeOldDataAsync`) + 6 unit tests
- [x] State machine Acknowledged (UI ปุ่ม Ack ในหน้า Alerts) + hysteresis 10% hold-band กัน flapping (+ unit tests)
- [x] ขยายให้ครอบ collector อื่นอัตโนมัติ (evaluator ทำงานกับทุก CollectorResult — Phase 1 collectors ทั้งหมด)

## Phase 4 — Notifications  🟡 (narrow: ServerStats — 2026-07-04)
- [x] EmailProvider (SMTP, HTML body)
- [x] TeamsProvider (Incoming Webhook / MessageCard + ปุ่ม dashboard)
- [x] LineProvider (LINE Notify Bearer token)
- [x] Routing ตาม `AlertRule.Channels` flags + cooldown (`LastNotifiedAtUtc` + `CooldownMinutes`) + 5 unit tests
- [x] `AlertNotificationService` fan-out + `MarkAlertNotified` · wire ใน `CollectorJob` · config disabled-by-default
- [x] Setup guide: [notifications-setup.md](notifications-setup.md)
- [x] Notification retry (exponential backoff 3 ครั้ง/provider + failure logging)
- [~] E2E จริง 3 ช่องทาง — **blocked**: ต้องใช้ credential จริง (Email/Teams/LINE) + verify ในสภาพแวดล้อมจริง (providers + retry unit-tested แล้ว)

## Phase 5 — Real-time (SignalR)  ✅ (2026-08-14)
- [x] Implement `IRealtimePublisher` (SignalR client) + `MonitorHub` (relay) + `NullRealtimePublisher`
- [x] push metric ล่าสุด + alert ใหม่ (throttle 1/s ต่อ collector + best-effort/backpressure — realtime ล้มไม่กระทบ collector)
- [x] Web subscribe live — Overview auto-update (📡 LIVE badge, CPU/mem trend + alert toast)
- [x] Verify: Worker→hub→browser เห็นค่า CPU อัปเดตสด (verified บน LocalDB)

## Phase 6 — Web: แทน Mock ด้วยข้อมูลจริง  ✅ (2026-08-14)
- [x] SlowQueries  - [x] ExecutionPlans  - [x] WaitStats  - [x] Blockings
- [x] Deadlocks  - [x] Indexes  - [x] Io  - [x] StoredProcedures  - [x] Alerts (จริงจาก DB + acknowledge)
- [x] Filter (search + type) / sort (คลิกหัวคอลัมน์) / paging + drill-down (query/plan/deadlock/blocking) + export CSV (ทุกหน้า)
- [x] Loading / Empty / Error / Unavailable states (StatusView) ครบทุกหน้า
- [x] **ลบ `MockDataService` ออกแล้ว** · หน้า K2 3 หน้าแสดงสถานะ "รอ verify K2 source" (ไม่ใช้ mock)
- [x] Verify: รัน Worker เก็บข้อมูลจริงบน LocalDB → เปิด Web เห็น real data (CPU 32%, 240 slow queries, alert จริง)

## Phase 7 — K2-Specific Monitoring  ⛔ BLOCKED (external dependency — 2026-08-14)
> ตามกฎ §16 (STOP AND SPIKE) + §30: **ห้ามเดา schema K2 / ห้ามสร้าง collector จากข้อมูลที่ยังไม่ verify**
> ไม่มี K2 instance จริงใน repo/environment นี้ → บันทึกเป็น blocked external dependency พร้อมแผน PoC (ดู [docs/collectors.md](collectors.md) §K2)
- [⛔] **Spike/PoC:** ต้องมี K2 host/runtime DB หรือ API จริงเพื่อยืนยัน schema (blocker)
- [ ] K2WorkflowCollector (duration, stuck/errored) — ออกแบบ interface ไว้แล้ว, รอ verify source
- [ ] K2SmartFormCollector (form load) — รอ verify source
- [ ] K2SmartObjectCollector (SMO call time) — รอ verify source
- [x] 3 หน้า K2 แสดงสถานะ "รอ verify K2 source" (ไม่ใช้ mock — ตาม §5) แทน mock เดิม

## Phase 8 — Auth, RBAC & Multi-Instance  ✅ (2026-08-15)
- [x] Login (cookie auth, PBKDF2 password hash) + role Admin/Operator/Viewer + seed admin จาก config (ไม่ใช้ default password)
- [x] จำกัดสิทธิ์ Settings + Instances + Hangfire dashboard (Admin only) · global `[Authorize]` + redirect login · verified (302/200 ตาม role)
- [x] เข้ารหัส credential (Data Protection `IDataProtector`) — ไม่ plaintext/ไม่ log · instance registry + admin UI (add/enable/delete)
- [x] **รองรับหลาย SQL/K2 instance จริง** — Worker collect ต่อ instance (Default + registry ที่ enabled, decrypt), **InstanceId บนทุก metric/alert/run**, data isolation (dedup+auto-resolve+query แยกตาม instance), instance selector บน dashboard · verified integration test + compose

## Phase 9 — Packaging, Deploy & Observability  🟡 (2026-08-14)
- [x] Dockerfile (web + worker) — multi-stage, non-root, .NET 9 runtime · **worker image build ผ่าน**
- [x] docker-compose (+ SQL Server 2022) — healthcheck, depends_on, restart policy
- [x] Config ผ่าน env (`ConnectionStrings__*`, `SignalR__*`) + migrate-on-startup ปลอดภัย (Web+Worker, EF migration lock)
- [x] Health/readiness probe (`/health`, `/health/live`, `/health/ready`) + self-monitoring (`CollectorRuns` audit + Serilog)
- [ ] เอกสารติดตั้ง/ใช้งาน + backup/restore (อยู่ใน docs batch — Phase 10)

## Phase 10 — Hardening & Release  🟡 (2026-08-14)
- [x] Security review (secret handling, SQL injection surface, XSS) — ดู [docs/security.md](security.md) (clean: parameterized, no MarkupString, no secret logging)
- [x] Resilience — source ล่ม → collector fail (worker/web ยังทำงาน) · notification/realtime ล้ม best-effort · verified
- [x] Documentation ครบ (11 docs + 3 ADR + README) + backup/restore
- [x] Release checklist ([RELEASE_CHECKLIST.md](../RELEASE_CHECKLIST.md)) + versioning (v0.9.0)
- [~] UAT — ต้องทำในสภาพแวดล้อมจริง (มี smoke + integration + E2E verify บน LocalDB แล้ว)
