# Deployment

## Docker Compose (แนะนำ)

```bash
cp .env.example .env          # ตั้ง MSSQL_SA_PASSWORD + ADMIN_INITIAL_PASSWORD
docker compose up -d --build
```

Compose รัน 3 service:
- `sqlserver` (SQL Server 2022, volume `mssql-data`, healthcheck)
- `worker` (collectors + Hangfire) — start หลัง SQL healthy
- `web` (dashboard :8080) — start หลัง SQL healthy, volume `dp-keys` เก็บ Data Protection keys

เปิด http://localhost:8080 → login `admin` / รหัสใน `.env`

> **Demo:** `SourceDb` ชี้ SQL Server ตัวเดียวกัน (self-monitoring). Production: เปลี่ยน `ConnectionStrings__SourceDb` ให้ชี้ SQL Server เป้าหมาย (login read-only + `VIEW SERVER STATE`)

## Build image แยก

```bash
docker build -f Dockerfile.web    -t k2pm-web:latest .
docker build -f Dockerfile.worker -t k2pm-worker:latest .
```
ทั้งคู่ multi-stage (SDK build → ASP.NET runtime), รันแบบ **non-root**

## Migration on startup

Web และ Worker ทำ `Database.MigrateAsync()` ตอน start (idempotent, EF ใช้ migration lock กัน concurrent)
→ DB เปล่า → start → schema + seed พร้อมใช้ อัตโนมัติ

## Health / readiness (สำหรับ orchestrator)

| endpoint | ตรวจ |
|---|---|
| `/health` | ทุก check |
| `/health/live` | process ยังอยู่ (ไม่แตะ DB) — liveness probe |
| `/health/ready` | ต่อ Monitoring DB ได้ — readiness probe |

Kubernetes: ใช้ `/health/live` เป็น livenessProbe, `/health/ready` เป็น readinessProbe

## Production checklist

- HTTPS ที่ reverse proxy (nginx/traefik) — terminate TLS หน้า web
- persist volume: `mssql-data` (DB), `dp-keys` (Data Protection)
- secret ผ่าน env / secret manager (ไม่ใช่ไฟล์ commit)
- ตั้ง backup Monitoring DB — ดู [backup-restore.md](backup-restore.md)
- ปรับ `CollectorSchedule` + `RetentionDays` ตามปริมาณงานจริง
