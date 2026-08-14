# Troubleshooting

## Worker / collection

| อาการ | สาเหตุ / แก้ |
|---|---|
| ไม่มีข้อมูลในหน้า dashboard | Worker ยังไม่รัน หรือรอบยังไม่ครบ interval · ดู log Worker + ตาราง `CollectorRuns` |
| `CollectorRuns` มี Success=false | source ล่ม / ไม่มีสิทธิ์ · ดู `ErrorMessage` — มัก = ต้อง `VIEW SERVER STATE` |
| CPU% = 0 | ring buffer ยังไม่มี record (instance เพิ่ง start ~1 นาที) หรือ workload ต่ำมาก |
| Wait/IO รอบแรกว่าง | ปกติ — รอบแรกเก็บ baseline (delta), รอบถัดไปมีข้อมูล |
| collector หนึ่งพัง | ไม่กระทบตัวอื่น (แยก job) · ดู log ตาม RunId |

## Web / dashboard

| อาการ | แก้ |
|---|---|
| redirect ไป `/login` ตลอด | ยังไม่ login หรือ cookie หมดอายุ (8h) |
| login แล้วเด้งกลับ | Data Protection key เปลี่ยน (ไม่ persist keys) → ตั้ง `DataProtection:KeyPath` + volume |
| หน้าแสดง "อ่านข้อมูลไม่ได้" | Monitoring DB ต่อไม่ได้ · เช็ค `/health/ready` + connection string |
| ไม่มี 📡 LIVE | `SignalR:Enabled=false` ฝั่ง Worker หรือ HubUrl ผิด/เข้าไม่ถึง web |

## Database / migration

| อาการ | แก้ |
|---|---|
| start แล้ว migrate fail | DB เข้าไม่ได้/สิทธิ์ไม่พอสร้าง table · เช็ค connection + สิทธิ์ `db_owner` บน Monitoring DB |
| Hangfire error ตอน start | schema ยังไม่พร้อม — Web/Worker สร้างเอง (`PrepareSchemaIfNecessary`), รอ SQL healthy |

## Docker compose

| อาการ | แก้ |
|---|---|
| `MSSQL_SA_PASSWORD` error | ยังไม่ได้ตั้งใน `.env` (คัดจาก `.env.example`) |
| web unhealthy | รอ SQL healthy ก่อน (`start_period`) · ดู `docker compose logs web` |
| SA password ถูกปฏิเสธ | ต้องซับซ้อน (>=8 ตัว ผสมตัวใหญ่/เล็ก/เลข/สัญลักษณ์) |

## Log

Serilog เขียน console + `logs/*.log` (rolling รายวัน) พร้อม `RunId` correlation ต่อ collect cycle
