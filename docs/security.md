# Security

## Authentication & Authorization

- **Cookie auth** — HttpOnly, `SameSite=Lax`, 8h sliding expiration, cookie name `K2PM.Auth`
- **Password** — PBKDF2 (HMACSHA256, 100k iterations, per-user 16-byte salt), เทียบแบบ constant-time (`FixedTimeEquals`) — **ไม่มี plaintext**
- **Roles (RBAC):** Admin / Operator / Viewer
  - Admin: Settings, Instances, Users, Hangfire dashboard
  - Operator: monitoring + acknowledge alert
  - Viewer: read-only
- **Global `[Authorize]`** — ทุกหน้า require login (ยกเว้น `/login`, `/Error`) → redirect `/login`
- **Admin-only** pages ใช้ `[Authorize(Roles="Admin")]` + Hangfire `IDashboardAuthorizationFilter`
- **Initial admin** สร้างจาก `Auth:InitialAdminPassword` เท่านั้น — **ไม่มี default password ตายตัว**
- **Open redirect** — `returnUrl` ตอน login ยอมรับเฉพาะ local path (`/...`, กัน `//`)

## Secrets

- Connection strings / SMTP / webhook / token → ผ่าน **environment variables** เท่านั้น (production)
- `appsettings.json` ที่ commit มี credential เป็นค่าว่าง
- **Instance credentials** เก็บ **เข้ารหัส** ด้วย `IDataProtector` (Data Protection) — decrypt เฉพาะฝั่ง server ตอนใช้จริง, ไม่แสดง/ไม่ log plaintext
- **ไม่ log** password/token/webhook/connection string (ตรวจแล้ว — log เฉพาะ username/status code)
- Data Protection keys + `.env` + `*.pfx/*.key` อยู่ใน `.gitignore` — ห้าม commit

## SQL Injection

- Collector ใช้ **parameterized query** ทุกจุดที่รับค่า (`@TopN`, `@ThresholdMs`); DMV text เป็น static
- Web query ใช้ **EF Core LINQ** (parameterized) — ไม่มี raw SQL ต่อ string จาก user input
- Filter/sort/paging ในหน้า dashboard ทำ **client-side** บนชุดข้อมูล TopN (ไม่ส่ง SQL จาก input)

## XSS

- Blazor **auto-encode** ค่าที่ render ทั้งหมด — **ไม่ใช้ `MarkupString`** กับข้อมูล user/source (query text, plan XML, deadlock XML render ใน `<pre>` แบบ encode)

## Source DB permission

- Collector ต้องการ `VIEW SERVER STATE` (read-only) — แนะนำสร้าง login เฉพาะ read-only (least privilege), ไม่ใช้ `sa`

## Resilience (security-adjacent)

- Source ล่ม → collector คืน fail (ไม่ crash), worker/web ยังทำงาน
- Notification ล้ม → retry 3 ครั้ง แล้ว log (ไม่กระทบ collector)
- Realtime ล้ม → best-effort (ไม่กระทบ collector)

## Checklist ก่อน deploy

- [ ] ตั้ง `Auth:InitialAdminPassword` แล้วเปลี่ยนรหัสหลัง login แรก
- [ ] connection string / secret ทั้งหมดผ่าน env
- [ ] persist Data Protection keys (volume) — ไม่งั้น cookie/credential ใช้ไม่ได้หลัง restart
- [ ] source DB ใช้ login read-only (VIEW SERVER STATE)
- [ ] จำกัดการเข้าถึง `/hangfire` (Admin เท่านั้น — ตั้งค่าแล้ว)
- [ ] HTTPS ที่ reverse proxy (production)
