# Notifications — คู่มือตั้งค่า

ตั้งค่าใน `src/K2PerfMonitor.Worker/appsettings.json` (หรือ user-secrets/env สำหรับ credential)
ทุกช่องทาง **ปิด (`Enabled: false`) โดย default** — alert จะไม่ส่งจนกว่าจะเปิด

การเลือกช่องทางต่อ rule คุมด้วย `AlertRules.Channels` (flags: `Line=1, Teams=2, Email=4, All=7`)
และมี **cooldown** ต่อ rule (`CooldownMinutes`) กันแจ้งรัวจาก alert เดิม

## Email (SMTP)
```json
"Notifications": {
  "Email": {
    "Enabled": true,
    "Host": "smtp.office365.com",
    "Port": 587,
    "UseSsl": true,
    "UserName": "alert@company.com",
    "Password": "<app-password>",
    "FromAddress": "alert@company.com",
    "FromName": "K2 Performance Monitor",
    "ToAddresses": "dba@company.com; ops@company.com"
  }
}
```
> ผู้รับหลายคนคั่นด้วย `;` หรือ `,`

## Microsoft Teams (Incoming Webhook)
1. ใน Teams channel → **Connectors** → **Incoming Webhook** → คัดลอก URL
```json
"Teams": {
  "Enabled": true,
  "WebhookUrl": "https://outlook.office.com/webhook/....",
  "DashboardUrl": "http://<host>:5046"
}
```
ส่งเป็น MessageCard พร้อมปุ่ม **View in Dashboard**

## LINE Notify
1. ออก token ที่ https://notify-bot.line.me/my/ (1 token = 1 กลุ่ม/ห้อง)
```json
"Line": {
  "Enabled": true,
  "AccessToken": "<line-notify-token>"
}
```
> ⚠️ LINE Notify กำลังปลดระวาง — อนาคตจะย้ายไป LINE Messaging API

## เก็บ credential อย่างปลอดภัย
อย่า commit รหัสผ่าน/token ลง repo — ใช้ user-secrets (dev) หรือ env var:
```bash
dotnet user-secrets set "Notifications:Email:Password" "<app-password>" --project src/K2PerfMonitor.Worker
```
Phase 8 จะย้ายไปเข้ารหัสด้วย ASP.NET Data Protection
