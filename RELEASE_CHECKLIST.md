# K2 Performance Monitor — Release Checklist

**Version:** v0.9.0 · **Date:** 2026-08-14 · **Build:** Release (0 warnings) · **Tests:** 64 passed (unit + integration on SQL Server)

## Release gate

| # | Item | สถานะ |
|---|---|---|
| 1 | ROADMAP phases | ✅ 8/10 complete · Phase 7 (K2) blocked-documented · Phase 8 multi-instance collection scoped |
| 2 | CHECKLIST | ✅ required items complete (ยกเว้น blocked/env-dependent ที่ระบุชัด) |
| 3 | No required TODO / placeholder | ✅ |
| 4 | No MockData | ✅ `MockDataService` ลบแล้ว — ทุกหน้าใช้ real data |
| 5 | All SQL collectors implemented | ✅ 9/9 (verified vs LocalDB) |
| 6 | Alert engine | ✅ fire/dedup/escalate/ack/auto-resolve/hysteresis |
| 7 | Notifications | ✅ Email/Teams/LINE + routing/cooldown/retry (live E2E ต้อง credential) |
| 8 | SignalR realtime | ✅ verified end-to-end |
| 9 | Dashboard real data | ✅ 9 SQL pages + Overview + Alerts (verified) |
| 10 | K2 monitoring | ⛔ blocked external dependency — documented + PoC plan |
| 11 | Auth/RBAC | ✅ cookie + PBKDF2 + roles + admin-only (verified) |
| 12 | Multi-instance | 🟡 registry + encrypted creds + UI ✅ · per-instance collection = scoped follow-up |
| 13 | Credential encryption | ✅ Data Protection (instance creds) |
| 14 | Docker + compose | ✅ worker image build verified |
| 15 | SQL Server compose | ✅ |
| 16 | Health/readiness | ✅ /health, /health/live, /health/ready |
| 17 | CI | ✅ restore/build/test + SQL service (integration runs) |
| 18 | Unit tests | ✅ 53 |
| 19 | Integration tests | ✅ 11 (real SQL Server) |
| 20 | Security review | ✅ no XSS/injection/secret-logging (docs/security.md) |
| 21 | Performance/resilience | ✅ TopN/timeout, source-down safe, worker survives |
| 22 | Documentation | ✅ 11 docs + 3 ADR + README |
| 23 | Backup/restore | ✅ documented (incl. DP keys) |
| 24 | Git clean | ✅ |
| 25 | No secrets committed | ✅ DP keys/.env gitignored, appsettings creds empty |
| 26 | Release version | ✅ v0.9.0 |

## Summary

```
Project Status:    ~90% — production-ready core; 2 items documented as blocked/scoped
Version:           v0.9.0
Build:             Release, 0 warnings
Tests:             64 passed (53 unit + 11 integration), 0 failed
Docker:            web + worker images (multi-stage, non-root) + compose (SQL 2022)
Security:          auth/RBAC + PBKDF2 + Data Protection creds + reviewed (no XSS/injection/secret-log)
Known Limitations:
  - K2 monitoring (Phase 7): blocked — needs real K2 instance to verify schema (PoC plan in docs/collectors.md)
  - Multi-instance collection: registry+encryption+UI done; worker per-instance fan-out is scoped follow-up
  - Notification live E2E + 24–48h load test: need real credentials/environment
Deployment:        docker compose up -d --build  (see docs/deployment.md)
```

## Why not 100%

ตามกฎโปรเจกต์ §16/§30/§33 — **ไม่ประกาศ 100%** เพราะยังมี item ที่ต้องพึ่งของจริงนอก repo:
K2 instance (schema verification), real notification credentials, และการทดสอบ load 24–48h ในสภาพแวดล้อมจริง
ส่วนที่เหลือ (core monitoring pipeline, alert, realtime, dashboard, auth, deploy) **complete + verified**
