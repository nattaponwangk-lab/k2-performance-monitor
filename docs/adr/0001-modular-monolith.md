# ADR-0001: Modular Monolith + Clean Architecture

- **Status:** Accepted
- **Date:** 2026-07-04

## Context
ต้องการระบบ monitoring ที่ maintainable, testable, deploy ง่าย สำหรับทีม DBA/Ops ขนาดกลาง

## Decision
ใช้ **Modular Monolith** (8 โปรเจกต์ตาม Clean Architecture) deploy เป็น 2 process (Web + Worker) แชร์ Monitoring DB
ไม่แยกเป็น microservices

## Consequences
- ✅ deploy/ops ง่าย (2 container + 1 DB), debug ข้าม layer ง่าย, refactor ข้าม module ปลอดภัย (compile-time)
- ✅ contract ชัด (interface ใน Core) → mock/test ได้, แยก process ภายหลังได้ถ้าจำเป็น
- ⚠️ Web + Worker แชร์ DB → ต้องระวัง schema coupling (จัดการด้วย EF migration + repository boundary)
- ➖ scale แยกส่วนได้จำกัด (แต่เพียงพอต่อ workload monitoring)
