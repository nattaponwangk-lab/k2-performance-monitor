# Database

Monitoring DB (`K2PerfMonitor`) — EF Core code-first + migrations. DB เปล่า → `MigrateAsync` → schema + seed พร้อม

## ตาราง

**Metric tables** (แต่ละ collector) — index บน `CollectedAtUtc` (+ composite) สำหรับ trend + retention:
`ServerStats`, `SlowQueries`, `ExecutionPlans`, `WaitStats`, `BlockingEvents`, `DeadlockEvents`, `IndexRecommendations`, `IoStats`, `StoredProcedureStats`, `K2WorkflowStats`, `K2SmartFormStats`, `K2SmartObjectStats`

**Aggregate:** `ServerStatRollups` (5m/1h bucket, unique `(BucketMinutes, BucketStartUtc)`)

**Alerting/system:** `Alerts` (index `DedupKey,Status` / `RaisedAtUtc` / `Status`), `AlertRules` (seed 15 rules), `CollectorRuns` (audit)

**Auth/multi-instance:** `Users` (unique `Username`), `MonitoredInstances` (unique `Name`, connection string เข้ารหัส)

**Hangfire:** schema `HangFire` (สร้างอัตโนมัติ)

## Migrations

```bash
# เพิ่ม migration
dotnet ef migrations add <Name> --project src/K2PerfMonitor.Data --startup-project src/K2PerfMonitor.Data
# apply เอง (ปกติ apply ตอน startup)
dotnet ef database update --project src/K2PerfMonitor.Data --startup-project src/K2PerfMonitor.Data --connection "<conn>"
```

migration ปัจจุบัน: `InitialCreate`, `AddExecutionPlans`, `AddServerStatRollups`, `AddUsersAndInstances`

## Retention & rollup

- **RetentionJob** (รายวัน 03:00 UTC) — ลบ metric/audit/resolved-alert ที่เก่ากว่า `RetentionDays` ทุกตาราง
- **RollupJob** (ทุก 5 นาที) — ย่อ `ServerStats` raw → 5m/1h (idempotent upsert)

## High-write design

- metric table เขียน insert-only ต่อ collect cycle, index เฉพาะที่จำเป็น (CollectedAtUtc)
- อ่าน dashboard = "รอบล่าสุด" (rows ที่ `CollectedAtUtc == MAX`) → เร็ว
- retention กัน DB บวม; rollup รองรับกราฟย้อนหลังยาวโดยไม่อ่าน raw ทั้งหมด

## seed

`AlertRuleSeed` seed 15 rules (CPU/Mem/SlowQuery/Wait/Blocking/IO/Index/K2) ผ่าน `HasData` (idempotent ใน migration)
