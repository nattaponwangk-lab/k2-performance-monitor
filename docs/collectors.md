# Collectors

Collector แต่ละตัว implement `ICollector` (ผ่าน `SqlCollectorBase`) อ่านข้อมูลจาก source แล้วคืน `CollectorResult { MetricItem[] }`
Worker ผูก Hangfire recurring job ต่อ collector ตาม `CollectorSchedule` ผ่าน `ICollectorRegistry`

## หลักการ (secure + lightweight)

- **parameterized** ทุกจุดที่รับค่า (TopN/threshold) — ไม่มี SQL injection
- **timeout** — connect 15s, command 30s (ปรับได้)
- **TopN** จำกัดจำนวนแถว — ไม่ query ข้อมูลมหาศาล
- **resilient** — source ล่ม → `CollectorResult.Success=false` (ไม่ crash worker; collector อื่นทำงานต่อ)
- **read-only** — ไม่เขียน source DB

## SQL collectors (Phase 1 — ✅ ทำงานจริง)

| Collector | Source DMV | Metric field (alert) | Delta? |
|---|---|---|---|
| ServerStats | `dm_os_sys_info`, `dm_os_process_memory`, **`dm_os_ring_buffers`**, `dm_exec_connections/requests` | CpuPercent, MemoryPercent, AvailableMemoryMb, BlockedProcessCount | no |
| SlowQuery | `dm_exec_query_stats` + `dm_exec_sql_text` | AvgDurationMs | no (avg) |
| ExecutionPlan | `dm_exec_query_plan` | — (informational) | no |
| WaitStatistics | `dm_os_wait_stats` | WaitTimeMs | **yes** |
| Blocking | `dm_exec_requests` + sessions | BlockingDurationMs | no |
| Deadlock | system_health XE ring buffer | — (informational) | last-seen |
| Index | `dm_db_missing_index_*`, `dm_db_index_usage_stats` | MissingIndexImpact | no |
| Io | `dm_io_virtual_file_stats` | IoStallMsPerRead | **yes** |
| StoredProcedure | `dm_exec_procedure_stats` | AvgDurationMs | no |

### CPU% — แหล่งจริง (ไม่ใช่ heuristic)

`ServerStatsCollector` ใช้ `sys.dm_os_ring_buffers` (`RING_BUFFER_SCHEDULER_MONITOR`):
- `CpuPercent` (host total) = `100 - SystemIdle`
- `SqlProcessCpuPercent` = `ProcessUtilization`
- **sampling:** SQL เขียน record ~1/นาที → ค่าอาจล่าช้าถึง ~1 นาที; granularity ระดับนาที; เป็น CPU ทั้งเครื่อง
- ทำงานได้ทุก edition (รวม Express/LocalDB) — ไม่พึ่ง Resource Governor
ดู [ADR-0002](adr/0002-cpu-from-ring-buffer.md)

### Delta/baseline (DMV สะสม)

DMV เช่น `dm_os_wait_stats`, `dm_io_virtual_file_stats` เป็นค่าสะสมตั้งแต่ server start
→ ใช้ `DeltaBaseline<T>` เก็บ snapshot ก่อนหน้า (in-memory, collector เป็น **singleton**)
- รอบแรกหลัง start = เก็บ baseline (คืน 0 items)
- รอบถัดไป = คืน delta; จัดการ counter reset (current < previous → ถือ current)
- restart worker = re-baseline หนึ่งรอบ (ยอมรับได้ — [ADR-0003](adr/0003-delta-baseline-in-memory.md))

## K2 collectors (Phase 7) — ⛔ BLOCKED

> **สถานะ:** blocked external dependency — ต้องมี K2 instance จริงเพื่อยืนยัน schema ก่อน implement
> ตามกฎโปรเจกต์ "ห้ามเดา schema K2 / ห้ามสร้าง collector จากข้อมูลที่ยังไม่ verify (STOP AND SPIKE)"

หน้า K2 3 หน้าใน dashboard แสดงสถานะ "รอ verify K2 source" (ไม่ใช้ mock)

### แผน PoC (ต้องทำก่อน implement)

1. **ยืนยันแหล่งข้อมูล** — เลือก 1 ใน:
   - **K2 host/runtime DB** (blackpearl): ตาราง candidate `[Server].[ProcInst]`, `[Server].[ActivityInst]`, `[Server].[_ProcInst]` — ต้อง verify ชื่อ/คอลัมน์จริงตามเวอร์ชัน K2
   - **K2 API** (Workflow REST/SmartObject services) — ถ้าต้องการ decouple จาก schema DB
2. **Sample + validate meaning** — query จริง ดูว่า field ไหน = duration / stuck / errored / form load / SMO call time
3. **Performance impact** — วัดผลกระทบต่อ K2 host (จำกัด TopN + interval)
4. **Security** — สิทธิ์ read-only บน K2 DB; credential ผ่าน instance registry (เข้ารหัสแล้ว)

### เมื่อ verify แล้ว — implement

- `K2WorkflowCollector` → MetricField `WorkflowDurationMs` / `StuckWorkflowCount` → entity `K2WorkflowStatEntity` (มีอยู่แล้ว)
- `K2SmartFormCollector` → `FormLoadMs` → `K2SmartFormStatEntity`
- `K2SmartObjectCollector` → `SmartObjectCallMs` → `K2SmartObjectStatEntity`
- เพิ่ม case ใน `MetricRepository.SaveResultAsync` (K2*) + register ใน `AddSqlCollectors`/registry
- alert rules 13–15 (seed แล้ว) จะทำงานอัตโนมัติเมื่อ collector คืน MetricField ที่ตรงกัน
- 3 หน้า K2 wire เข้า `MetricQueryService` (เหมือนหน้า SQL)

โครงสร้าง entity + alert rules + หน้า UI + interface พร้อมรองรับแล้ว — เหลือแค่ตัว collector หลัง verify source
