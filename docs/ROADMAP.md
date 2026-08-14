# K2 Performance Monitor — Enterprise Roadmap

> **ระบบ:** K2 Performance Monitor — เฝ้าระวัง + วิเคราะห์ประสิทธิภาพ **SQL Server** และ **K2 (SmartForms / SmartObjects / Workflows)** แบบ real-time พร้อม alert + notification
> **Solution:** `K2PerformanceMonitor.sln` · Clean Architecture (Core / Data / Collectors / Alerts / Notifications / Realtime / Worker / Web)
> **เอกสารนี้คือแผนพัฒนาแม่บท** — ทุก milestone ยึดที่นี่ · รายละเอียดงานย่อยดู [`CHECKLIST.md`](CHECKLIST.md)
> อัปเดตล่าสุด: 2026-07-04

---

## 1. Vision
ระบบ monitoring ระดับ enterprise ที่ให้ทีม DBA / K2 Admin / Ops **เห็นสุขภาพของ SQL Server + K2 platform ได้ทันที** — CPU/RAM, slow queries, wait stats, blocking, deadlock, index, I/O ไปจนถึงประสิทธิภาพ K2 form/workflow — พร้อม **alert อัตโนมัติ** และ **แจ้งเตือนผ่าน Email/Teams/LINE** เมื่อค่าเกิน threshold โดยไม่ต้องนั่งเฝ้าจอ

**Design pillars:** Collect → Store → Evaluate → Notify → Visualize (real-time)

---

## 2. Technology Stack
| ชั้น | เทคโนโลยี |
|---|---|
| Runtime | .NET 10 |
| Architecture | Modular Monolith + Clean Architecture |
| Persistence | **SQL Server** (Monitoring DB) + EF Core (code-first + migrations) |
| Background jobs | **Hangfire** (SQL Server storage) — scheduling collectors, retry, retention |
| Data source | SQL Server DMVs (source DB) + K2 host/runtime tables |
| Real-time | SignalR (live metric push → dashboard) |
| Notifications | Email (SMTP) + Microsoft Teams + LINE |
| Frontend | Blazor Server (InteractiveServer) — Overview + 14 metric pages |
| Deploy | Docker (web + worker + db, compose) |

---

## 3. สถานะปัจจุบัน (Baseline — 2026-07-04)

| Layer | สถานะ | หมายเหตุ |
|---|---|---|
| Solution / โครงสร้าง 8 โปรเจกต์ | ✅ | Clean Architecture วางไว้ครบ |
| Core (interfaces/enums/models/options) | ✅ | ออกแบบ 12 collector types, MetricFields, alert/notification models ครบ |
| Data (DbContext + ~15 entities) | 🟡 | มี entity + repository + seed แต่ **ยังไม่มี migration** (`db/` ว่าง) |
| Collectors | 🟡 **1/12** | มีแค่ `ServerStatsCollector` (CPU/RAM) — CPU% ยังเป็น heuristic ชั่วคราว |
| Worker (orchestration) | 🟡 | `BackgroundService` loop ธรรมดา รันแค่ ServerStats/15s — **ยังไม่ใช้ Hangfire** |
| Alert engine | ❌ | โปรเจกต์ `K2PerfMonitor.Alerts` ว่างเปล่า (มีแค่ interface `IAlertEvaluator`) |
| Notifications | ❌ | `K2PerfMonitor.Notifications` ว่าง (มี options Email/Teams/LINE แต่ไม่มี impl) |
| Realtime (SignalR) | ❌ | `K2PerfMonitor.Realtime` ว่าง (มีแค่ interface `IRealtimePublisher`) |
| Web dashboard | 🟡 **2/15** | Overview + CPU/RAM ใช้ข้อมูลจริง · **อีก 13 หน้าเป็น MOCK DATA** |
| Auth / RBAC | ❌ | ยังไม่มี |
| Tests | ❌ | โปรเจกต์ test ว่าง |
| Docs / Deploy | ❌ | `docs/` ว่าง, ยังไม่มี Dockerfile / compose |

**สรุป:** โครงกระดูก + สัญญา (contracts) ดีเยี่ยม แต่ "เนื้อ" ยังเป็น prototype — ถนนหลักคือ **เติม collector ที่เหลือ, เดิน pipeline persist→alert→notify→realtime ให้ครบ, และแทน mock ด้วยข้อมูลจริงทุกหน้า**

---

## Progress (อัปเดต 2026-08-14 · v0.9.0)

| Phase | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| สถานะ | ✅ | ✅ | ✅ | ✅ | ✅* | ✅ | ✅ | ⛔ | 🟡 | ✅ | ✅* |

