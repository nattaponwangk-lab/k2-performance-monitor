# Architecture

## หลักการ

**Modular Monolith + Clean Architecture** — deploy เป็น 2 process (Web + Worker) แชร์ Monitoring DB เดียวกัน; แยกเป็น 8 โปรเจกต์ตามความรับผิดชอบ โดย dependency ชี้เข้าหา `Core` เสมอ (Core ไม่พึ่งใคร)

```
                 ┌──────────────┐
                 │     Core     │  interfaces, enums, models, options, constants
                 └──────▲───────┘
        ┌───────────────┼───────────────┬───────────────┐
    ┌───┴────┐     ┌────┴─────┐    ┌─────┴──────┐   ┌────┴─────┐
    │  Data  │     │Collectors│    │   Alerts   │   │ Realtime │
    │(EF Core)│    │(SQL DMV) │    │(evaluator) │   │(SignalR) │
    └───▲────┘     └────▲─────┘    └─────▲──────┘   └────▲─────┘
        │  Notifications│                │                │
        │  ┌────────────┴───┐            │                │
        └──┤ (Email/Teams/  │            │                │
           │   LINE)        │            │                │
           └────────────────┘            │                │
     ┌──────────────────────────────────┴────────────────┴──────┐
     │                          Worker                            │  Hangfire jobs
     │            Web (Blazor dashboard + SignalR hub)            │
     └───────────────────────────────────────────────────────────┘
```

## Data flow (pipeline)

```
Hangfire recurring job (per collector, ตาม CollectorSchedule)
   │
   ▼  CollectorJob.RunAsync(type)
ICollector.CollectAsync()  ── อ่าน DMV จาก source (parameterized, timeout, resilient)
   │  CollectorResult { Items: MetricItem[] }
   ▼
IMetricRepository.SaveResultAsync()  ── persist ลง metric table (dispatch ตาม CollectorType)
   │
   ├─▶ IRealtimePublisher.PublishSnapshotAsync()  ── SignalR → browser (throttled)
   │
   ▼
IAlertEvaluator.EvaluateAsync()  ── match rules + hysteresis → firing alerts
   │
   ├─▶ IMetricRepository.UpsertAlertAsync()  ── dedup + escalate + state machine
   ├─▶ IAlertNotifier.NotifyAsync()          ── routing + cooldown + retry → Email/Teams/LINE
   └─▶ IRealtimePublisher.PublishAlertAsync() ── SignalR alert banner
   │
   ▼  ResolveMissingAsync() ── auto-resolve alert ที่กลับปกติ
CollectorRunEntity (audit ทุกรอบ: start/end/success/error/elapsed)
```

หลัง collect ทุกรอบ Web อ่านจาก Monitoring DB (real data) มาแสดง + subscribe SignalR สำหรับ live update

## โปรเจกต์

| โปรเจกต์ | ความรับผิดชอบ | dependency |
|---|---|---|
| `Core` | contracts (`ICollector`, `IMetricRepository`, `IAlertEvaluator`, `IRealtimePublisher`, `INotificationProvider`), enums, models, options | — |
| `Data` | `MonitorDbContext`, 18 entities, migrations, `MetricRepository`, seed | Core |
| `Collectors` | `SqlCollectorBase`, 9 SQL collectors, `DeltaBaseline`, `CollectorRegistry` | Core, Data |
| `Alerts` | `AlertEvaluator` (pure `Match` + hysteresis) | Core, Data |
| `Notifications` | `EmailProvider`, `TeamsProvider`, `LineProvider`, `AlertNotificationService`, cooldown | Core, Data |
| `Realtime` | `MonitorHub`, `SignalRRealtimePublisher`, `NullRealtimePublisher` | Core |
| `Worker` | `CollectorJob`, `RetentionJob`, Hangfire wiring, DI, migrate-on-startup | ทั้งหมด |
| `Web` | Blazor pages, query services, auth/RBAC, SignalR hub host, health | Core, Data, Alerts, Notifications, Realtime |

## การตัดสินใจสำคัญ (ADR)

- [ADR-0001](adr/0001-modular-monolith.md) — Modular Monolith แทน microservices
- [ADR-0002](adr/0002-cpu-from-ring-buffer.md) — CPU% จาก ring buffer (แทน heuristic)
- [ADR-0003](adr/0003-delta-baseline-in-memory.md) — delta baseline in-memory (singleton collectors)

## ทำไม 2 process

- **Worker** = background collection (Hangfire server) — ต้องรันตลอด, restart ได้อิสระ
- **Web** = dashboard (stateless-ish) — scale/redeploy ไม่กระทบการเก็บข้อมูล
- ทั้งคู่ทำ migrate-on-startup (idempotent, EF migration lock) → ตัวไหน start ก่อนก็ปลอดภัย
