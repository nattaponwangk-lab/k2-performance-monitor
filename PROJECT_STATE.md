# K2 Performance Monitor — Project State

> Living status tracker. ดูแผนแม่บทที่ [docs/ROADMAP.md](docs/ROADMAP.md) · งานย่อยที่ [docs/CHECKLIST.md](docs/CHECKLIST.md) · release ที่ [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)
> อัปเดตล่าสุด: **2026-08-14** · version **v0.9.0**

## Snapshot

| ด้าน | สถานะ |
|---|---|
| Build | ✅ Release, 0 warnings |
| Tests | ✅ 64 passed (53 unit + 11 integration on SQL Server), 0 failed |
| Runtime | .NET 9 (SDK 10) · SQL Server 2025 LocalDB verified · Docker 27.5.1 (worker image built) |
| Pipeline | ✅ Collect→Store→Evaluate→Alert→Notify→Realtime→Visualize — verified end-to-end |

## Phase status

| Phase | สถานะ | หมายเหตุ |
|---|---|---|
| 0 — Foundation | ✅ | migration, Hangfire, Serilog, health, CI, tests |
| 1 — SQL Collectors | ✅ | 9 collectors + real CPU% + delta — verified vs LocalDB |
| 2 — Retention/Rollup | ✅ | retention (all tables) + 5m/1h rollup |
| 3 — Alert Engine | ✅ | fire/dedup/escalate/ack/auto-resolve/hysteresis |
| 4 — Notifications | ✅* | Email/Teams/LINE + routing/cooldown/retry (*live E2E ต้อง credential) |
| 5 — Realtime (SignalR) | ✅ | verified browser↔hub live |
| 6 — Web real data | ✅ | 13 หน้า real data, MockDataService ลบแล้ว |
| 7 — K2 monitoring | ⛔ | **blocked external dependency** — PoC plan documented |
| 8 — Auth/RBAC/Multi-instance | 🟡 | auth/RBAC/encryption ✅ · per-instance collection = scoped follow-up |
| 9 — Deploy/Docker/Health | ✅ | Dockerfiles + compose + health/readiness |
| 10 — Hardening/Release | ✅* | security review + docs + release checklist (*UAT in real env) |

## Known deviations / remaining (honest)

1. **K2 monitoring (Phase 7)** — blocked: ต้องมี K2 instance จริงเพื่อ verify schema (ห้ามเดา schema ตามกฎ §16). Entity/rules/UI/interface พร้อมรองรับ; เหลือ collector หลัง verify. ดู [docs/collectors.md §K2](docs/collectors.md#k2-collectors-phase-7--blocked)
2. **Multi-instance collection** — registry + encrypted credentials + admin UI เสร็จ; การให้ Worker collect ต่อ instance (fan-out + tag `InstanceId` บน metric) เป็น scoped follow-up (ปัจจุบัน collect จาก `SourceDb` ที่ config)
3. **Notification live E2E** — providers + retry unit-tested; ส่งจริง 3 ช่องทางต้อง credential + verify ในสภาพแวดล้อมจริง
4. **Load test 24–48h** — pipeline เขียนต่อเนื่องได้ (verified); การทดสอบยาวต้องรันใน deploy จริง
5. **Target framework** = `net9.0` (ROADMAP ระบุ .NET 10) — SDK 10 build/test ได้ปกติ; retarget เป็น optional

## Environment (verification capability)

- SQL Server 2025 Express (LocalDB) → integration + full pipeline verified locally
- Docker 27.5.1 → worker image build verified; compose พร้อม
