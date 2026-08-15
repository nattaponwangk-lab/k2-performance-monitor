# K2 Performance Monitor — Project State

> Living status tracker. ดูแผนแม่บทที่ [docs/ROADMAP.md](docs/ROADMAP.md) · งานย่อยที่ [docs/CHECKLIST.md](docs/CHECKLIST.md) · release ที่ [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)
> อัปเดตล่าสุด: **2026-08-15** · version **v0.9.0**

## Snapshot

| ด้าน | สถานะ |
|---|---|
| Build | ✅ Release, 0 warnings, **.NET 10** |
| Tests | ✅ 80 passed (unit + integration on SQL Server), 0 failed |
| Runtime | **.NET 10** (SDK 10.0.100) · SQL Server 2025 LocalDB + SQL 2022 (Docker) verified · Docker compose up verified |
| Pipeline | ✅ Collect→Store→Evaluate→Alert→Notify→Realtime→Visualize — verified end-to-end (single + multi-instance) |

## Phase status

| Phase | สถานะ | หมายเหตุ |
|---|---|---|
| 0 — Foundation | ✅ | migration, Hangfire, Serilog, health, CI, tests |
| 1 — SQL Collectors | ✅ | 9 collectors + DatabaseStats (discovery) + real CPU% + delta — verified vs SQL |
| 2 — Retention/Rollup | ✅ | retention (all tables) + 5m/1h rollup |
| 3 — Alert Engine | ✅ | fire/dedup/escalate/ack/auto-resolve/hysteresis (per-instance) |
| 4 — Notifications | ✅* | Email/Teams/LINE + routing/cooldown/retry (*live E2E ต้อง credential) |
| 5 — Realtime (SignalR) | ✅ | verified browser↔hub live |
| 6 — Web real data | ✅ | ทุกหน้า real data + Databases page, MockDataService ลบแล้ว |
| 7 — K2 monitoring | ⛔ | **blocked external dependency** — PoC plan documented |
| 8 — Auth/RBAC/Multi-instance | ✅ | auth/RBAC/encryption + **multi-instance collection + InstanceId isolation + selector** |
| 9 — Deploy/Docker/Health | ✅ | Dockerfiles + compose (**compose up verified**) + health/readiness |
| 10 — Hardening/Release | ✅* | .NET 10, security review (+CSV injection), docs, release checklist (*UAT in real env) |

## Remaining (honest — external/environment-dependent only)

1. **K2 monitoring (Phase 7)** — BLOCKED_EXTERNAL: ต้องมี K2 instance จริงเพื่อ verify schema (ห้ามเดา schema ตามกฎ §16). Entity/rules/UI/interface + instance registry พร้อมรองรับ; เหลือ collector หลัง verify. ดู [docs/collectors.md §K2](docs/collectors.md#k2-collectors-phase-7--blocked)
2. **Notification live E2E** — providers + retry unit-tested; ส่งจริง 3 ช่องทางต้อง credential + verify ในสภาพแวดล้อมจริง
3. **Load test 24–48h + UAT** — pipeline เขียนต่อเนื่องได้ (verified single+multi-instance); การทดสอบยาว/UAT ต้องรันใน deploy จริง

## Environment (verification capability)

- SQL Server 2025 Express (LocalDB) → integration + full pipeline verified locally
- Docker 27.5.1 → worker image build verified; compose พร้อม