✅ complete · 🟡 partial (ระบุใน [PROJECT_STATE](../PROJECT_STATE.md)) · ⛔ blocked external dependency (K2 — ต้อง verify schema จริง)
`*` = มีส่วนที่ต้อง verify ในสภาพแวดล้อมจริง (notification credential / UAT). Build: Release 0 warnings · Tests: 64 passed

## 4. Phases (10 เฟส)

ลำดับออกแบบให้ **เดินได้ทีละชั้นและเห็นผลจริงเร็ว** — ทำ foundation ให้แข็งก่อน แล้วเดิน pipeline end-to-end บน collector เดียว ก่อนขยายไปครบ 12 ตัว

### Phase 0 — Foundation Hardening 🧱
วางฐานให้ทุก phase ต่อยอดได้ ไม่ต้องรื้อทีหลัง
- สร้าง EF Core **migration แรก** + สคริปต์ `db/` (create/seed) — ปิดช่อง "ไม่มี migration"
- ยก Worker ไปใช้ **Hangfire** (SQL storage) + Hangfire Dashboard
- Serilog structured logging + correlation id ต่อ collector run
- `CollectorRun` audit (เริ่ม/จบ/สำเร็จ/error/ระยะเวลา) เขียนจริงทุกรอบ
- Options validation (`ValidateOnStart`) + health check endpoint
- ตั้ง test project จริง (unit + integration harness) + CI build

### Phase 1 — Collector Framework & SQL Core Collectors 🔌
เติม collector ฝั่ง SQL ที่ "ได้ผลเร็ว/ข้อมูลอยู่ใน DMV ทั้งหมด" (ตามที่เลือก: SQL ก่อน)
- `ICollectorRegistry` + per-collector schedule (Hangfire recurring jobs อ่านจาก `CollectorSchedule`)
- **ServerStats:** แก้ CPU% จาก heuristic → ค่าจริง (`sys.dm_os_ring_buffers` / resource pool stats)
- SlowQuery · WaitStatistics · Blocking · Deadlock · Index (missing/unused) · I/O stall · StoredProcedure
- Delta handling (DMV เป็น cumulative → เก็บ snapshot ก่อนหน้าเพื่อคำนวณ per-interval)

### Phase 2 — Persistence, Retention & Rollups 💾
ทำให้เก็บได้นานโดยไม่บวม
- Index/partition ตาราง metric ให้ query dashboard เร็ว
- **Data retention job** (ใช้ `RetentionDays`) — ลบ/ย่อยข้อมูลเก่าอัตโนมัติผ่าน Hangfire
- Rollup/aggregation (1m raw → 5m/1h) สำหรับกราฟย้อนหลัง
- ทดสอบ load: เก็บต่อเนื่อง 24–48 ชม. โดย DB ไม่โต/ช้าผิดปกติ

### Phase 3 — Alert Engine 🚨
- Implement `IAlertEvaluator` ใน `K2PerfMonitor.Alerts`
- อ่าน `AlertRule` (metric field + comparison operator + threshold + severity) มาประเมินกับ `MetricItem` ทุกรอบ collect
- State machine: Firing → Acknowledged → Resolved + de-dupe/hysteresis กัน alert เด้ง
- เก็บ `Alert` ประวัติถาวร + ผูก Hangfire ให้ประเมินหลัง collect เสร็จ

### Phase 4 — Notifications 📣
- Implement `INotificationProvider`: **Email (SMTP) · Microsoft Teams (webhook) · LINE (Messaging/Notify)**
- Routing ตาม severity + channel + throttle/summary (กัน spam)
- Notification log + retry เมื่อส่งล้มเหลว
- ทดสอบ end-to-end: rule เกิน → alert → ส่งจริงเข้า 3 ช่องทาง

### Phase 5 — Real-time Dashboard (SignalR) 📡
- Implement `IRealtimePublisher` + SignalR hub ใน `K2PerfMonitor.Realtime`
- push metric ล่าสุด + alert ใหม่เข้า dashboard (throttle + backpressure — กันภาพหน่วงสะสม)
- Web subscribe live: Overview/CPU-RAM อัปเดตเองไม่ต้องกด Refresh

### Phase 6 — Web Dashboard: แทน Mock ด้วยข้อมูลจริง 📊
เปลี่ยน **13 หน้าที่ยังเป็น mock** → service ที่อ่านจาก Monitoring DB
- SlowQueries · ExecutionPlans · WaitStats · Blockings · Deadlocks · Indexes · Io · StoredProcedures · Alerts
- Filter/sort/paging + drill-down + export (CSV)
- ปลด `MockDataService` ออกเมื่อทุกหน้าเป็นของจริง

