# Smoke test — Phase 0 (รันในเทอร์มินัลของคุณเอง ที่ล็อกอิน Windows แล้ว)
# ตรวจว่า: migrate-on-startup ทำงาน, Hangfire ยิง ServerStats, และมีข้อมูลลง DB จริง
#
# ใช้งาน:  .\smoke-test.ps1
# ต้องมี: SQL Server local (Server=.) + สิทธิ์สร้าง DB K2PerfMonitor + ต่อ DB K2 ได้

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$worker = Join-Path $root 'src/K2PerfMonitor.Worker'

Write-Host '1) Build Worker...' -ForegroundColor Cyan
dotnet build (Join-Path $worker 'K2PerfMonitor.Worker.csproj') -c Debug -v q

Write-Host '2) Run Worker for ~45s (migrate-on-startup + collect)...' -ForegroundColor Cyan
$proc = Start-Process dotnet -ArgumentList "run --project `"$worker`" --no-build" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 45
if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
Write-Host '   Worker stopped.'

Write-Host '3) Query Monitoring DB...' -ForegroundColor Cyan
$q = @'
SET NOCOUNT ON;
SELECT COUNT(*) AS ServerStatsRows FROM dbo.ServerStats;
SELECT TOP 3 CollectedAtUtc, InstanceName, CpuPercent, MemoryPercent, ConnectionCount
FROM dbo.ServerStats ORDER BY CollectedAtUtc DESC;
SELECT COUNT(*) AS CollectorRuns, SUM(CASE WHEN Success=1 THEN 1 ELSE 0 END) AS Succeeded
FROM dbo.CollectorRuns;
SELECT COUNT(*) AS AlertRulesSeeded FROM dbo.AlertRules;
SELECT COUNT(*) AS ActiveAlerts FROM dbo.Alerts WHERE Status <> 2;
'@
sqlcmd -S . -d K2PerfMonitor -E -C -Q $q

Write-Host ''
Write-Host '4) Latest worker log:' -ForegroundColor Cyan
$log = Get-ChildItem (Join-Path $worker 'logs') -Filter 'worker-*.log' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($log) { Get-Content $log.FullName -Tail 15 } else { Write-Host '   (no log file found)' }

Write-Host ''
Write-Host 'ถ้า ServerStatsRows > 0 และ CollectorRuns.Succeeded > 0 = Phase 0 ทำงานครบวง ✅' -ForegroundColor Green
Write-Host 'เปิด Hangfire dashboard ได้ที่ (รัน Web ก่อน):  http://localhost:5046/hangfire' -ForegroundColor Green
