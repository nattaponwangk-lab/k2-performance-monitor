# Notifications

ส่ง alert ออก 3 ช่องทาง: **Email (SMTP)**, **Microsoft Teams (webhook)**, **LINE**
ตั้งค่า credential (ปิดโดย default) — ดู [notifications-setup.md](notifications-setup.md)

## Flow

```
Alert → AlertNotificationService.NotifyAsync
  → โหลด rule (Channels flags + CooldownMinutes)
  → เช็ค cooldown (LastNotifiedAtUtc)
  → fan-out ไป provider ที่ IsEnabled และ channel ตรง rule.Channels
  → ส่ง (retry) → ถ้าสำเร็จอย่างน้อย 1 ช่อง → MarkAlertNotified
```

## Routing

- `AlertRule.Channels` (flags: Email/Teams/LINE/All) กำหนดช่องทางต่อ rule
- provider ปิด (IsEnabled=false) จะถูกข้าม

## Retry

ต่อ provider: **exponential backoff 3 ครั้ง** (0.5s, 1s, 2s) แล้ว log error ถ้ายังล้ม
notification ล้ม **ไม่กระทบ** collector/worker (แยก try/catch)

## Cooldown

`rule.CooldownMinutes` (default 5–360 แล้วแต่ rule) — กันแจ้งซ้ำถี่ ๆ ต่อ alert เดิม

## Security

- credential (SMTP password / webhook / token) ผ่าน **env เท่านั้น** — ไม่ commit, ไม่ log
- provider log เฉพาะสถานะ (status code) ไม่ log payload/secret

## E2E จริง

ต้องตั้ง credential จริง + verify การส่งในสภาพแวดล้อมจริง (providers + retry unit-tested แล้ว)
