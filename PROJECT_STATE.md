# K2 Performance Monitor — Project State

> Living status tracker. อัปเดตทุก phase gate. ดูแผนแม่บทที่ [docs/ROADMAP.md](docs/ROADMAP.md) · งานย่อยที่ [docs/CHECKLIST.md](docs/CHECKLIST.md)
> อัปเดตล่าสุด: **2026-08-14**

## Snapshot

| ด้าน | สถานะ |
|---|---|
| Build | ✅ solution build clean (0 warnings) |
| Tests | ✅ 60 passed (unit + integration บน SQL Server LocalDB) |
| Runtime | .NET 9 (SDK 10.0.100) · SQL Server 2025 LocalDB verified · Docker 27.5.1 |

## Phase status

| Phase | สถานะ | หมายเหตุ |
|---|---|---|
| 0 — Foundation | ✅ | migration, Hangfire, Serilog, health, CI, tests |
| 1 — SQL Collectors | ✅ | 9 SQL collectors + framework + real CPU% + delta — **verified vs LocalDB** |
| 2 — Retention/Rollup | 🟡 | retention job + purge-all-tables ✅ · rollup + load test ค้าง |
| 3 — Alert Engine | 🟡 | firing/dedup/auto-resolve/escalate ✅ · ack UI + hysteresis ค้าง |
| 4 — Notifications | 🟡 | Email/Teams/LINE providers + routing/cooldown ✅ · retry/queue ค้าง · live E2E ต้อง credential |
| 5 — Realtime (SignalR) | ☐ | |
| 6 — Web real data | ☐ | 13 หน้ายัง mock |
| 7 — K2 monitoring | ☐ | **blocked: ต้อง verify K2 schema จริง** (PoC) |
| 8 — Auth/RBAC/Multi-instance | ☐ | |
| 9 — Deploy/Docker/Observability | ☐ | |
| 10 — Hardening/Release | ☐ | |

## Environment notes (verification capability)

- **SQL Server 2025 Express (LocalDB `MSSQLLocalDB`)** พร้อมใช้ → integration test รัน collect→persist→alert ได้จริง
- **Docker 27.5.1** พร้อม → compose + SQL container ทดสอบได้ (Phase 9)
- Blockers ที่ต้องการของจริง: K2 instance (Phase 7), credential Email/Teams/LINE (Phase 4 live E2E)

## Known deviations from ROADMAP

- Target framework = `net9.0` (ROADMAP ระบุ .NET 10). SDK 10 build/test ได้ปกติ; retarget เป็น optional (ทำท้ายโปรเจกต์เพื่อลด churn)
