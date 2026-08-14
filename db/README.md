# Database — K2PerfMonitor (Monitoring DB)

Monitoring DB แยกอิสระจาก source/K2 DB. เก็บ metrics, alerts, alert rules, collector run audit.

## วิธีสร้าง/อัปเดต schema

### ทางเลือก A — รัน SQL script (แนะนำสำหรับ deploy/DBA)
สร้าง database เปล่าก่อน แล้วรัน script (idempotent — รันซ้ำได้):
```sql
CREATE DATABASE K2PerfMonitor;   -- ครั้งแรกเท่านั้น
GO
```
```bash
sqlcmd -S . -d K2PerfMonitor -i db/001_InitialCreate.sql
```
Script `001_InitialCreate.sql` มี `__EFMigrationsHistory` guard — ถ้า migration ถูก apply แล้วจะข้ามให้เอง และ seed default alert rules 15 ข้อ

### ทางเลือก B — EF Core CLI (สำหรับ dev)
```bash
dotnet ef database update \
  --project src/K2PerfMonitor.Data \
  --startup-project src/K2PerfMonitor.Data
```

## เพิ่ม migration ใหม่ (เมื่อแก้ entity)
```bash
dotnet ef migrations add <ชื่อ> \
  --project src/K2PerfMonitor.Data --startup-project src/K2PerfMonitor.Data --output-dir Migrations

# แล้ว regenerate script รวม (idempotent)
dotnet ef migrations script --idempotent \
  --project src/K2PerfMonitor.Data --startup-project src/K2PerfMonitor.Data \
  --output db/001_InitialCreate.sql
```

## Connection string
ตั้งใน `appsettings.json` ของ Worker + Web ที่ `ConnectionStrings:MonitorDb`
(ค่า default: `Server=.;Database=K2PerfMonitor;Trusted_Connection=True;TrustServerCertificate=True`)

> Migration ถูก apply อัตโนมัติตอน Worker startup (ดู `Program.cs`) — script ในโฟลเดอร์นี้ไว้สำหรับ deploy ที่ควบคุมโดย DBA
