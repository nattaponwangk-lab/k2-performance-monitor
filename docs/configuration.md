# Configuration

ตั้งค่าผ่าน `appsettings.json` หรือ **environment variables** (คั่นด้วย `__` เช่น `ConnectionStrings__MonitorDb`)
Docker/production ใช้ env เสมอ — **ห้าม hard-code secret ในไฟล์ที่ commit**

## Connection strings (`ConnectionStrings`)

| Key | ความหมาย |
|---|---|
| `MonitorDb` | Monitoring DB (K2PerfMonitor) — เก็บ metrics/alerts/history/users |
| `SourceDb` | SQL Server ที่จะ monitor (อ่าน DMV) — ต้องมีสิทธิ์ `VIEW SERVER STATE` |
| `K2Db` | K2 host/runtime DB (Phase 7) |

## Collector schedule (`CollectorSchedule`)

| Key | default | หมายเหตุ |
|---|---|---|
| `ServerStatsIntervalSeconds` | 15 | ถี่สุด (real-time) |
| `SlowQueryIntervalSeconds` / `WaitStatsIntervalSeconds` | 60 | |
| `BlockingIntervalSeconds` | 30 | |
| `DeadlockIntervalSeconds` | 120 | |
| `IndexIntervalSeconds` | 300 | ข้อมูลค่อนข้างคงที่ |
| `IoIntervalSeconds` / `StoredProcedureIntervalSeconds` | 120 | |
| `TopN` | 20 | จำกัดจำนวนแถวต่อ collect |
| `SlowQueryThresholdMs` | 1000 | เกณฑ์ query "ช้า" |
| `RetentionDays` | 30 | ลบข้อมูลเก่ากว่านี้ |

## Auth (`Auth`)

| Key | หมายเหตุ |
|---|---|
| `InitialAdminPassword` | สร้าง admin คนแรก (username `admin`) เมื่อยังไม่มีผู้ใช้ · **ถ้าไม่ตั้ง จะไม่สร้าง admin** (ไม่มี default password) |

## Data Protection (`DataProtection`)

| Key | default | หมายเหตุ |
|---|---|---|
| `KeyPath` | `<contentRoot>/keys` | ที่เก็บ key เข้ารหัส cookie + instance credential · **persist volume ใน production** มิฉะนั้น restart แล้ว cookie/credential ถอดรหัสไม่ได้ |

## SignalR (`SignalR`) — ฝั่ง Worker

| Key | default | หมายเหตุ |
|---|---|---|
| `Enabled` | false | เปิด real-time push |
| `HubUrl` | — | URL hub บน Web เช่น `http://web:8080/hubs/monitor` |

## Notifications (`Notifications`)

ปิดทุกช่องโดย default — ดู [notifications-setup.md](notifications-setup.md) · ค่า credential ตั้งผ่าน env เท่านั้น เช่น
`Notifications__Email__Password`, `Notifications__Teams__WebhookUrl`, `Notifications__Line__AccessToken`

## ตัวอย่าง (env)

```bash
ConnectionStrings__MonitorDb="Server=sql;Database=K2PerfMonitor;User Id=sa;Password=***;TrustServerCertificate=True"
ConnectionStrings__SourceDb="Server=sql;Database=master;User Id=monitor_ro;Password=***;TrustServerCertificate=True"
Auth__InitialAdminPassword="***"
SignalR__Enabled=true
SignalR__HubUrl="http://web:8080/hubs/monitor"
```

Options ถูก validate ตอน startup (`ValidateOnStart`) — ค่าจำเป็นขาด = fail fast