### Phase 7 — K2-Specific Monitoring 🔄
ส่วนที่แยกจาก SQL ล้วน — เน้นคุณค่าเฉพาะทางของผลิตภัณฑ์ (ตามที่เลือก: ทำคู่กับ SQL)
- หาแหล่งข้อมูลจริงของ K2 (host server DB / runtime tables / API) — **ต้อง spike/PoC ก่อน** (ดู §6 ความเสี่ยง)
- K2Workflow (duration, stuck/errored instances) · K2SmartForm (form load) · K2SmartObject (SMO call time)
- แทน mock 3 หน้า K2 ด้วยข้อมูลจริง

### Phase 8 — Auth, RBAC & Multi-Instance 🔐
- Login (user/password ของระบบเอง) + role: Admin / Operator / Viewer
- ป้องกัน Settings/Alert-rule management เฉพาะ Admin
- รองรับ **หลาย SQL/K2 instance** (เลือกดูรายเครื่อง) + เข้ารหัส credential (Data Protection) — ห้าม plaintext/ห้าม log

### Phase 9 — Packaging, Deploy & Observability 📦
- Dockerfile (web + worker) + `docker-compose` (+ SQL Server)
- Config ผ่าน env, migration รันตอน startup อย่างปลอดภัย
- Health/readiness probe + metrics ของตัว monitor เอง (self-monitoring)
- เอกสารติดตั้ง/ใช้งาน + backup/restore Monitoring DB

### Phase 10 — Hardening & Release 🚀
- Security review (secret handling, SQL injection surface ใน DMV query, XSS ใน dashboard)
- Performance tuning collector + dashboard, resilience (source DB ล่ม → monitor ไม่ crash)
- UAT + release checklist + versioning

---

## 5. ลำดับส่งมอบที่แนะนำ (Delivery Order)

> หลักการ: **เดิน pipeline ให้ครบวงจรบน collector เดียวก่อน แล้วค่อยขยาย** — จะได้ระบบที่ "ใช้ได้จริง" เร็วที่สุด

1. **Slice A (Vertical MVP):** Phase 0 → เดิน ServerStats ครบวง (Hangfire → persist → alert → Teams/Email → live) = ได้ระบบ monitor CPU/RAM ที่แจ้งเตือนได้จริง
2. **Slice B (SQL breadth):** Phase 1 (collector SQL ที่เหลือ) + Phase 2 (retention/rollup) + Phase 6 บางส่วน (หน้า SQL เป็นจริง)
3. **Slice C (K2 value):** Phase 7 (K2 metrics) — หลัง spike แหล่งข้อมูลผ่าน
4. **Slice D (Enterprise-ready):** Phase 8 (auth/multi-instance) → Phase 9 (deploy) → Phase 10 (release)

Phase 3/4/5 ทำครั้งแรกใน Slice A แบบแคบ (ServerStats) แล้ว "กว้างขึ้นอัตโนมัติ" เมื่อเพิ่ม collector ใน Slice B

---

## 6. ความเสี่ยง & จุดต้องตัดสินใจ (Open Items)

- **CPU% ที่แม่นยำ:** ค่าใน `ServerStatsCollector` ตอนนี้เป็น heuristic (`batch/sec ÷ 10`) — ต้องแทนด้วยแหล่งจริง (ring buffer / resource pool). ⚠️ *ถือเป็นบั๊กที่ต้องแก้ใน Phase 1*
- **แหล่งข้อมูล K2 (Phase 7):** ยังไม่ยืนยันว่าจะดึงจาก K2 host DB, runtime table, หรือ API — **ต้อง PoC ก่อนวางแผนละเอียด** (มีโอกาสเป็นงานหนักสุดและไม่แน่นอนสุด)
- **DMV cumulative vs interval:** หลาย DMV เป็นค่าสะสมตั้งแต่ start — ต้องเก็บ baseline snapshot เพื่อคำนวณ per-interval ให้ถูก (ออกแบบใน Phase 1)
- **สิทธิ์บน source DB:** collector ต้องการสิทธิ์ `VIEW SERVER STATE` — ต้องระบุใน requirement การ deploy
- **ผลกระทบต่อ source:** collector ต้องเบา ไม่ไปเพิ่มโหลดให้ระบบที่กำลังเฝ้า (จำกัด TopN, interval, timeout)

---

## 7. Definition of Done (ต่อ phase)
- โค้ด + unit/integration test ผ่าน · ไม่มี mock หลงเหลือในขอบเขต phase นั้น
- Migration + seed รันสะอาดบน DB เปล่า
- เอกสาร/หน้าจอที่เกี่ยวข้องอัปเดต · demo end-to-end ได้จริง
