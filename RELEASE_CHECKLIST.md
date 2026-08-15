# K2 Performance Monitor — Release Checklist

**Version:** v0.9.0 · **Date:** 2026-08-15 · **Runtime:** .NET 10 · **Build:** Release (0 warnings) · **Tests:** 80 passed (unit + integration on SQL Server) · **Docker:** compose up verified

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
| 12 | Multi-instance | ✅ per-instance collection + InstanceId on every metric + isolation + selector (verified) |
| 12b | Database monitoring | ✅ discovery (sys.databases) + per-DB size/state + Databases page (verified) |
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
Overall Status:    READY FOR UAT / EXTERNAL VALIDATION
Version:           v0.9.0 · .NET 10
Build:             Release, 0 warnings
Tests:             80 passed (unit + integration on real SQL Server), 0 failed
Docker:            web + worker images (multi-stage, non-root) + compose (SQL 2022) — compose up VERIFIED
Security:          auth/RBAC + PBKDF2 + Data Protection creds + reviewed
                   (no XSS/SQL-injection/secret-log; CSV-injection guarded; Newtonsoft.Json vuln patched)
Multi-instance:    COMPLETE — per-instance collection, InstanceId on every metric, isolation, selector
Database scope:    COMPLETE — discovery via sys.databases (no hard-coded names), per-DB size/state
K2:                BLOCKED_EXTERNAL — needs real K2 instance to verify schema (PoC plan documented)
Known Limitations (environment-dependent only):
  - K2 monitoring: BLOCKED_EXTERNAL (Phase 7)
  - Notification live E2E + 24–48h load test + UAT: need real credentials/environment
Deployment:        docker compose up -d --build  (see docs/deployment.md)
```

## Why not literally "100%"

ตามกฎโปรเจกต์ §16/§24/§33 — **ไม่ประกาศ 100%** เพราะเหลือเฉพาะ item ที่ต้องพึ่งของจริงนอก repo:
K2 instance (schema verification, BLOCKED_EXTERNAL), real notification credentials, และ load/UAT ในสภาพแวดล้อมจริง
ทุกอย่างที่ทำได้ใน repo/environment นี้ **COMPLETE + verified** (รวม .NET 10, multi-instance, database discovery, Docker compose)
