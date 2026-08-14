# ADR-0003: Delta baseline แบบ in-memory (singleton collectors)

- **Status:** Accepted
- **Date:** 2026-08-14

## Context
DMV บางตัว (`dm_os_wait_stats`, `dm_io_virtual_file_stats`) เป็นค่าสะสมตั้งแต่ server start
ต้องคำนวณ delta ต่อ interval — ห้ามแสดง cumulative เป็น interval metric

## Decision
เก็บ snapshot ก่อนหน้าใน memory (`DeltaBaseline<T>`) โดยลงทะเบียน delta collectors เป็น **Singleton**
พิจารณาเก็บ baseline ใน DB (persist) แล้ว — เลือก in-memory เพราะเรียบง่าย, เป็น pattern มาตรฐานของ wait-stats monitoring

## Consequences
- ✅ เรียบง่าย, ไม่มีตาราง/round-trip เพิ่ม, จัดการ counter reset ได้ (current < previous → ถือ current)
- ⚠️ restart worker → เสีย baseline → re-baseline หนึ่งรอบ (missed interval หนึ่งครั้ง) — ยอมรับได้และ document ไว้
- ⚠️ delta collector ต้องเป็น singleton (ระวัง state/thread-safety → ใช้ `ConcurrentDictionary`)
- point-in-time collectors (Blocking/Index/SlowQuery avg) ไม่ต้อง delta → เป็น Transient ได้
