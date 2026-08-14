# K2 Performance Monitor

ระบบเฝ้าระวัง + วิเคราะห์ประสิทธิภาพ **SQL Server** และ **K2** แบบ real-time พร้อม alert และแจ้งเตือนผ่าน Email/Teams/LINE

> Modular Monolith + Clean Architecture · .NET 9 · Blazor Server · EF Core · Hangfire · SignalR · Serilog · Docker

[![CI](.github/workflows/ci.yml)](.github/workflows/ci.yml)

---

## ทำอะไรได้บ้าง

- **SQL Server monitoring** — CPU/RAM (ค่าจริงจาก ring buffer), slow queries, execution plans, wait statistics, blocking, deadlocks, index (missing/unused), I/O stalls, stored procedures
- **Pipeline ครบวงจร:** Collect → Store → Evaluate → Alert → Notify → Realtime → Visualize
- **Alert engine** — rule-based, state machine (Firing → Acknowledged → Resolved), de-dup, hysteresis, auto-resolve, severity escalation
- **Notifications** — Email (SMTP) / Microsoft Teams / LINE พร้อม routing, cooldown, retry
- **Real-time dashboard** — SignalR push (CPU/mem trend + alert สด)
- **Auth/RBAC** — Admin / Operator / Viewer + credential encryption (Data Protection)
- **K2 monitoring** — ดู [สถานะ Phase 7](docs/collectors.md#k2-collectors-phase-7--blocked) (blocked external dependency)

## สถาปัตยกรรม (8 โปรเจกต์)

```
Core          contracts/enums/models/options (ไม่มี dependency)
Data          EF Core DbContext + entities + migrations + repository
Collectors    SQL DMV collectors + framework (delta/baseline)
Alerts        alert evaluator (rule matching + hysteresis)
Notifications Email/Teams/LINE providers + routing/cooldown/retry
Realtime      SignalR hub + publisher
Worker        Hangfire orchestration (collect→persist→alert→notify→publish)
Web           Blazor dashboard + SignalR hub host + auth
```

รายละเอียด: [docs/architecture.md](docs/architecture.md)

## เริ่มใช้งานเร็ว (Docker)

```bash
cp .env.example .env      # ตั้ง MSSQL_SA_PASSWORD + ADMIN_INITIAL_PASSWORD
docker compose up -d --build
# เปิด http://localhost:8080  (login: admin / รหัสที่ตั้งใน .env)
```

## เริ่มใช้งาน (local dev)

ต้องมี .NET 9/10 SDK + SQL Server (หรือ LocalDB)

```bash
dotnet build K2PerformanceMonitor.sln
dotnet test  K2PerformanceMonitor.sln
# ตั้ง connection string ใน appsettings.Development.json (ตัวอย่างชี้ LocalDB มาให้แล้ว)
dotnet run --project src/K2PerfMonitor.Worker    # เก็บข้อมูล
dotnet run --project src/K2PerfMonitor.Web       # dashboard :5046
```

## เอกสาร

| หัวข้อ | ไฟล์ |
|---|---|
| สถาปัตยกรรม | [docs/architecture.md](docs/architecture.md) |
| ติดตั้ง | [docs/installation.md](docs/installation.md) |
| ตั้งค่า | [docs/configuration.md](docs/configuration.md) |
| ฐานข้อมูล | [docs/database.md](docs/database.md) |
| Collectors | [docs/collectors.md](docs/collectors.md) |
| Alerts | [docs/alerts.md](docs/alerts.md) |
| Notifications | [docs/notifications.md](docs/notifications.md) · [setup](docs/notifications-setup.md) |
| Deploy | [docs/deployment.md](docs/deployment.md) |
| Troubleshooting | [docs/troubleshooting.md](docs/troubleshooting.md) |
| Backup/Restore | [docs/backup-restore.md](docs/backup-restore.md) |
| Security | [docs/security.md](docs/security.md) |
| ADR | [docs/adr/](docs/adr/) |
| แผนงาน · Checklist | [docs/ROADMAP.md](docs/ROADMAP.md) · [docs/CHECKLIST.md](docs/CHECKLIST.md) |
| สถานะโปรเจกต์ | [PROJECT_STATE.md](PROJECT_STATE.md) |

## Requirement บน source SQL Server

Collector ต้องการสิทธิ์ `VIEW SERVER STATE` (อ่าน DMV) — ดู [docs/installation.md](docs/installation.md)

## License

Internal project.
