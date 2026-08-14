# ADR-0002: CPU% จาก ring buffer (แทน heuristic)

- **Status:** Accepted
- **Date:** 2026-08-14

## Context
`ServerStatsCollector` เดิมประมาณ CPU ด้วย heuristic `batch/sec ÷ 10` — ไม่มีความหมายจริง (ROADMAP ระบุเป็นบั๊ก)

## Decision
ใช้ `sys.dm_os_ring_buffers` (`RING_BUFFER_SCHEDULER_MONITOR`):
- `CpuPercent` (host total) = `100 - SystemIdle`
- `SqlProcessCpuPercent` = `ProcessUtilization`

พิจารณา `sys.dm_resource_governor_resource_pools` แล้ว — **ปฏิเสธ** เพราะเป็น Enterprise-only (ใช้บน Express/LocalDB ไม่ได้)

## Consequences
- ✅ ค่าจริง, ทำงานทุก edition, ไม่ต้องตั้ง Resource Governor
- ⚠️ granularity ระดับนาที (SQL เขียน record ~1/นาที) → ค่าอาจล่าช้าถึง ~1 นาที
- ⚠️ เป็น CPU ทั้งเครื่อง (host) ไม่แยกตาม instance/pool
- documented ใน [collectors.md](../collectors.md#cpu--แหล่งจริงไม่ใช่-heuristic)
