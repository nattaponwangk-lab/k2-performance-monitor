# Installation

## ความต้องการ

- **.NET SDK 9 หรือ 10** (โปรเจกต์ target `net9.0`; SDK 10 build ได้)
- **SQL Server** สำหรับ Monitoring DB (LocalDB / Express / Standard+ ก็ได้)
- **สิทธิ์บน source SQL Server:** `VIEW SERVER STATE` (อ่าน DMV) — แนะนำสร้าง login read-only เฉพาะ
- (ทางเลือก) Docker + Docker Compose

## สร้าง read-only monitor login บน source

```sql
CREATE LOGIN monitor_ro WITH PASSWORD = '***';
GRANT VIEW SERVER STATE TO monitor_ro;
GRANT VIEW ANY DEFINITION TO monitor_ro;   -- สำหรับ object name ใน plan/index
-- (ต่อ database ที่ต้องการชื่อ object) CREATE USER monitor_ro FOR LOGIN monitor_ro;
```

## Local (dev)

```bash
git clone <repo> && cd "K2 Performance Monitor"
dotnet build K2PerformanceMonitor.sln
dotnet test  K2PerformanceMonitor.sln      # unit + integration (ต้องมี SQL/LocalDB)

# ตั้ง connection string (appsettings.Development.json ตัวอย่างชี้ LocalDB มาให้)
dotnet run --project src/K2PerfMonitor.Worker   # เก็บข้อมูล (migrate + collect)
dotnet run --project src/K2PerfMonitor.Web      # dashboard http://localhost:5046
```

integration test ใช้ SQL ที่ env `MONITOR_TEST_SQL` (default `(localdb)\MSSQLLocalDB`); ถ้าเชื่อมไม่ได้จะ **skip** (ไม่ทำ CI แดง)

## Docker

ดู [deployment.md](deployment.md)

## ครั้งแรก

ตั้ง `Auth:InitialAdminPassword` → start Web → login `admin` → **เปลี่ยนรหัส** + สร้าง user เพิ่มตาม role
