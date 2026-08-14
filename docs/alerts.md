# Alerts

## กลไก

หลัง collect ทุกรอบ `AlertEvaluator` เทียบ `MetricItem` กับ `AlertRule` ที่ enabled ของ collector นั้น

```
Metric → Rule (field + operator + threshold + severity) → Evaluator → Alert state
```

- **Match** (pure, testable): item.MetricField == rule.MetricField และ operator.Matches(value, threshold)
- **DedupKey** = `{CollectorType}:{MetricField}:{ItemKey}` → กัน alert ซ้ำทุก cycle
- **Escalation** — เก็บ severity สูงสุดต่อ dedup key (เช่น CPU 96% เข้าทั้ง >80 และ >95 → เหลือ Critical)

## State machine

```
New (Firing) → Acknowledged (ปุ่ม Ack ในหน้า Alerts) → Resolved (auto เมื่อกลับปกติ)
```

- **Upsert** — ถ้ามี alert active (ไม่ Resolved) ของ dedup key เดิม → อัปเดตค่า + escalate (ไม่ insert ซ้ำ)
- **Auto-resolve** — `ResolveMissingAsync`: alert ที่ dedup key ไม่อยู่ในชุด firing รอบนี้ → Resolved
- **Acknowledge** — Operator/Admin กด Ack → New → Acknowledged

## Hysteresis (กัน flapping)

alert ที่ active อยู่จะ **ยัง firing** จนกว่าค่าจะหลุด hold-band (10%):
- `>` rule: clear เมื่อ value < threshold × 0.9
- `<` rule: clear เมื่อ value > threshold × 1.1

→ ค่าที่แกว่งรอบ threshold ไม่ทำให้ alert เด้ง on/off

## Cooldown

notification เคารพ `rule.CooldownMinutes` (`LastNotifiedAtUtc`) — alert firing ต่อเนื่องไม่ spam

## Rules (seed 15)

CPU >80/>95, Memory <512/<128 MB, SlowQuery avg >5s/>15s, StoredProc >5s, Wait >30s, Blocking >30s/>120s, IO >20ms/read, Missing index impact >80, K2 (3 rules — รอ Phase 7)

แก้ไข/เพิ่ม rule ได้ในตาราง `AlertRules` (หน้า Settings — Admin)
