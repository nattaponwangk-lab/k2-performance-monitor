# Backup & Restore — Monitoring DB

Monitoring DB (`K2PerfMonitor`) เก็บ metrics/alerts/history/users/instances + Hangfire jobs
Backup ตามมาตรฐาน SQL Server (FULL + log ตาม recovery model)

## Backup

```sql
BACKUP DATABASE [K2PerfMonitor]
TO DISK = N'/var/backups/K2PerfMonitor_FULL.bak'
WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;
```

แนะนำ: FULL รายวัน + (ถ้า FULL recovery) log backup ทุก 15–30 นาที · เก็บ off-site · ทดสอบ restore สม่ำเสมอ

### Docker (compose)

```bash
docker exec k2pm-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
  -Q "BACKUP DATABASE [K2PerfMonitor] TO DISK=N'/var/opt/mssql/backup/K2PerfMonitor_FULL.bak' WITH INIT, COMPRESSION, CHECKSUM"
docker cp k2pm-sqlserver:/var/opt/mssql/backup/K2PerfMonitor_FULL.bak ./backups/
```
(volume `mssql-data` เก็บ data ไฟล์อยู่แล้ว — backup ยังจำเป็นสำหรับ point-in-time/off-site)

## Restore

```sql
ALTER DATABASE [K2PerfMonitor] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [K2PerfMonitor]
FROM DISK = N'/var/backups/K2PerfMonitor_FULL.bak'
WITH REPLACE, RECOVERY, STATS = 10;
ALTER DATABASE [K2PerfMonitor] SET MULTI_USER;
```

## สำคัญ: Data Protection keys

การ restore DB **ไม่คืน** Data Protection keys (เก็บนอก DB ที่ `DataProtection:KeyPath` / volume `dp-keys`)
- **backup keys directory ด้วย** — ถ้า keys หาย: cookie เดิม + **instance credentials ที่เข้ารหัสไว้ถอดไม่ได้** (ต้องใส่ connection string instance ใหม่)
- users table ยังอยู่ (password hash ไม่พึ่ง DP keys) → login ได้ปกติหลัง restore

## ทดสอบ restore

restore ลง DB ชื่ออื่นเป็นระยะ + ตรวจ `SELECT COUNT(*)` ตารางหลัก + start Web ชี้ DB นั้น เพื่อยืนยัน integrity
